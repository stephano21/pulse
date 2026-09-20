using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Pulse.Application.Abstractions;

namespace Pulse.Infrastructure.Storage;

/// <summary>
/// Sube y firma URLs de archivos en cualquier storage que hable el protocolo S3 (Cloudflare R2,
/// Backblaze B2, MinIO, AWS S3...) — el proveedor concreto es solo config (.env), no código.
/// Pensado para bucket PRIVADO: nunca se guarda ni devuelve una URL pública fija, siempre se
/// firma al vuelo con expiración.
/// </summary>
public sealed class S3FileStorageService(IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions _opt = options.Value;

    public bool IsConfigured => _opt.IsConfigured;

    private AmazonS3Client CreateClient() => new(
        new BasicAWSCredentials(_opt.AccessKey, _opt.SecretKey),
        new AmazonS3Config
        {
            ServiceURL = _opt.ServiceUrl,
            ForcePathStyle = true, // requerido por la mayoría de los S3-compatibles que no son AWS
            AuthenticationRegion = _opt.Region
        });

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("El storage de archivos no está configurado (Storage:ServiceUrl / AccessKey / SecretKey / BucketName).");

        using var client = CreateClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _opt.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true // varios S3-compatibles no soportan streaming-signed-payload
        }, ct);
    }

    public string GetPresignedUrl(string key, TimeSpan? expiry = null)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("El storage de archivos no está configurado (Storage:ServiceUrl / AccessKey / SecretKey / BucketName).");

        using var client = CreateClient();
        return client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _opt.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(24))
        });
    }
}
