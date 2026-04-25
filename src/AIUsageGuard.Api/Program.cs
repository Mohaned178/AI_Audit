using System.Diagnostics;
using System.Diagnostics.Metrics;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.AIUsageEvents.ListEvents;
using AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;
using AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;
using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Billing.GetBillingCycle;
using AIUsageGuard.Application.Billing.GetPlanStatus;
using AIUsageGuard.Application.Billing.ListBillingCycles;
using AIUsageGuard.Application.Billing.ReconcileUsageCycles;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Auditing.GetAuditLog;
using AIUsageGuard.Application.Auditing.ListAuditLogs;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Identity;
using AIUsageGuard.Application.Memberships.CreateMembership;
using AIUsageGuard.Application.Memberships.ListMemberships;
using AIUsageGuard.Application.Memberships.UpdateMembership;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.Application.Notifications.GetNotification;
using AIUsageGuard.Application.Notifications.GetNotificationPreferences;
using AIUsageGuard.Application.Notifications.ListNotifications;
using AIUsageGuard.Application.Reporting.GetAlertsSummary;
using AIUsageGuard.Application.Reporting.GetCostSummary;
using AIUsageGuard.Application.Reporting.GetDashboard;
using AIUsageGuard.Application.Reporting.GetUsageByTool;
using AIUsageGuard.Application.Reporting.GetUsageByUser;
using AIUsageGuard.Application.RiskDetection.EvaluateEvent;
using AIUsageGuard.Application.RiskDetection.GetFinding;
using AIUsageGuard.Application.RiskDetection.GetRiskPolicy;
using AIUsageGuard.Application.RiskDetection.ListFindings;
using AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Api.Security;
using AIUsageGuard.Application.Workspaces.CreateWorkspace;
using AIUsageGuard.Application.Workspaces.GetWorkspaceContext;
using AIUsageGuard.Application.Workspaces.ResolveCurrentWorkspace;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.BackgroundProcessing;
using AIUsageGuard.Infrastructure.Identity;
using AIUsageGuard.Infrastructure.Notifications;
using AIUsageGuard.Infrastructure.Billing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.Infrastructure.Tenancy;
using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var apiMeter = new Meter("AIUsageGuard.Api");
var failedRequestCounter = apiMeter.CreateCounter<long>("ai_usage_guard.api.failed_requests");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId;
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AI Usage Guard API",
            Version = "v1",
            Description = "Workspace-scoped SaaS API for monitoring AI usage, detecting risk, and reporting cost and governance data."
        });

        options.OperationFilter<ProtectedRequestIntegrityOperationFilter>();
    });
}
var databaseProvider =
    builder.Configuration["Database:Provider"] ??
    builder.Configuration["DATABASE_PROVIDER"] ??
    "Postgres";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DEFAULT_CONNECTION"]
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<IPlatformStore>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<WorkspaceContextAccessor>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditRequestContextAccessor>();
builder.Services.AddSingleton<AuditCategoryResolver>();
builder.Services.AddScoped<ListAuditLogsService>();
builder.Services.AddScoped<GetAuditLogService>();
builder.Services.Configure<NotificationProcessingOptions>(builder.Configuration.GetSection(NotificationProcessingOptions.SectionName));
builder.Services.Configure<DigestSchedulingOptions>(builder.Configuration.GetSection(DigestSchedulingOptions.SectionName));
builder.Services.Configure<BillingReconciliationOptions>(builder.Configuration.GetSection(BillingReconciliationOptions.SectionName));
builder.Services.Configure<SignInHardeningOptions>(builder.Configuration.GetSection(SignInHardeningOptions.SectionName));
builder.Services.Configure<ProtectedRequestIntegrityOptions>(builder.Configuration.GetSection(ProtectedRequestIntegrityOptions.SectionName));
builder.Services.AddSingleton<BackgroundProcessingClock>();
builder.Services.AddSingleton<BackgroundJobScopeRunner>();
builder.Services.AddScoped<INotificationSender>(_ => builder.Environment.IsProduction()
    ? new EmailNotificationSender(_.GetRequiredService<ILogger<EmailNotificationSender>>())
    : new LoggedNotificationSender(_.GetRequiredService<ILogger<LoggedNotificationSender>>()));
