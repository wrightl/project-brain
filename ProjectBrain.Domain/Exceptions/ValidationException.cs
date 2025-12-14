namespace ProjectBrain.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : AppException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors) 
        : base("VALIDATION_ERROR", "One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error) 
        : base("VALIDATION_ERROR", "One or more validation errors occurred.", 400)
    {
        Errors = new Dictionary<string, string[]> { [field] = new[] { error } };
    }
}

