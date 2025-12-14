namespace ProjectBrain.Api.Middlewares;

using ProjectBrain.Api.Exceptions;
using System.Net;
using System.Text.Json;

/// <summary>
/// Global exception handler middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = new ErrorResponse();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = notFoundEx.ErrorCode,
                        Message = notFoundEx.Message
                    }
                };
                break;

            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = validationEx.ErrorCode,
                        Message = validationEx.Message,
                        Details = validationEx.Errors.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                    }
                };
                break;

            case AppException appEx:
                context.Response.StatusCode = appEx.StatusCode;
                response = new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = appEx.ErrorCode,
                        Message = appEx.Message
                    }
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "INTERNAL_SERVER_ERROR",
                        Message = _environment.IsDevelopment()
                            ? exception.Message
                            : "An error occurred while processing your request."
                    }
                };
                if (_environment.IsDevelopment())
                {
                    response.Error.Details = new Dictionary<string, object?>
                    {
                        ["stackTrace"] = exception.StackTrace,
                        ["innerException"] = exception.InnerException?.Message
                    };
                }
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment(),
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Standard error response structure
/// </summary>
public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = new();
}

/// <summary>
/// Error detail structure
/// </summary>
public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object?>? Details { get; set; }
}

