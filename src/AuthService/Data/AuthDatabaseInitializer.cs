using AuthService.Models;
using ECommerce.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public static class AuthDatabaseInitializer
{
    private const string LegacyPlaceholderHash = "$2a$11$w1RfZwK1V2h59/XWn3p00e57sVz2K4mFwA8zQyKjUaR3sJv9P4cGi";

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync(
                """SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Roles"), 1));""");
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var existingRoles = await db.Roles.ToListAsync();
        var existingNames = existingRoles.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in RoleNames.All)
        {
            if (!existingNames.Contains(roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }

        await db.SaveChangesAsync();

        var allRoles = await db.Roles.ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var admin = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "admin@ecommerce.com");

        if (admin == null)
        {
            admin = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@ecommerce.com",
                DisplayName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            if (allRoles.TryGetValue(RoleNames.User, out var userRole))
            {
                admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = userRole.Id });
            }

            if (allRoles.TryGetValue(RoleNames.Admin, out var adminRole))
            {
                admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            }

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        else if (admin.PasswordHash == LegacyPlaceholderHash)
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            await db.SaveChangesAsync();
        }
    }
}
