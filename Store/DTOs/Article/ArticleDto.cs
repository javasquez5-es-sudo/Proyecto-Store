namespace Store.DTOs.Article;

public class ArticleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public List<ArticleImageDto> Images { get; set; } = new();
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
