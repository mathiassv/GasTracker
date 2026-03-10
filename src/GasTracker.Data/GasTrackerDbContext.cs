using GasTracker.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data;

public class GasTrackerDbContext(DbContextOptions<GasTrackerDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.GoogleSubjectId).IsUnique();
            e.Property(u => u.GoogleSubjectId).IsRequired();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.DisplayName).IsRequired();
        });

        modelBuilder.Entity<Car>(e =>
        {
            e.HasOne(c => c.AppUser)
             .WithMany(u => u.Cars)
             .HasForeignKey(c => c.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FuelLog>(e =>
        {
            e.HasIndex(f => new { f.CarId, f.FilledAt });
            e.HasOne(f => f.Car)
             .WithMany(c => c.FuelLogs)
             .HasForeignKey(f => f.CarId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
