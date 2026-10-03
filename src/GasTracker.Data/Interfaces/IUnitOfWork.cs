namespace GasTracker.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ICarRepository Cars { get; }
    IFuelLogRepository FuelLogs { get; }
    Task<int> SaveChangesAsync();

    /// <summary>Stops tracking all pending changes, e.g. after a failed save.</summary>
    void DiscardChanges();
}
