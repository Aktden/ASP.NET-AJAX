using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AspNetApiNotes.Errors;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) =>
        _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception ex,
        CancellationToken ct)
    {
        _logger.LogError(ex, "Unhandled exception");

        var (status, title) = ex switch
        {
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Not Found"),

            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Bad Request"),

            UnauthorizedAccessException =>
                (StatusCodes.Status403Forbidden, "Forbidden"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message,
            Instance = ctx.Request.Path
        };

        pd.Extensions["traceId"] = ctx.TraceIdentifier;

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(pd, ct);

        return true;
    }
}
