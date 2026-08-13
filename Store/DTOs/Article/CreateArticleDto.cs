using System.ComponentModel.DataAnnotations;

namespace Store.DTOs.Article;

public class CreateArticleDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 9999999999999999.99,
        ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Price { get; set; }

    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Debes seleccionar al menos una imagen.")]
    public List<IFormFile> Images { get; set; } = new();
}
