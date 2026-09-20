namespace Pulse.Application.Admin;

public sealed record AdminTenantDto(Guid Id, string Name, DateTimeOffset CreatedAt, int UserCount, string? NotificationEmail, string? LogoUrl);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    Guid? TenantId,
    string? TenantName,
    IReadOnlyList<string> Roles,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    bool Active);
