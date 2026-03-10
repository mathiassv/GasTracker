using GasTracker.Data.Entities;

namespace GasTracker.Data.Interfaces;

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByGoogleSubjectIdAsync(string sub);
}
