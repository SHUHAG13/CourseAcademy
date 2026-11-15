using CourseAcademy.Api.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Common.Middleware;

/// <summary>Turns every unhandled exception into the standard response envelope. Internal details are logged, never returned.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public const string UnexpectedErrorMessage = "An unexpected error occurred. Please try again later.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is no one to answer.
            logger.LogDebug("Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(exception, "Unhandled exception after the response started.");
                throw;
            }

            var (status, body) = Map(exception);
            if (status >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                logger.LogInformation("Request {Method} {Path} failed with {Status}: {Message}",
                    context.Request.Method, context.Request.Path, status, body.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(body, context.RequestAborted);
        }
    }

    internal static (int Status, ApiResponse<object?> Body) Map(Exception exception) => exception switch
    {
        AppException app => (app.StatusCode, ApiResponse.Fail(app.Message, app.Errors)),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            ApiResponse.Fail("This item was changed by someone else. Reload and try again.")),
        BadHttpRequestException bad => (bad.StatusCode, ApiResponse.Fail("The request could not be read.")),
        _ => (StatusCodes.Status500InternalServerError, ApiResponse.Fail(UnexpectedErrorMessage)),
    };
}
