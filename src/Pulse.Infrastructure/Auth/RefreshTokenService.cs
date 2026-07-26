using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Pulse.Application.Abstractions;
using Pulse.Domain;
using Pulse.Infrastructure.Data;

namespace Pulse.Infrastructure.Auth;

/// <summary>
/// Refresh tokens rotativos y de un solo uso: cada rotación exitosa revoca el token presentado
/// y emite uno nuevo. Solo se persiste el hash SHA-256 del valor crudo; el valor crudo se entrega
/// una única vez al cliente y nunca se puede recuperar desde la base de datos.
/// </summary>
public sealed class RefreshTokenService(PulseDbContext db) : IRefreshTokenService
{
    public async Task<RefreshTokenIssued> IssueAsync(Guid tenantId, Guid userId, int lifetimeDays, string? createdByIp, CancellationToken ct)
    {
        var (raw, hash) = GenerateTokenPair();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(lifetimeDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp
        });
        await db.SaveChangesAsync(ct);

        return new RefreshTokenIssued(raw, expiresAt);
    }

    public async Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string rawToken, int lifetimeDays, string? requestIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return new RefreshTokenValidationResult(false, null, null, null, null, "empty_token");

        var hash = HashToken(rawToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (existing is null)
            return new RefreshTokenValidationResult(false, null, null, null, null, "not_found");

        if (existing.RevokedAt is not null)
            return new RefreshTokenValidationResult(false, null, null, null, null, "revoked_or_reused");

        if (existing.ExpiresAt < DateTimeOffset.UtcNow)
            return new RefreshTokenValidationResult(false, null, null, null, null, "expired");

        var (newRaw, newHash) = GenerateTokenPair();
        var now = DateTimeOffset.UtcNow;
        var newExpiresAt = now.AddDays(lifetimeDays);

        existing.RevokedAt = now;
        existing.ReplacedByTokenHash = newHash;

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = existing.TenantId,
            UserId = existing.UserId,
            TokenHash = newHash,
            CreatedAt = now,
            ExpiresAt = newExpiresAt,
            CreatedByIp = requestIp
        });
        await db.SaveChangesAsync(ct);

        return new RefreshTokenValidationResult(true, existing.TenantId, existing.UserId, newRaw, newExpiresAt, null);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;

        var hash = HashToken(rawToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (existing is null || existing.RevokedAt is not null) return;

        existing.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static (string Raw, string Hash) GenerateTokenPair()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (raw, HashToken(raw));
    }

    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
