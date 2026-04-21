using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.AIUsageEvents.ListEvents;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Identity;
using AIUsageGuard.Application.Memberships.CreateMembership;
using AIUsageGuard.Application.Memberships.ListMemberships;
using AIUsageGuard.Application.Memberships.UpdateMembership;
using AIUsageGuard.Application.Workspaces.CreateWorkspace;
using AIUsageGuard.Application.Workspaces.GetWorkspaceContext;
using AIUsageGuard.Application.Workspaces.ResolveCurrentWorkspace;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Identity;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.Infrastructure.Tenancy;
using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode;
});
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Postgres";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
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
builder.Services.AddScoped<CreateWorkspaceService>();
builder.Services.AddScoped<SignInGuardService>();
builder.Services.AddScoped<GetWorkspaceContextService>();
builder.Services.AddScoped<ResolveCurrentWorkspaceService>();
builder.Services.AddScoped<ListMembershipsService>();
builder.Services.AddScoped<CreateMembershipService>();
builder.Services.AddScoped<UpdateMembershipService>();
builder.Services.AddScoped<IngestAIUsageEventService>();
builder.Services.AddScoped<ListAIUsageEventsService>();
builder.Services.AddScoped<IAuthorizationHandler, WorkspaceAuthorizationHandler>();
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

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            RequestFailureException failure => failure.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = exception?.Message
        };

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

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
