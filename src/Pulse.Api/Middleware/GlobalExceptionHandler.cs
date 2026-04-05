using Microsoft.AspNetCore.Diagnostics;

namespace Pulse.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is InvalidOperationException ex)
        {
            log.LogWarning(ex, "Error de negocio");
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return true;
        }

        return false;
    }
}
