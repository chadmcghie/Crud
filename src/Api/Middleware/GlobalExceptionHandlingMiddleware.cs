using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
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
            _logger.LogError(ex, "An unhandled exception occurred during request {Method} {Path}. " +
                "Request ID: {RequestId}, User: {User}", 
                context.Request.Method, 
                context.Request.Path,
                context.TraceIdentifier,
                context.User?.Identity?.Name ?? "Anonymous");
                
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ValidationException validationException => new ErrorResponse
            {
                Status = HttpStatusCode.BadRequest,
                Title = "Validation Failed",
                Errors = validationException.Errors.ToDictionary(
                    e => e.PropertyName,
                    e => new[] { e.ErrorMessage }
                )
            },
            KeyNotFoundException keyNotFoundException => new ErrorResponse
            {
                Status = HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = keyNotFoundException.Message
            },
            ArgumentException argumentException => new ErrorResponse
            {
                Status = HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = argumentException.Message
            },
            InvalidOperationException invalidOperationException => new ErrorResponse
            {
                Status = HttpStatusCode.Conflict,
                Title = "Operation Failed",
                Detail = invalidOperationException.Message
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Status = HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "You are not authorized to perform this action"
            },
            _ => new ErrorResponse
            {
                Status = HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = _environment.IsDevelopment() || _environment.EnvironmentName == "Testing"
                    ? exception.ToString() 
                    : "An error occurred while processing your request"
            }
        };

        context.Response.StatusCode = (int)response.Status;
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public HttpStatusCode Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}