using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Store.Data;
using Store.DTOs.Common;
using Store.DTOs.User;
using Store.Exceptions;

namespace Store.Services;

public class UserService(AppDbContext db, IConfiguration configuration)
{
    public async Task<PagedResultDto<UserDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search)
    {
        var query = db.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(user =>
                user.Name.ToLower().Contains(normalizedSearch) ||
                user.Username.ToLower().Contains(normalizedSearch));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(user => user.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username
            })
            .ToListAsync();

        return new PagedResultDto<UserDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        return await db.Users
            .AsNoTracking()
            .Where(user => user.Id == id && user.DeletedAt == null)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var username = NormalizeUsername(dto.Username);
        await EnsureUsernameIsAvailableAsync(username);

        var user = new Models.User
        {
            Name = dto.Name.Trim(),
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return ToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(user =>
            user.Id == id && user.DeletedAt == null);

        if (user == null)
            return null;

        var username = NormalizeUsername(dto.Username);
        await EnsureUsernameIsAvailableAsync(username, id);

        user.Name = dto.Name.Trim();
        user.Username = username;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(user =>
            user.Id == id && user.DeletedAt == null);

        if (user == null)
            return false;

        user.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var username = NormalizeUsername(dto.Username);
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Username == username &&
                user.DeletedAt == null);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "No se configuro la clave JWT.");

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            Id = user.Id,
            Name = user.Name,
            Username = user.Username
        };
    }

    private async Task EnsureUsernameIsAvailableAsync(
        string username,
        Guid? excludedId = null)
    {
        var exists = await db.Users.AnyAsync(user =>
            user.Username == username &&
            user.DeletedAt == null &&
            (!excludedId.HasValue || user.Id != excludedId.Value));

        if (exists)
            throw new BusinessValidationException(
                "Ya existe un usuario activo con ese nombre de usuario.");
    }

    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    private static UserDto ToDto(Models.User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Username = user.Username
    };
}
