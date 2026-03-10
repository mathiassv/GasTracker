using GasTracker.Data;
using GasTracker.Data.Entities;
using GasTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Tests.Repositories;

public class CarRepositoryTests
{
    private static GasTrackerDbContext CreateContext(string name) =>
        new(new DbContextOptionsBuilder<GasTrackerDbContext>()
            .UseInMemoryDatabase(name).Options);

    private static AppUser MakeUser(string sub, string email) =>
        new() { GoogleSubjectId = sub, Email = email, DisplayName = email };

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUsersCars()
    {
        using var ctx = CreateContext(nameof(GetByUserIdAsync_ReturnsOnlyUsersCars));
        var user1 = MakeUser("sub1", "a@b.com");
        var user2 = MakeUser("sub2", "b@b.com");
        ctx.AppUsers.AddRange(user1, user2);
        await ctx.SaveChangesAsync();

        ctx.Cars.AddRange(
            new Car { AppUserId = user1.Id, Name = "Car A" },
            new Car { AppUserId = user1.Id, Name = "Car B" },
            new Car { AppUserId = user2.Id, Name = "Car C" }
        );
        await ctx.SaveChangesAsync();

        var repo = new CarRepository(ctx);
        var cars = (await repo.GetByUserIdAsync(user1.Id)).ToList();

        Assert.Equal(2, cars.Count);
        Assert.All(cars, c => Assert.Equal(user1.Id, c.AppUserId));
    }

    [Fact]
    public async Task GetWithFuelLogsAsync_EnforcesUserIsolation()
    {
        using var ctx = CreateContext(nameof(GetWithFuelLogsAsync_EnforcesUserIsolation));
        var user1 = MakeUser("sub1", "a@b.com");
        var user2 = MakeUser("sub2", "b@b.com");
        ctx.AppUsers.AddRange(user1, user2);
        await ctx.SaveChangesAsync();

        var car = new Car { AppUserId = user1.Id, Name = "Car A" };
        ctx.Cars.Add(car);
        await ctx.SaveChangesAsync();

        var repo = new CarRepository(ctx);

        // User2 cannot access user1's car
        var result = await repo.GetWithFuelLogsAsync(car.Id, user2.Id);
        Assert.Null(result);

        // User1 can access their own car
        var ownCar = await repo.GetWithFuelLogsAsync(car.Id, user1.Id);
        Assert.NotNull(ownCar);
    }

    [Fact]
    public async Task GetWithFuelLogsAsync_IncludesFuelLogs()
    {
        using var ctx = CreateContext(nameof(GetWithFuelLogsAsync_IncludesFuelLogs));
        var user = MakeUser("sub1", "a@b.com");
        ctx.AppUsers.Add(user);
        await ctx.SaveChangesAsync();

        var car = new Car { AppUserId = user.Id, Name = "My Car" };
        ctx.Cars.Add(car);
        await ctx.SaveChangesAsync();

        ctx.FuelLogs.AddRange(
            new FuelLog { CarId = car.Id, OdometerReading = 1000, LitersFilled = 40, TotalCost = 60, FilledAt = DateTime.UtcNow.AddDays(-2) },
            new FuelLog { CarId = car.Id, OdometerReading = 1400, LitersFilled = 35, TotalCost = 52, FilledAt = DateTime.UtcNow.AddDays(-1) }
        );
        await ctx.SaveChangesAsync();

        var repo = new CarRepository(ctx);
        var result = await repo.GetWithFuelLogsAsync(car.Id, user.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.FuelLogs.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCar()
    {
        using var ctx = CreateContext(nameof(DeleteAsync_RemovesCar));
        var user = MakeUser("sub1", "a@b.com");
        ctx.AppUsers.Add(user);
        await ctx.SaveChangesAsync();

        var car = new Car { AppUserId = user.Id, Name = "Car A" };
        ctx.Cars.Add(car);
        await ctx.SaveChangesAsync();

        var repo = new CarRepository(ctx);
        await repo.DeleteAsync(car.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(car.Id);
        Assert.Null(found);
    }
}
