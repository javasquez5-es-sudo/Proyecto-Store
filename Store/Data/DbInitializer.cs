using Microsoft.EntityFrameworkCore;

namespace Store.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hasActiveUsers = await db.Users.AnyAsync(user =>
            user.DeletedAt == null);

        if (hasActiveUsers)
            return;

        var username = configuration["BootstrapAdmin:Username"]
            ?? throw new InvalidOperationException(
                "No se configuro BootstrapAdmin:Username.");
        var password = configuration["BootstrapAdmin:Password"]
            ?? throw new InvalidOperationException(
                "No se configuro BootstrapAdmin:Password en User Secrets.");

        db.Users.Add(new Models.User
        {
            Name = "Administrador",
            Username = username.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(password)
        });

        await db.SaveChangesAsync();
    }
}
