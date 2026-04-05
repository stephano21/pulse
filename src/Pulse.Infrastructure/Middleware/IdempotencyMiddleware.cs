using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Domain;
using Pulse.Infrastructure.Data;

namespace Pulse.Infrastructure.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    private static readonly Regex VersionedSyncPath = new(
        @"^/v\d+(?:\.\d+)?/sync(?:/|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        var path = context.Request.Path.Value ?? "";
        if (context.Request.Method != HttpMethods.Post || !VersionedSyncPath.IsMatch(path))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            await next(context);
            return;
        }

        var key = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next(context);
            return;
        }

        var tenantId = ResolveTenantId(context);
        if (tenantId == null)
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        var requestHash = Convert.ToHexString(hashBytes);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var existing = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Key == key && x.RequestPath == path);

        if (existing != null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""{"error":"idempotency_key_mismatch"}""", context.RequestAborted);
                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next(context);

        buffer.Position = 0;
        var responseText = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;

        if (context.Response.StatusCode is >= 200 and < 300 && responseText.Length > 0)
        {
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Key = key,
                RequestPath = path,
                RequestHash = requestHash,
                ResponseBody = responseText,
                StatusCode = context.Response.StatusCode,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(context.RequestAborted);
        }
    }

    private static Guid? ResolveTenantId(HttpContext context)
    {
        var tid = context.User.FindFirst("tenant_id")?.Value;
        if (tid != null && Guid.TryParse(tid, out var g)) return g;

        return null;
    }
}
