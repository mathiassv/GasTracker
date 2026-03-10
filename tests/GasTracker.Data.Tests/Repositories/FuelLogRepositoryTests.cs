using GasTracker.Data;
using GasTracker.Data.Entities;
using GasTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Tests.Repositories;

public class FuelLogRepositoryTests
{
    private static GasTrackerDbContext CreateContext(string name) =>
        new(new DbContextOptionsBuilder<GasTrackerDbContext>()
            .UseInMemoryDatabase(name).Options);

    private static async Task<(AppUser user1, AppUser user2, Car car1, Car car2)> SeedTwoUsersAsync(GasTrackerDbContext ctx)
    {
        var user1 = new AppUser { GoogleSubjectId = "sub1", Email = "a@b.com", DisplayName = "A" };
        var user2 = new AppUser { GoogleSubjectId = "sub2", Email = "b@b.com", DisplayName = "B" };
        ctx.AppUsers.AddRange(user1, user2);
        await ctx.SaveChangesAsync();

        var car1 = new Car { AppUserId = user1.Id, Name = "Car1" };
        var car2 = new Car { AppUserId = user2.Id, Name = "Car2" };
        ctx.Cars.AddRange(car1, car2);
        await ctx.SaveChangesAsync();

        return (user1, user2, car1, car2);
    }

    [Fact]
    public async Task GetByCarIdAsync_EnforcesUserIsolation()
    {
        using var ctx = CreateContext(nameof(GetByCarIdAsync_EnforcesUserIsolation));
        var (user1, user2, car1, _) = await SeedTwoUsersAsync(ctx);

        ctx.FuelLogs.Add(new FuelLog { CarId = car1.Id, OdometerReading = 1000, LitersFilled = 40, TotalCost = 60 });
        await ctx.SaveChangesAsync();

        var repo = new FuelLogRepository(ctx);

        // user2 cannot see user1's logs
        var logs = (await repo.GetByCarIdAsync(car1.Id, user2.Id)).ToList();
        Assert.Empty(logs);

        // user1 can see their own logs
        var ownLogs = (await repo.GetByCarIdAsync(car1.Id, user1.Id)).ToList();
        Assert.Single(ownLogs);
    }

    [Fact]
    public async Task GetByCarIdInRangeAsync_FiltersDateRange()
    {
        using var ctx = CreateContext(nameof(GetByCarIdInRangeAsync_FiltersDateRange));
        var (user1, _, car1, _) = await SeedTwoUsersAsync(ctx);

        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        ctx.FuelLogs.AddRange(
            new FuelLog { CarId = car1.Id, OdometerReading = 1000, LitersFilled = 40, TotalCost = 60, FilledAt = baseDate },
            new FuelLog { CarId = car1.Id, OdometerReading = 1400, LitersFilled = 35, TotalCost = 52, FilledAt = baseDate.AddDays(10) },
            new FuelLog { CarId = car1.Id, OdometerReading = 1750, LitersFilled = 30, TotalCost = 45, FilledAt = baseDate.AddDays(20) }
        );
        await ctx.SaveChangesAsync();

        var repo = new FuelLogRepository(ctx);
        var logs = (await repo.GetByCarIdInRangeAsync(car1.Id, user1.Id, baseDate.AddDays(5), baseDate.AddDays(15))).ToList();

        Assert.Single(logs);
        Assert.Equal(baseDate.AddDays(10), logs[0].FilledAt);
    }

    [Fact]
    public async Task GetPreviousLogAsync_ReturnsImmediatelyPrecedingLog()
    {
        using var ctx = CreateContext(nameof(GetPreviousLogAsync_ReturnsImmediatelyPrecedingLog));
        var (user1, _, car1, _) = await SeedTwoUsersAsync(ctx);

        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        ctx.FuelLogs.AddRange(
            new FuelLog { CarId = car1.Id, OdometerReading = 1000, LitersFilled = 40, TotalCost = 60, FilledAt = baseDate },
            new FuelLog { CarId = car1.Id, OdometerReading = 1400, LitersFilled = 35, TotalCost = 52, FilledAt = baseDate.AddDays(5) }
        );
        await ctx.SaveChangesAsync();

        var repo = new FuelLogRepository(ctx);
        var prev = await repo.GetPreviousLogAsync(car1.Id, baseDate.AddDays(5));

        Assert.NotNull(prev);
        Assert.Equal(1000, prev.OdometerReading);
    }

    [Fact]
    public async Task GetPreviousLogAsync_NoLogs_ReturnsNull()
    {
        using var ctx = CreateContext(nameof(GetPreviousLogAsync_NoLogs_ReturnsNull));
        var (_, _, car1, _) = await SeedTwoUsersAsync(ctx);

        var repo = new FuelLogRepository(ctx);
        var prev = await repo.GetPreviousLogAsync(car1.Id, DateTime.UtcNow);

        Assert.Null(prev);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFuelLog()
    {
        using var ctx = CreateContext(nameof(DeleteAsync_RemovesFuelLog));
        var (_, _, car1, _) = await SeedTwoUsersAsync(ctx);

        var log = new FuelLog { CarId = car1.Id, OdometerReading = 1000, LitersFilled = 40, TotalCost = 60 };
        ctx.FuelLogs.Add(log);
        await ctx.SaveChangesAsync();

        var repo = new FuelLogRepository(ctx);
        await repo.DeleteAsync(log.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(log.Id);
        Assert.Null(found);
    }
}
