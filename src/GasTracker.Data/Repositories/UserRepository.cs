using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Repositories;

public class UserRepository(GasTrackerDbContext context) : BaseRepository<AppUser>(context), IUserRepository
{
    public async Task<AppUser?> GetByGoogleSubjectIdAsync(string sub) =>
        await Context.AppUsers.FirstOrDefaultAsync(u => u.GoogleSubjectId == sub);
}
