using System.Text.Json;
using System.Text.RegularExpressions;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed class SensitiveDataPatternRiskEvaluator : IRiskRuleEvaluator
{
    private static readonly Regex EmailPattern = new(@"(?<![\w.+-])[\w.+-]+@[\w-]+(?:\.[\w-]+)+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"(?<!\d)(?:\+?\d[\d\s().-]{7,}\d)(?!\d)", RegexOptions.Compiled);

    public RiskRuleType RuleType => RiskRuleType.SensitiveDataPattern;

    public Task<IReadOnlyList<RiskRuleMatch>> EvaluateAsync(
        AIUsageEvent eventRecord,
        WorkspaceRiskPolicy policy,
        decimal dailyEstimatedCost,
        CancellationToken cancellationToken = default)
    {
        _ = policy;
        _ = dailyEstimatedCost;
        _ = cancellationToken;

        var evidence = CollectTextEvidence(eventRecord);
        foreach (var value in evidence)
        {
            var emailMatch = EmailPattern.Match(value);
            if (emailMatch.Success)
            {
                return Task.FromResult<IReadOnlyList<RiskRuleMatch>>(
                    [new RiskRuleMatch(RiskRuleType.SensitiveDataPattern, RiskSeverity.High, "Prompt or metadata contains an email address.", emailMatch.Value)]);
            }

            var phoneMatch = PhonePattern.Match(value);
            if (phoneMatch.Success)
            {
                return Task.FromResult<IReadOnlyList<RiskRuleMatch>>(
                    [new RiskRuleMatch(RiskRuleType.SensitiveDataPattern, RiskSeverity.High, "Prompt or metadata contains a phone number.", phoneMatch.Value)]);
            }
        }

        return Task.FromResult<IReadOnlyList<RiskRuleMatch>>([]);
    }

    private static IReadOnlyList<string> CollectTextEvidence(AIUsageEvent eventRecord)
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(eventRecord.PromptPreview))
        {
            values.Add(eventRecord.PromptPreview);
        }

        if (string.IsNullOrWhiteSpace(eventRecord.DetailsJson))
        {
            return values;
        }

        try
        {
            using var document = JsonDocument.Parse(eventRecord.DetailsJson);
            foreach (var element in document.RootElement.EnumerateObject())
            {
                if (element.Value.ValueKind == JsonValueKind.String)
                {
                    var value = element.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value);
                    }
                }
            }
        }
        catch (JsonException)
        {
            return values;
        }

        return values;
    }
}
