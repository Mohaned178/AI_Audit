using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;

    public CreateWorkspaceService(
        IPlatformStore store,
        IAuditService auditService,
        ApplyWorkspacePlanAssignmentService planAssignmentService)
    {
        _store = store;
        _auditService = auditService;
        _planAssignmentService = planAssignmentService;
    }

    public async Task<RegisterWorkspaceResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        string workspaceName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await _store.FindUserByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            throw new RequestFailureException(400, "An account already exists for this email.");
        }

        var slug = Slugify(workspaceName);
        var existingWorkspace = await _store.FindWorkspaceBySlugAsync(slug, cancellationToken);
        if (existingWorkspace is not null)
        {
            throw new RequestFailureException(400, "A workspace with the same name already exists.");
        }

        var user = new UserAccount
        {
            Email = normalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = string.Empty
        };
        user.PasswordHash = PasswordHashing.HashPassword(password);

        var workspace = new Workspace
        {
            Name = workspaceName.Trim(),
            Slug = slug,
            CreatedByUserId = user.Id
        };

        var membership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = user.Id,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        };

        await _store.AddUserAsync(user, cancellationToken);
        await _store.AddWorkspaceAsync(workspace, cancellationToken);
        await _store.AddMembershipAsync(membership, cancellationToken);
        await _planAssignmentService.EnsureCurrentCycleAsync(workspace.Id, user.Id, DateTimeOffset.UtcNow, cancellationToken);

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspace.Id,
            ActorUserId = user.Id,
            ActionType = "workspace.register",
            TargetType = "workspace",
            TargetId = workspace.Id.ToString(),
            Result = "success",
            Reason = "Workspace and owner account created."
        }, cancellationToken);

        return new RegisterWorkspaceResult(user, workspace, membership);
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
