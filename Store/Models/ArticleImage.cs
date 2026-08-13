using Store.Models.Common;

namespace Store.Models;

public class ArticleImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
