using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Store.DTOs.Image;
using Store.Exceptions;

namespace Store.Services;

public class ImageService
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly Cloudinary _cloudinary;

    public ImageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Falta configurar Cloudinary en User Secrets.");
        }

        _cloudinary = new Cloudinary(
            new Account(cloudName, apiKey, apiSecret))
        {
            Api = { Secure = true }
        };
    }

    public async Task<ImageUploadResultDto> UploadAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new BusinessValidationException(
                "El archivo de imagen esta vacio.");

        if (file.Length > MaxFileSize)
            throw new BusinessValidationException(
                "La imagen no puede superar los 5 MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new BusinessValidationException(
                "Solo se permiten imagenes JPEG, PNG o WebP.");

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "store/articles",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new BusinessValidationException(
                $"Cloudinary no pudo subir la imagen: {result.Error.Message}");

        return new ImageUploadResultDto
        {
            Url = result.SecureUrl.ToString(),
            PublicId = result.PublicId
        };
    }

    public async Task DeleteAsync(string publicId)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Invalidate = true
        });

        if (result.Error != null)
            throw new BusinessValidationException(
                $"Cloudinary no pudo eliminar la imagen: {result.Error.Message}");
    }
}
