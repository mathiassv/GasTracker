using GasTracker.Data.Interfaces;
using GasTracker.Data.Repositories;

namespace GasTracker.Data;

public class UnitOfWork(GasTrackerDbContext context) : IUnitOfWork
{
    private IUserRepository? _users;
    private ICarRepository? _cars;
    private IFuelLogRepository? _fuelLogs;

    public IUserRepository Users => _users ??= new UserRepository(context);
    public ICarRepository Cars => _cars ??= new CarRepository(context);
    public IFuelLogRepository FuelLogs => _fuelLogs ??= new FuelLogRepository(context);

    public Task<int> SaveChangesAsync() => context.SaveChangesAsync();

    public void Dispose() => context.Dispose();
}
