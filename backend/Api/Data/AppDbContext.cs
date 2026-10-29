using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(user =>
        {
            user.Property(u => u.Username).HasMaxLength(AdminUser.UsernameMaxLength);
            user.HasIndex(u => u.Username).IsUnique();
        });
    }
}
