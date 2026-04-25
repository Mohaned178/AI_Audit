namespace AIUsageGuard.Application.Identity;

public sealed class SignInHardeningException : Exception
{
    public SignInHardeningException(
        int statusCode,
        string message,
        Guid? workspaceId,
        Guid userId,
        bool lockedOut)
        : base(message)
    {
        StatusCode = statusCode;
        WorkspaceId = workspaceId;
        UserId = userId;
        LockedOut = lockedOut;
    }

    public int StatusCode { get; }

    public Guid? WorkspaceId { get; }

    public Guid UserId { get; }

    public bool LockedOut { get; }
}
