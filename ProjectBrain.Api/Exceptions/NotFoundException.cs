namespace ProjectBrain.Api.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object? resourceId = null) 
        : base(
            "NOT_FOUND",
            resourceId != null 
                ? $"{resourceName} with ID '{resourceId}' was not found."
                : $"{resourceName} was not found.",
            404)
    {
    }
}

