using Store.Models.Common;

namespace Store.Models;

public class Article : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<ArticleImage> Images { get; set; } =
        new List<ArticleImage>();
}
