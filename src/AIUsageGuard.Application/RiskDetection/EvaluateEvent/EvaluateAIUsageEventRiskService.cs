using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed class EvaluateAIUsageEventRiskService
{
    private const string RuleVersion = "built-in-v1";
    private static readonly Meter RiskDetectionMeter = new("AIUsageGuard.RiskDetection");
    private static readonly Counter<long> EvaluatedEventsCounter = RiskDetectionMeter.CreateCounter<long>("ai_usage_guard.risk.events_evaluated");
    private static readonly Counter<long> MatchedRulesCounter = RiskDetectionMeter.CreateCounter<long>("ai_usage_guard.risk.rules_matched");
    private static readonly Counter<long> CreatedFindingsCounter = RiskDetectionMeter.CreateCounter<long>("ai_usage_guard.risk.findings_created");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly IReadOnlyList<IRiskRuleEvaluator> _evaluators;
    private readonly ILogger<EvaluateAIUsageEventRiskService> _logger;

    public EvaluateAIUsageEventRiskService(
        IPlatformStore store,
        IAuditService auditService,
        IEnumerable<IRiskRuleEvaluator> evaluators,
        ILogger<EvaluateAIUsageEventRiskService> logger)
    {
        _store = store;
        _auditService = auditService;
        _evaluators = evaluators.OrderBy(item => item.RuleType).ToList();
        _logger = logger;
    }

    public async Task<EvaluateAIUsageEventRiskResult> EvaluateAsync(
        EvaluateAIUsageEventRiskCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var eventRecord = await _store.FindAIUsageEventByIdAsync(command.WorkspaceId, command.EventId, cancellationToken)
            ?? throw new RequestFailureException(404, "AI usage event not found.");

        var existingOutcome = await _store.FindRiskEvaluationOutcomeByEventIdAsync(command.WorkspaceId, command.EventId, cancellationToken);
        if (existingOutcome is not null)
        {
            _logger.LogInformation(
                "Skipping duplicate risk evaluation for workspace {WorkspaceId} event {EventId}; existing outcome {OutcomeId} found.",
                command.WorkspaceId,
                command.EventId,
                existingOutcome.Id);
            return new EvaluateAIUsageEventRiskResult(existingOutcome, [], false);
        }

        var policy = await _store.FindWorkspaceRiskPolicyAsync(command.WorkspaceId, cancellationToken)
            ?? new WorkspaceRiskPolicy { WorkspaceId = command.WorkspaceId };

        _logger.LogInformation(
            "Evaluating AI usage event {EventId} for workspace {WorkspaceId} with tool {ToolName}.",
            eventRecord.Id,
            command.WorkspaceId,
            eventRecord.ToolName);

        var dailyTotal = await GetDailyCostTotalAsync(eventRecord, cancellationToken);
        var matches = new List<RiskRuleMatch>();
        foreach (var evaluator in _evaluators)
        {
            var evaluatorMatches = await evaluator.EvaluateAsync(eventRecord, policy, dailyTotal, cancellationToken);
            matches.AddRange(evaluatorMatches);
        }

        var outcome = new RiskEvaluationOutcome
        {
            WorkspaceId = command.WorkspaceId,
            EventId = command.EventId,
            EvaluatedAt = DateTimeOffset.UtcNow,
            AppliedRuleVersion = RuleVersion,
            MatchedRuleCount = matches.Count,
            EvaluationResult = matches.Count == 0 ? RiskEvaluationResult.NoMatch : RiskEvaluationResult.Matched,
            EvidenceSummary = matches.Count == 0
                ? "No built-in risk rules matched."
                : string.Join("; ", matches.Select(match => $"{match.RuleType}:{match.Reason}"))
        };

        await _store.AddRiskEvaluationOutcomeAsync(outcome, cancellationToken);
        EvaluatedEventsCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", command.WorkspaceId));
        if (matches.Count > 0)
        {
            MatchedRulesCounter.Add(matches.Count, new KeyValuePair<string, object?>("workspace.id", command.WorkspaceId));
        }

        var findings = new List<RiskFinding>();
        foreach (var match in matches)
        {
            var existingFinding = await _store.FindRiskFindingByEventAndRuleAsync(command.WorkspaceId, command.EventId, match.RuleType, cancellationToken);
            if (existingFinding is not null)
            {
                findings.Add(existingFinding);
                continue;
            }

            var finding = new RiskFinding
            {
                WorkspaceId = command.WorkspaceId,
                EventId = command.EventId,
                EvaluationOutcomeId = outcome.Id,
                RuleType = match.RuleType,
                Severity = match.Severity,
                Status = RiskFindingStatus.Open,
                Reason = match.Reason,
                EvidencePreview = NormalizeEvidence(match.EvidencePreview),
                ActorUserId = eventRecord.ActorUserId,
                ToolName = eventRecord.ToolName,
                DetectedAt = DateTimeOffset.UtcNow
            };

            await _store.AddRiskFindingAsync(finding, cancellationToken);
            findings.Add(finding);
            CreatedFindingsCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", command.WorkspaceId));

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = command.WorkspaceId,
                ActorUserId = command.RequestedByUserId,
                ActionType = "risk_finding.create",
                TargetType = "risk_finding",
                TargetId = finding.Id.ToString(),
                Result = "success",
                Reason = finding.Reason
            }, cancellationToken);
        }

        _logger.LogInformation(
            "Completed risk evaluation for workspace {WorkspaceId} event {EventId} with result {EvaluationResult} and {MatchedRuleCount} matched rules.",
            command.WorkspaceId,
            eventRecord.Id,
            outcome.EvaluationResult,
            outcome.MatchedRuleCount);

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = command.WorkspaceId,
            ActorUserId = command.RequestedByUserId,
            ActionType = "risk_evaluation.run",
            TargetType = "ai_usage_event",
            TargetId = eventRecord.Id.ToString(),
            Result = "success",
            Reason = outcome.EvidenceSummary ?? "Risk evaluation completed."
        }, cancellationToken);

        return new EvaluateAIUsageEventRiskResult(outcome, findings, true);
    }

    private async Task<decimal> GetDailyCostTotalAsync(AIUsageEvent eventRecord, CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(eventRecord.OccurredAt.UtcDateTime.Date, TimeSpan.Zero);
        var end = start.AddDays(1);
        return await _store.SumAIUsageEventCostsAsync(eventRecord.WorkspaceId, start, end, cancellationToken);
    }

    private static string? NormalizeEvidence(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(EvaluateAIUsageEventRiskCommand command)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (command.EventId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Event is required.");
        }

        if (command.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }
    }
}
