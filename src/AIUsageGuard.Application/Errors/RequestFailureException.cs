namespace AIUsageGuard.Application.Errors;

public sealed class RequestFailureException : Exception
{
    public RequestFailureException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
