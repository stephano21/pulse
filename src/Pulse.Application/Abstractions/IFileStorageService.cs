namespace Pulse.Application.Abstractions;

public interface IFileStorageService
{
    bool IsConfigured { get; }

    /// <summary>Sube un archivo. Lanza InvalidOperationException si el storage no está configurado.</summary>
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct);

    /// <summary>
    /// URL firmada temporal para leer un objeto de un bucket privado (no guardamos URLs fijas:
    /// el bucket no es público). Sin llamada de red — es solo firmado local (síncrono).
    /// </summary>
    string GetPresignedUrl(string key, TimeSpan? expiry = null);
}
