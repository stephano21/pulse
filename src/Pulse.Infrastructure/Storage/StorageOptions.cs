namespace Pulse.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Endpoint S3-compatible (Cloudflare R2, Backblaze B2, MinIO, AWS S3, etc.). Vacío = deshabilitado.</summary>
    public string ServiceUrl { get; set; } = "";

    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "";

    /// <summary>Región del proveedor. La mayoría de los S3-compatibles no la usan de verdad; "auto" sirve para R2.</summary>
    public string Region { get; set; } = "auto";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(BucketName);
}
