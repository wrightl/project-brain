namespace ProjectBrain.Domain.Exceptions;

/// <summary>
/// Base exception for application-specific errors
/// </summary>
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AppException(string errorCode, string message, int statusCode = 500) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public AppException(string errorCode, string message, Exception innerException, int statusCode = 500) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

