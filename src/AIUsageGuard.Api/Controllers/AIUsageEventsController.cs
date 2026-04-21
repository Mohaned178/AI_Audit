using System.Security.Claims;
using System.Text.Json;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.AIUsageEvents.ListEvents;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/events")]
public sealed class AIUsageEventsController : ControllerBase
{
    private readonly IngestAIUsageEventService _ingestService;
    private readonly ListAIUsageEventsService _listService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public AIUsageEventsController(
        IngestAIUsageEventService ingestService,
        ListAIUsageEventsService listService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _ingestService = ingestService;
        _listService = listService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.WorkspaceMember)]
    public async Task<ActionResult<EventIngestionResponse>> Ingest(
        [FromRoute] Guid workspaceId,
        [FromBody] IngestAIUsageEventRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _ingestService.IngestAsync(
            new IngestAIUsageEventCommand(
                workspaceId,
                userId.Value,
                request.IdempotencyKey,
                request.EventType,
                request.OccurredAt,
                request.ToolName,
                request.ModelName,
                request.SourceLabel,
                request.PromptPreview,
                request.FileName,
                request.FileSizeBytes,
                request.InputTokenCount,
                request.OutputTokenCount,
                request.EstimatedCost,
                request.Details is null ? null : JsonSerializer.Serialize(request.Details)),
            cancellationToken);

        var response = ToResponse(result.Event, result.IsDuplicate ? "duplicate" : "accepted");
        return result.IsDuplicate
            ? Ok(response)
            : CreatedAtAction(nameof(List), new { workspaceId }, response);
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<AIUsageEventHistoryResponse>> List(
        [FromRoute] Guid workspaceId,
        [FromQuery] ListAIUsageEventsRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventType = ParseEventType(request.EventType);
        var result = await _listService.ListAsync(
            new ListAIUsageEventsQuery(
                workspaceId,
                userId.Value,
                eventType,
                request.ActorUserId,
                request.ToolName,
                request.FromOccurredAt,
                request.ToOccurredAt,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        var response = new AIUsageEventHistoryResponse(
            result.Items.Select(ToHistoryItemResponse).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);

        return Ok(response);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static AIUsageEventType? ParseEventType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "prompt_submitted" => AIUsageEventType.PromptSubmitted,
            "file_uploaded" => AIUsageEventType.FileUploaded,
            "tool_used" => AIUsageEventType.ToolUsed,
            "usage_recorded" => AIUsageEventType.UsageRecorded,
            "model_called" => AIUsageEventType.ModelCalled,
            _ => throw new AIUsageGuard.Application.Errors.RequestFailureException(400, "Event type is not supported for Phase 2 filtering.")
        };
    }

    private static EventIngestionResponse ToResponse(AIUsageGuard.Application.Models.AIUsageEvent eventRecord, string outcome)
    {
        return new EventIngestionResponse(
            eventRecord.Id,
            eventRecord.WorkspaceId,
            eventRecord.ActorUserId,
            ToApiEventType(eventRecord.EventType),
            outcome,
            eventRecord.IdempotencyKey,
            eventRecord.ToolName,
            eventRecord.ModelName,
            eventRecord.SourceLabel,
            eventRecord.PromptPreview,
            eventRecord.FileName,
            eventRecord.FileSizeBytes,
            eventRecord.InputTokenCount,
            eventRecord.OutputTokenCount,
            eventRecord.EstimatedCost,
            eventRecord.OccurredAt,
            eventRecord.ReceivedAt,
            DeserializeDetails(eventRecord.DetailsJson));
    }

    private static AIUsageEventHistoryItemResponse ToHistoryItemResponse(AIUsageGuard.Application.Models.AIUsageEvent eventRecord)
    {
        return new AIUsageEventHistoryItemResponse(
            eventRecord.Id,
            eventRecord.WorkspaceId,
            eventRecord.ActorUserId,
            ToApiEventType(eventRecord.EventType),
            eventRecord.IdempotencyKey,
            eventRecord.ToolName,
            eventRecord.ModelName,
            eventRecord.SourceLabel,
            eventRecord.PromptPreview,
            eventRecord.FileName,
            eventRecord.FileSizeBytes,
            eventRecord.InputTokenCount,
            eventRecord.OutputTokenCount,
            eventRecord.EstimatedCost,
            eventRecord.OccurredAt,
            eventRecord.ReceivedAt,
            DeserializeDetails(eventRecord.DetailsJson));
    }

    private static IReadOnlyDictionary<string, string>? DeserializeDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
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
}
