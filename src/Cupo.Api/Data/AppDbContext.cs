using Cupo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cupo.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Capacity).IsRequired();
            e.Property(x => x.HeldCount).HasDefaultValue(0);
            e.Property(x => x.ConfirmedCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<Hold>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Quantity).IsRequired();
            e.HasIndex(x => x.EventId);
            e.HasIndex(x => x.UserId);
        });
    }
}