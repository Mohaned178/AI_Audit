using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetAlertsSummary;
using AIUsageGuard.Application.Reporting.GetCostSummary;
using AIUsageGuard.Application.Reporting.GetUsageByTool;
using AIUsageGuard.Application.Reporting.GetUsageByUser;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly GetUsageByUserService _usageByUserService;
    private readonly GetUsageByToolService _usageByToolService;
    private readonly GetAlertsSummaryService _alertsSummaryService;
    private readonly GetCostSummaryService _costSummaryService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public ReportsController(
        GetUsageByUserService usageByUserService,
        GetUsageByToolService usageByToolService,
        GetAlertsSummaryService alertsSummaryService,
        GetCostSummaryService costSummaryService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _usageByUserService = usageByUserService;
        _usageByToolService = usageByToolService;
        _alertsSummaryService = alertsSummaryService;
        _costSummaryService = costSummaryService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet("usage-by-user")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<UsageSummaryResponse>> GetUsageByUser(
        [FromRoute] Guid workspaceId,
        [FromQuery] PagedReportingRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _usageByUserService.GetAsync(
            new GetUsageByUserQuery(
                workspaceId,
                userId.Value,
                new ReportingPeriodQuery(request.FromDate, request.ToDate),
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        return Ok(ReportingResponseFactory.ToUsageSummaryResponse(result.WorkspaceId, result.Period, "user", result.Page));
    }

    [HttpGet("usage-by-tool")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<UsageSummaryResponse>> GetUsageByTool(
        [FromRoute] Guid workspaceId,
        [FromQuery] PagedReportingRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _usageByToolService.GetAsync(
            new GetUsageByToolQuery(
                workspaceId,
                userId.Value,
                new ReportingPeriodQuery(request.FromDate, request.ToDate),
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        return Ok(ReportingResponseFactory.ToUsageSummaryResponse(result.WorkspaceId, result.Period, "tool", result.Page));
    }

    [HttpGet("alerts-summary")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<AlertsSummaryResponse>> GetAlertsSummary(
        [FromRoute] Guid workspaceId,
        [FromQuery] ReportingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _alertsSummaryService.GetAsync(
            new GetAlertsSummaryQuery(
                workspaceId,
                userId.Value,
                new ReportingPeriodQuery(request.FromDate, request.ToDate)),
            cancellationToken);

        return Ok(ReportingResponseFactory.ToAlertsSummaryResponse(result.WorkspaceId, result.Period, result.Summary));
    }

    [HttpGet("cost-summary")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<CostSummaryResponse>> GetCostSummary(
        [FromRoute] Guid workspaceId,
        [FromQuery] ReportingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _costSummaryService.GetAsync(
            new GetCostSummaryQuery(
                workspaceId,
                userId.Value,
                new ReportingPeriodQuery(request.FromDate, request.ToDate)),
            cancellationToken);

        return Ok(ReportingResponseFactory.ToCostSummaryResponse(result.WorkspaceId, result.Period, result.Summary));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
