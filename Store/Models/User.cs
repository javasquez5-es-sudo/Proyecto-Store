using Store.Models.Common;

namespace Store.Models;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
