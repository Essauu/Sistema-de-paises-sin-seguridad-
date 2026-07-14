using Microsoft.AspNetCore.Http;

namespace PaisApp.Services;

public interface IFileValidationService
{
    Task<FileValidationResult> ValidateImageAsync(IFormFile file);
    string GetSafeFileName(string originalFileName);
}

public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ContentType { get; set; }
}

public class FileValidationService : IFileValidationService
{
    private static readonly HashSet<string> TiposMimePermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private const long TamanoMaximoArchivo = 5 * 1024 * 1024; // 5MB

    public async Task<FileValidationResult> ValidateImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "No se ha seleccionado ningún archivo." };
        }

        if (file.Length > TamanoMaximoArchivo)
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "El archivo supera el tamaño máximo permitido (5MB)." };
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !ExtensionesPermitidas.Contains(extension))
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "Extensión de archivo no permitida. Use: .jpg, .png, .gif, .webp, .bmp" };
        }

        if (!TiposMimePermitidos.Contains(file.ContentType))
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "Tipo de contenido no válido." };
        }

        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        await stream.ReadExactlyAsync(buffer, 0, 8);

        if (!EsCabeceraImagenValida(buffer))
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "El archivo no es una imagen válida." };
        }

        stream.Position = 0;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (!EsContenidoImagenValido(bytes, file.ContentType))
        {
            return new FileValidationResult { IsValid = false, ErrorMessage = "El contenido del archivo no coincide con su tipo declarado." };
        }

        return new FileValidationResult 
        { 
            IsValid = true, 
            ContentType = file.ContentType 
        };
    }

    private static bool EsCabeceraImagenValida(byte[] header)
    {
        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // GIF: 47 49 46 38 (GIF87a/GIF89a)
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;
        // WebP: 52 49 46 46 (RIFF) ... 57 45 42 50 (WEBP)
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46) return true;
        // BMP: 42 4D
        if (header[0] == 0x42 && header[1] == 0x4D) return true;

        return false;
    }

    private static bool EsContenidoImagenValido(byte[] bytes, string contentType)
    {
        try
        {
            return contentType switch
            {
                "image/jpeg" => bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8,
                "image/png" => bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
                "image/gif" => bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38,
                "image/webp" => bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
                "image/bmp" => bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    public string GetSafeFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeName = Guid.NewGuid().ToString("N") + extension;
        return safeName;
    }
}
