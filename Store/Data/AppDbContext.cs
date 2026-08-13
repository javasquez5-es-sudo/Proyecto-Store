using Microsoft.EntityFrameworkCore;
using Store.Models;

namespace Store.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleImage> ArticleImages => Set<ArticleImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        modelBuilder.Entity<Article>()
            .Property(article => article.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ArticleImage>()
            .HasIndex(image => image.PublicId)
            .IsUnique();
    }
}
