using Microsoft.AspNetCore.Identity;

namespace Pulse.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
}
