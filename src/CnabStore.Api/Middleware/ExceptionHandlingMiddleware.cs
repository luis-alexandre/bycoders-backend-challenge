using System.Net;
using System.Text.Json;

namespace CnabStore.Api.Middleware;

/// <summary>
/// Middleware responsible for handling unhandled exceptions
/// </summary>
public class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var errorMessage = "An unexpected error has occurred.";
        string? details = null;

        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                errorMessage = "Invalid request.";
                details = argEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                errorMessage = "Operation could not be completed.";
                details = invalidOpEx.Message;
                break;
            default:
                details = exception.Message;
                break;
        }

        _logger.LogError(exception, 
                         "Unhandled exception caught by middleware. StatusCode={StatusCode}, Message={Message}",
                         statusCode,
                         exception.Message);

        var traceId = context.TraceIdentifier;

        var errorPayload = new
        {
            traceId,
            statusCode,
            error = errorMessage,
            details
        };

        var json = JsonSerializer.Serialize(errorPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(json);
    }
}
