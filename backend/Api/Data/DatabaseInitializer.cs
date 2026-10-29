using Api.Configuration;
using Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Data;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and creates the admin account if none exists yet.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (await db.AdminUsers.AnyAsync())
            return;

        var options = services.GetRequiredService<IOptions<AdminOptions>>().Value;
        var admin = new AdminUser { Username = options.Username };
        admin.PasswordHash = services.GetRequiredService<IPasswordHasher<AdminUser>>().HashPassword(admin, options.Password);

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
    }
}
