using System.Security.Claims;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.RiskDetection.GetFinding;
using AIUsageGuard.Application.RiskDetection.ListFindings;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/risk-findings")]
public sealed class RiskFindingsController : ControllerBase
{
    private readonly ListRiskFindingsService _listService;
    private readonly GetRiskFindingService _getService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public RiskFindingsController(
        ListRiskFindingsService listService,
        GetRiskFindingService getService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _listService = listService;
        _getService = getService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<RiskFindingListResponse>> List(
        [FromRoute] Guid workspaceId,
        [FromQuery] ListRiskFindingsRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _listService.ListAsync(
            new ListRiskFindingsQuery(
                workspaceId,
                userId.Value,
                ParseRuleType(request.RuleType),
                ParseSeverity(request.Severity),
                ParseStatus(request.Status),
                request.ActorUserId,
                request.ToolName,
                request.FromDetectedAt,
                request.ToDetectedAt,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        var response = new RiskFindingListResponse(
            result.Items.Select(ToListItemResponse).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);

        return Ok(response);
    }

    [HttpGet("{findingId:guid}")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<RiskFindingDetailResponse>> Get(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid findingId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getService.GetAsync(new GetRiskFindingQuery(workspaceId, userId.Value, findingId), cancellationToken);
        return Ok(ToDetailResponse(result));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static RiskFindingListItemResponse ToListItemResponse(RiskFinding finding)
    {
        return new RiskFindingListItemResponse(
            finding.Id,
            finding.WorkspaceId,
            finding.EventId,
            finding.EvaluationOutcomeId,
            ToApiRiskRuleType(finding.RuleType),
            ToApiRiskSeverity(finding.Severity),
            ToApiRiskFindingStatus(finding.Status),
            finding.Reason,
            finding.EvidencePreview,
            finding.ActorUserId,
            finding.ToolName,
            finding.DetectedAt);
    }

    private static RiskFindingDetailResponse ToDetailResponse(GetRiskFindingResult result)
    {
        return new RiskFindingDetailResponse(
            result.Finding.Id,
            result.Finding.WorkspaceId,
            result.Finding.EventId,
            result.Finding.EvaluationOutcomeId,
            ToApiRiskRuleType(result.Finding.RuleType),
            ToApiRiskSeverity(result.Finding.Severity),
            ToApiRiskFindingStatus(result.Finding.Status),
            result.Finding.Reason,
            result.Finding.EvidencePreview,
            result.Finding.ActorUserId,
            result.Finding.ToolName,
            result.Finding.DetectedAt,
            result.Outcome.AppliedRuleVersion,
            ToApiRiskEvaluationResult(result.Outcome.EvaluationResult),
            result.Outcome.MatchedRuleCount,
            result.Outcome.EvidenceSummary,
            new RiskFindingEventContextResponse(
                result.EventRecord.Id,
                ToApiEventType(result.EventRecord.EventType),
                result.EventRecord.ActorUserId,
                result.EventRecord.ToolName,
                result.EventRecord.ModelName,
                result.EventRecord.SourceLabel,
                result.EventRecord.PromptPreview,
                result.EventRecord.FileName,
                result.EventRecord.FileSizeBytes,
                result.EventRecord.EstimatedCost,
                result.EventRecord.OccurredAt,
                result.EventRecord.ReceivedAt));
    }

    private static RiskRuleType? ParseRuleType(string? value)
    {
        return ParseNullableEnum<RiskRuleType>(value, "Risk rule type is not supported.");
    }

    private static RiskSeverity? ParseSeverity(string? value)
    {
        return ParseNullableEnum<RiskSeverity>(value, "Risk severity is not supported.");
    }

    private static RiskFindingStatus? ParseStatus(string? value)
    {
        return ParseNullableEnum<RiskFindingStatus>(value, "Risk finding status is not supported.");
    }

    private static TEnum? ParseNullableEnum<TEnum>(string? value, string errorMessage) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new RequestFailureException(400, errorMessage);
    }

    private static string ToApiEventType(AIUsageEventType eventType)
    {
        return eventType switch
        {
            AIUsageEventType.PromptSubmitted => "prompt_submitted",
            AIUsageEventType.FileUploaded => "file_uploaded",
            AIUsageEventType.ToolUsed => "tool_used",
            AIUsageEventType.UsageRecorded => "usage_recorded",
            AIUsageEventType.ModelCalled => "model_called",
            _ => eventType.ToString()
        };
    }

    private static string ToApiRiskRuleType(RiskRuleType value)
    {
        return value switch
        {
            RiskRuleType.SensitiveDataPattern => "sensitive_data_pattern",
            RiskRuleType.FileUpload => "file_upload",
            RiskRuleType.UnapprovedTool => "unapproved_tool",
            RiskRuleType.CostThresholdExceeded => "cost_threshold_exceeded",
            _ => value.ToString()
        };
    }

    private static string ToApiRiskSeverity(RiskSeverity value)
    {
        return value switch
        {
            RiskSeverity.Low => "low",
            RiskSeverity.Medium => "medium",
            RiskSeverity.High => "high",
            _ => value.ToString()
        };
    }

    private static string ToApiRiskFindingStatus(RiskFindingStatus value)
    {
        return value switch
        {
            RiskFindingStatus.Open => "open",
            _ => value.ToString()
        };
    }

    private static string ToApiRiskEvaluationResult(RiskEvaluationResult value)
    {
        return value switch
        {
            RiskEvaluationResult.NoMatch => "no_match",
            RiskEvaluationResult.Matched => "matched",
            RiskEvaluationResult.Skipped => "skipped",
            _ => value.ToString()
        };
    }
}
