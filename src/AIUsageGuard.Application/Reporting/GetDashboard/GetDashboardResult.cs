using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting.GetDashboard;

public sealed record GetDashboardResult(
    DashboardSummary Summary);
