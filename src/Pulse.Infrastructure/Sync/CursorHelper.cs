using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Pulse.Infrastructure.Sync;

public sealed record CursorPayload(DateTimeOffset UpdatedAt, Guid Id);

public static class CursorHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Encode(DateTimeOffset updatedAt, Guid id)
    {
        var payload = new CursorPayload(updatedAt, id);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    public static bool TryDecode(string? cursor, out CursorPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            payload = JsonSerializer.Deserialize<CursorPayload>(json, JsonOptions);
            return payload != null;
        }
        catch
        {
            return false;
        }
    }
}
