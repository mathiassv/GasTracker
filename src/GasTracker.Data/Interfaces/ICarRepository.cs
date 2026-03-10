using GasTracker.Data.Entities;

namespace GasTracker.Data.Interfaces;

public interface ICarRepository : IRepository<Car>
{
    Task<IEnumerable<Car>> GetByUserIdAsync(int userId);
    Task<Car?> GetWithFuelLogsAsync(int carId, int userId);
}
