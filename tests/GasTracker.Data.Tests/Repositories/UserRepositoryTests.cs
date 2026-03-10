using GasTracker.Data;
using GasTracker.Data.Entities;
using GasTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Tests.Repositories;

public class UserRepositoryTests
{
    private static GasTrackerDbContext CreateContext(string name) =>
        new(new DbContextOptionsBuilder<GasTrackerDbContext>()
            .UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsUser()
    {
        using var ctx = CreateContext(nameof(AddAsync_And_GetByIdAsync_ReturnsUser));
        var repo = new UserRepository(ctx);

        var user = new AppUser { GoogleSubjectId = "sub1", Email = "a@b.com", DisplayName = "Alice" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        Assert.NotNull(found);
        Assert.Equal("a@b.com", found.Email);
    }

    [Fact]
    public async Task GetByGoogleSubjectIdAsync_ReturnsCorrectUser()
    {
        using var ctx = CreateContext(nameof(GetByGoogleSubjectIdAsync_ReturnsCorrectUser));
        var repo = new UserRepository(ctx);

        await repo.AddAsync(new AppUser { GoogleSubjectId = "sub1", Email = "a@b.com", DisplayName = "Alice" });
        await repo.AddAsync(new AppUser { GoogleSubjectId = "sub2", Email = "b@b.com", DisplayName = "Bob" });
        await ctx.SaveChangesAsync();

        var user = await repo.GetByGoogleSubjectIdAsync("sub2");
        Assert.NotNull(user);
        Assert.Equal("b@b.com", user.Email);
    }

    [Fact]
    public async Task GetByGoogleSubjectIdAsync_UnknownSub_ReturnsNull()
    {
        using var ctx = CreateContext(nameof(GetByGoogleSubjectIdAsync_UnknownSub_ReturnsNull));
        var repo = new UserRepository(ctx);

        var result = await repo.GetByGoogleSubjectIdAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        using var ctx = CreateContext(nameof(UpdateAsync_PersistsChanges));
        var repo = new UserRepository(ctx);

        var user = new AppUser { GoogleSubjectId = "sub1", Email = "a@b.com", DisplayName = "Alice" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        user.CurrencyCode = "EUR";
        await repo.UpdateAsync(user);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        Assert.Equal("EUR", found!.CurrencyCode);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        using var ctx = CreateContext(nameof(DeleteAsync_RemovesUser));
        var repo = new UserRepository(ctx);

        var user = new AppUser { GoogleSubjectId = "sub1", Email = "a@b.com", DisplayName = "Alice" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(user.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        Assert.Null(found);
    }
}
