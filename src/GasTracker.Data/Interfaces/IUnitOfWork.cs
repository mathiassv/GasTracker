namespace GasTracker.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICarRepository Cars { get; }
    IFuelLogRepository FuelLogs { get; }
    Task<int> SaveChangesAsync();
}
