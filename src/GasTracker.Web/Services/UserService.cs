using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;

namespace GasTracker.Web.Services;

public class UserService(IUnitOfWork uow, ILogger<UserService> logger)
{
    public async Task<AppUser> GetOrCreateAsync(string googleSubjectId, string email, string displayName)
    {
        var existing = await uow.Users.GetByGoogleSubjectIdAsync(googleSubjectId);
        if (existing is not null)
        {
            // Keep email/name in sync
            if (existing.Email != email || existing.DisplayName != displayName)
            {
                existing.Email = email;
                existing.DisplayName = displayName;
                await uow.Users.UpdateAsync(existing);
                await uow.SaveChangesAsync();
            }
            return existing;
        }

        logger.LogInformation("Creating new user for sub={Sub}", googleSubjectId);

        var user = new AppUser
        {
            GoogleSubjectId = googleSubjectId,
            Email = email,
            DisplayName = displayName
        };

        await uow.Users.AddAsync(user);
        await uow.SaveChangesAsync();
        return user;
    }
}
