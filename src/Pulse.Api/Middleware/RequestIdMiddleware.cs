namespace Pulse.Api.Middleware;

public sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> log)
{
    public const string HttpContextItemKey = "RequestId";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers.TryGetValue("X-Request-Id", out var h) && !string.IsNullOrWhiteSpace(h)
            ? h.ToString()
            : Guid.NewGuid().ToString("N");
        context.Items[HttpContextItemKey] = id;
        context.Response.Headers.TryAdd("X-Request-Id", id);

        using (log.BeginScope(new Dictionary<string, object> { ["request_id"] = id }))
        {
            await next(context);
        }
    }
}
