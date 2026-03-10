using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;
using GasTracker.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GasTracker.Services.Tests;

public class UserServiceTests
{
    private static UserService MakeSut(IUnitOfWork uow) =>
        new(uow, NullLogger<UserService>.Instance);

    [Fact]
    public async Task GetOrCreateAsync_ExistingUser_ReturnsSameUser()
    {
        var existing = new AppUser
        {
            Id = 1,
            GoogleSubjectId = "sub1",
            Email = "a@b.com",
            DisplayName = "Alice"
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByGoogleSubjectIdAsync("sub1")).ReturnsAsync(existing);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users).Returns(userRepo.Object);

        var result = await MakeSut(uow.Object).GetOrCreateAsync("sub1", "a@b.com", "Alice");

        Assert.Equal(1, result.Id);
        userRepo.Verify(r => r.AddAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_NewUser_CreatesAndSaves()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByGoogleSubjectIdAsync("sub_new")).ReturnsAsync((AppUser?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users).Returns(userRepo.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await MakeSut(uow.Object).GetOrCreateAsync("sub_new", "new@b.com", "Newbie");

        Assert.Equal("sub_new", result.GoogleSubjectId);
        Assert.Equal("new@b.com", result.Email);
        userRepo.Verify(r => r.AddAsync(It.IsAny<AppUser>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingUserEmailChanged_UpdatesEmailAndSaves()
    {
        var existing = new AppUser
        {
            Id = 1,
            GoogleSubjectId = "sub1",
            Email = "old@b.com",
            DisplayName = "Alice"
        };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByGoogleSubjectIdAsync("sub1")).ReturnsAsync(existing);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users).Returns(userRepo.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await MakeSut(uow.Object).GetOrCreateAsync("sub1", "new@b.com", "Alice");

        Assert.Equal("new@b.com", result.Email);
        userRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
