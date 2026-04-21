using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Identity;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Workspaces.CreateWorkspace;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CreateWorkspaceService _createWorkspaceService;
    private readonly SignInGuardService _signInGuardService;
    private readonly IAuditService _auditService;

    public AuthController(
        CreateWorkspaceService createWorkspaceService,
        SignInGuardService signInGuardService,
        IAuditService auditService)
    {
        _createWorkspaceService = createWorkspaceService;
        _signInGuardService = signInGuardService;
        _auditService = auditService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<WorkspaceSessionResponse>> Register([FromBody] RegisterWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _createWorkspaceService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.WorkspaceName,
            cancellationToken);

        await SignInAsync(result.User.Id, result.User.DisplayName, result.User.Email, result.Workspace.Id, result.Membership.Role, cancellationToken);

        return Created(
            $"/workspaces/{result.Workspace.Id}/context",
            new WorkspaceSessionResponse(result.User.Id, result.Workspace.Id, result.Workspace.Name, result.Membership.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<WorkspaceSessionResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _signInGuardService.ValidateAsync(request.Email, request.Password, cancellationToken);
        await SignInAsync(result.User.Id, result.User.DisplayName, result.User.Email, result.Workspace.Id, result.Membership.Role, cancellationToken);
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = result.Workspace.Id,
            ActorUserId = result.User.Id,
            ActionType = "auth.login",
            TargetType = "session",
            TargetId = result.User.Id.ToString(),
            Result = "success",
            Reason = "User signed in successfully."
        }, cancellationToken);

        return Ok(new WorkspaceSessionResponse(result.User.Id, result.Workspace.Id, result.Workspace.Name, result.Membership.Role.ToString()));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var workspaceId = User.FindFirstValue("workspace_id");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            Guid? parsedWorkspaceId = Guid.TryParse(workspaceId, out var currentWorkspaceId)
                ? currentWorkspaceId
                : null;

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = parsedWorkspaceId,
                ActorUserId = parsedUserId,
                ActionType = "auth.logout",
                TargetType = "session",
                TargetId = parsedUserId.ToString(),
                Result = "success",
                Reason = "User signed out successfully."
            }, cancellationToken);
        }

        return NoContent();
    }

    private Task SignInAsync(Guid userId, string displayName, string email, Guid workspaceId, WorkspaceRole role, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new("workspace_id", workspaceId.ToString()),
            new("workspace_role", role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
