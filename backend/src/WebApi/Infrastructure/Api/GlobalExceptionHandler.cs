using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Infrastructure.Api;

internal sealed class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                exception,
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogWarning(exception, "Request was cancelled.");

            await WriteProblemDetailsAsync(
                context,
                exception,
                StatusCodes.Status499ClientClosedRequest,
                "Request cancelled",
                "The request was cancelled.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception occurred.");

            await WriteProblemDetailsAsync(
                context,
                exception,
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred while processing the request.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails);

        await context.Response.WriteAsync(json);
    }
}