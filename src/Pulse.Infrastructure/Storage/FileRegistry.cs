using Microsoft.EntityFrameworkCore;
using Pulse.Application.Abstractions;
using Pulse.Domain;
using Pulse.Infrastructure.Data;

namespace Pulse.Infrastructure.Storage;

public sealed class FileRegistry(PulseDbContext db) : IFileRegistry
{
    public async Task RegisterAsync(Guid id, Guid? tenantId, Guid uploadedByUserId, string key, string contentType, long sizeBytes, CancellationToken ct)
    {
        db.Files.Add(new StoredFile
        {
            Id = id,
            Key = key,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            TenantId = tenantId,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<StoredFileInfo?> GetAsync(Guid fileId, CancellationToken ct)
    {
        return await db.Files
            .Where(f => f.Id == fileId)
            .Select(f => new StoredFileInfo(f.Id, f.Key, f.ContentType, f.TenantId, f.UploadedByUserId))
            .FirstOrDefaultAsync(ct);
    }
}
