namespace Pulse.Application.Abstractions;

public sealed record RefreshTokenIssued(string RawToken, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenValidationResult(
    bool Success,
    Guid? TenantId,
    Guid? UserId,
    string? NewRawToken,
    DateTimeOffset? NewExpiresAt,
    string? FailureReason);

public interface IRefreshTokenService
{
    /// <param name="lifetimeDays">Vida útil del token emitido; la política real (<c>Jwt:RefreshTokenLifetimeDays</c>) vive en Pulse.Api.</param>
    Task<RefreshTokenIssued> IssueAsync(Guid tenantId, Guid userId, int lifetimeDays, string? createdByIp, CancellationToken ct);

    /// <summary>Valida el refresh token y, si es válido, lo rota (revoca el actual y emite uno nuevo con la misma vida útil).</summary>
    Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string rawToken, int lifetimeDays, string? requestIp, CancellationToken ct);

    /// <summary>Revoca el token si existe; no falla si ya estaba revocado o no existe (logout idempotente).</summary>
    Task RevokeAsync(string rawToken, CancellationToken ct);
}
