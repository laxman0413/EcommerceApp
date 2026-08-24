using System.Net;
using EcommerceApp.Application.Common.Exceptions;

namespace EcommerceApp.API.Middleware;

// Catches anything that escapes the controllers, logs it, and turns it into a consistent
// ProblemDetails JSON response instead of leaking a stack trace to the client.
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                NotFoundAppException => HttpStatusCode.NotFound,
                ConflictAppException => HttpStatusCode.Conflict,
                UnauthorizedAppException => HttpStatusCode.Unauthorized,
                PaymentDeclinedAppException => HttpStatusCode.PaymentRequired,
                PaymentGatewayAppException => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning(ex, "{ExceptionType} while processing {Method} {Path}", ex.GetType().Name, context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                status = (int)statusCode,
                title = statusCode == HttpStatusCode.InternalServerError ? "An unexpected error occurred" : ex.Message,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
