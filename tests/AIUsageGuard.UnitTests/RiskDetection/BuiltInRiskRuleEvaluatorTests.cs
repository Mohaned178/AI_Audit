using System.Text.Json;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.RiskDetection.EvaluateEvent;

namespace AIUsageGuard.UnitTests.RiskDetection;

public sealed class BuiltInRiskRuleEvaluatorTests
{
    [Fact]
    public async Task Sensitive_data_evaluator_matches_email_in_prompt_preview()
    {
        var evaluator = new SensitiveDataPatternRiskEvaluator();

        var matches = await evaluator.EvaluateAsync(
            new AIUsageEvent
            {
                ToolName = "ChatGPT",
                PromptPreview = "Email me at owner@example.com"
            },
            new WorkspaceRiskPolicy(),
            0m);

        var match = Assert.Single(matches);
        Assert.Equal(RiskRuleType.SensitiveDataPattern, match.RuleType);
        Assert.Equal(RiskSeverity.High, match.Severity);
        Assert.Equal("owner@example.com", match.EvidencePreview);
    }

    [Fact]
    public async Task Sensitive_data_evaluator_matches_phone_in_details_payload()
    {
        var evaluator = new SensitiveDataPatternRiskEvaluator();

        var matches = await evaluator.EvaluateAsync(
            new AIUsageEvent
            {
                ToolName = "ChatGPT",
                DetailsJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["note"] = "Call +1 (555) 123-4567 after review"
                })
            },
            new WorkspaceRiskPolicy(),
            0m);

        var match = Assert.Single(matches);
        Assert.Equal(RiskRuleType.SensitiveDataPattern, match.RuleType);
        Assert.Equal("+1 (555) 123-4567", match.EvidencePreview);
    }

    [Fact]
    public async Task File_upload_evaluator_matches_uploaded_file_metadata()
    {
        var evaluator = new FileUploadRiskEvaluator();

        var matches = await evaluator.EvaluateAsync(
            new AIUsageEvent
            {
                EventType = AIUsageEventType.FileUploaded,
                ToolName = "ChatGPT",
                FileName = "draft.pdf"
            },
            new WorkspaceRiskPolicy(),
            0m);

        var match = Assert.Single(matches);
        Assert.Equal(RiskRuleType.FileUpload, match.RuleType);
        Assert.Equal(RiskSeverity.Medium, match.Severity);
        Assert.Equal("draft.pdf", match.EvidencePreview);
    }

    [Fact]
    public async Task File_upload_evaluator_returns_no_match_when_file_data_is_absent()
    {
        var evaluator = new FileUploadRiskEvaluator();

        var matches = await evaluator.EvaluateAsync(
            new AIUsageEvent
            {
                EventType = AIUsageEventType.PromptSubmitted,
                ToolName = "ChatGPT"
            },
            new WorkspaceRiskPolicy(),
            0m);

        Assert.Empty(matches);
    }
}
