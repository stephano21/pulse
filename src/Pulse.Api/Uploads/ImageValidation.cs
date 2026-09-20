namespace Pulse.Api.Uploads;

public static class ImageValidation
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>Null = válido; si no, el mensaje de error para mostrar.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Archivo requerido.";
        if (file.Length > MaxSizeBytes)
            return "El archivo no puede superar 5 MB.";
        if (!AllowedContentTypes.Contains(file.ContentType))
            return "Formato no soportado (usá JPG, PNG o WEBP).";
        return null;
    }

    public static string ExtensionFor(IFormFile file) => file.ContentType.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };
}
