using Microsoft.EntityFrameworkCore;
using Store.Data;
using Store.DTOs.Article;
using Store.DTOs.Common;
using Store.DTOs.Image;
using Store.Exceptions;

namespace Store.Services;

public class ArticleService(AppDbContext db, ImageService imageService)
{
    public async Task<PagedResultDto<ArticleDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        Guid? userId)
    {
        var query = db.Articles
            .AsNoTracking()
            .Where(article => article.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(article =>
                article.Name.ToLower().Contains(normalizedSearch) ||
                (article.Description != null &&
                 article.Description.ToLower().Contains(normalizedSearch)));
        }

        if (userId.HasValue)
            query = query.Where(article => article.UserId == userId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(article => article.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(article => new ArticleDto
            {
                Id = article.Id,
                Name = article.Name,
                Description = article.Description,
                Price = article.Price,
                Images = article.Images
                    .Where(image => image.DeletedAt == null)
                    .Select(image => new ArticleImageDto
                    {
                        Id = image.Id,
                        Url = image.Url
                    })
                    .ToList(),
                UserId = article.UserId,
                UserName = article.User.Name
            })
            .ToListAsync();

        return new PagedResultDto<ArticleDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<ArticleDto?> GetByIdAsync(Guid id)
    {
        return await db.Articles
            .AsNoTracking()
            .Where(article => article.Id == id && article.DeletedAt == null)
            .Select(article => new ArticleDto
            {
                Id = article.Id,
                Name = article.Name,
                Description = article.Description,
                Price = article.Price,
                Images = article.Images
                    .Where(image => image.DeletedAt == null)
                    .Select(image => new ArticleImageDto
                    {
                        Id = image.Id,
                        Url = image.Url
                    })
                    .ToList(),
                UserId = article.UserId,
                UserName = article.User.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto dto)
    {
        await EnsureUserExistsAsync(dto.UserId);

        if (dto.Images.Count == 0)
            throw new BusinessValidationException(
                "Debes seleccionar al menos una imagen.");

        var uploadedImages = new List<ImageUploadResultDto>();

        try
        {
            foreach (var file in dto.Images)
                uploadedImages.Add(await imageService.UploadAsync(file));

            var article = new Models.Article
            {
                Name = dto.Name.Trim(),
                Description = NormalizeOptionalText(dto.Description),
                Price = dto.Price,
                UserId = dto.UserId,
                Images = uploadedImages.Select(image => new Models.ArticleImage
                {
                    Url = image.Url,
                    PublicId = image.PublicId
                }).ToList()
            };

            db.Articles.Add(article);
            await db.SaveChangesAsync();
            await db.Entry(article).Reference(item => item.User).LoadAsync();

            return ToDto(article);
        }
        catch
        {
            foreach (var image in uploadedImages)
            {
                try
                {
                    await imageService.DeleteAsync(image.PublicId);
                }
                catch
                {
                    // Preserve the original failure while attempting cleanup.
                }
            }

            throw;
        }
    }

    public async Task<ArticleDto?> UpdateAsync(
        Guid id,
        UpdateArticleDto dto)
    {
        var article = await db.Articles
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item =>
                item.Id == id && item.DeletedAt == null);

        if (article == null)
            return null;

        await EnsureUserExistsAsync(dto.UserId);

        article.Name = dto.Name.Trim();
        article.Description = NormalizeOptionalText(dto.Description);
        article.Price = dto.Price;
        article.UserId = dto.UserId;
        article.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(article).Reference(item => item.User).LoadAsync();

        return ToDto(article);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var article = await db.Articles
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item =>
                item.Id == id && item.DeletedAt == null);

        if (article == null)
            return false;

        foreach (var image in article.Images.Where(image =>
                     image.DeletedAt == null))
        {
            await imageService.DeleteAsync(image.PublicId);
            image.DeletedAt = DateTime.UtcNow;
        }

        article.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        var exists = await db.Users.AnyAsync(user =>
            user.Id == userId && user.DeletedAt == null);

        if (!exists)
            throw new BusinessValidationException(
                "El usuario indicado no existe o esta eliminado.");
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ArticleDto ToDto(Models.Article article) => new()
    {
        Id = article.Id,
        Name = article.Name,
        Description = article.Description,
        Price = article.Price,
        Images = article.Images
            .Where(image => image.DeletedAt == null)
            .Select(image => new ArticleImageDto
            {
                Id = image.Id,
                Url = image.Url
            })
            .ToList(),
        UserId = article.UserId,
        UserName = article.User.Name
    };
}
