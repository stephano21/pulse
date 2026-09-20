namespace Pulse.Application.Abstractions;

public sealed record StoredFileInfo(Guid Id, string Key, string ContentType, Guid? TenantId, Guid UploadedByUserId);

/// <summary>
/// Metadatos de archivos subidos (tabla genérica, sin relación fija a ningún tipo de entidad).
/// El contenido en sí vive en <see cref="IFileStorageService"/>; esto es solo el registro.
/// </summary>
public interface IFileRegistry
{
    Task RegisterAsync(Guid id, Guid? tenantId, Guid uploadedByUserId, string key, string contentType, long sizeBytes, CancellationToken ct);

    Task<StoredFileInfo?> GetAsync(Guid fileId, CancellationToken ct);
}