builder.Services.AddScoped<BillingDimensionCounter>();
builder.Services.AddScoped<BillingLimitEvaluator>();
builder.Services.AddScoped<DefaultPlanCatalogSeeder>();
builder.Services.AddScoped<UsageCycleFactory>();
builder.Services.AddScoped<ApplyWorkspacePlanAssignmentService>();
builder.Services.AddScoped<CreateWorkspaceService>();
builder.Services.AddScoped<SignInGuardService>();
builder.Services.AddScoped<ProtectedRequestIntegrityService>();
builder.Services.AddScoped<AIUsageGuard.Api.Security.ProtectedRequestIntegrityFilter>();
builder.Services.AddScoped<GetWorkspaceContextService>();
builder.Services.AddScoped<ResolveCurrentWorkspaceService>();
builder.Services.AddScoped<ListMembershipsService>();
builder.Services.AddScoped<CreateMembershipService>();
builder.Services.AddScoped<UpdateMembershipService>();
builder.Services.AddScoped<EvaluateAIUsageEventRiskService>();
builder.Services.AddScoped<IRiskRuleEvaluator, SensitiveDataPatternRiskEvaluator>();
builder.Services.AddScoped<IRiskRuleEvaluator, FileUploadRiskEvaluator>();
builder.Services.AddScoped<IRiskRuleEvaluator, UnapprovedToolRiskEvaluator>();
builder.Services.AddScoped<IRiskRuleEvaluator, CostThresholdRiskEvaluator>();
builder.Services.AddScoped<ListRiskFindingsService>();
builder.Services.AddScoped<GetRiskFindingService>();
builder.Services.AddScoped<GetWorkspaceRiskPolicyService>();
builder.Services.AddScoped<UpdateWorkspaceRiskPolicyService>();
builder.Services.AddScoped<IngestAIUsageEventService>();
builder.Services.AddScoped<ListAIUsageEventsService>();
builder.Services.AddScoped<ListBillingCyclesService>();
builder.Services.AddScoped<GetBillingCycleService>();
builder.Services.AddScoped<GetPlanStatusService>();
builder.Services.AddScoped<GetDashboardService>();
builder.Services.AddScoped<GetUsageByUserService>();
builder.Services.AddScoped<GetUsageByToolService>();
builder.Services.AddScoped<GetAlertsSummaryService>();
builder.Services.AddScoped<GetCostSummaryService>();
builder.Services.AddScoped<RunUrgentAlertScanService>();
builder.Services.AddScoped<RunDigestGenerationService>();
builder.Services.AddScoped<RunDeliveryRetryService>();
builder.Services.AddScoped<ReconcileUsageCyclesService>();
builder.Services.AddScoped<DeliverPendingNotificationsService>();
builder.Services.AddScoped<GetNotificationPreferencesService>();
builder.Services.AddScoped<UpdateWorkspaceNotificationPreferencesService>();
builder.Services.AddScoped<ListNotificationsService>();
builder.Services.AddScoped<GetNotificationService>();
builder.Services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<NotificationBackgroundWorker>();
    builder.Services.AddHostedService<BillingCycleBackgroundWorker>();
}
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.Cookie.Name = "AIUsageGuard.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WorkspacePolicies.WorkspaceMember, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new WorkspaceMemberRequirement());
    });

    options.AddPolicy(WorkspacePolicies.WorkspaceAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new WorkspaceRoleRequirement(WorkspaceRole.Admin));
    });

    options.AddPolicy(WorkspacePolicies.WorkspaceOwner, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new WorkspaceRoleRequirement(WorkspaceRole.Owner));
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DefaultPlanCatalogSeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            RequestFailureException failure => failure.StatusCode,
            SignInHardeningException failure => failure.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        failedRequestCounter.Add(
            1,
            new KeyValuePair<string, object?>("http.status_code", statusCode),
            new KeyValuePair<string, object?>("exception.type", exception?.GetType().Name ?? "unknown"),
            new KeyValuePair<string, object?>("request.path", context.Request.Path.Value));

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            app.Logger.LogError(
                exception,
                "Unhandled request failure for {Method} {Path} returned {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                statusCode);
        }
        else
        {
            app.Logger.LogWarning(
                exception,
                "Request failure for {Method} {Path} returned {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                statusCode);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = CreateProblemDetails(context, exception, statusCode);

        await context.Response.WriteAsJsonAsync(problem);
    });
});
app.UseHttpsRedirection();
app.UseHttpLogging();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = context.TraceIdentifier;
    }

    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/antiforgery-token", (HttpContext httpContext, IAntiforgery antiforgery) =>
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        if (string.IsNullOrWhiteSpace(tokens.RequestToken))
        {
            return Results.Problem(
                title: "Unable to generate antiforgery token.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new AntiforgeryTokenResponse(
            tokens.RequestToken,
            "X-CSRF-TOKEN",
            "Use the returned request token in the X-CSRF-TOKEN header for protected write requests."));
    })
    .AllowAnonymous()
    .WithName("GetAntiforgeryToken")
    .WithTags("Development")
    .Produces<AntiforgeryTokenResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Usage Guard API v1");
        options.DisplayRequestDuration();
        options.DocumentTitle = "AI Usage Guard API Documentation";
    });
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static ProblemDetails CreateProblemDetails(HttpContext context, Exception? exception, int statusCode)
{
    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = ReasonPhrases.GetReasonPhrase(statusCode),
        Detail = statusCode >= StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception?.Message,
        Instance = context.Request.Path
    };

    problem.Extensions["traceId"] = context.TraceIdentifier;
    problem.Extensions["statusCode"] = statusCode;
    return problem;
}

internal sealed record AntiforgeryTokenResponse(
    string RequestToken,
    string HeaderName,
    string Instructions);

public partial class Program;
