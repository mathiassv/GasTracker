using GasTracker.Data.Entities;

namespace GasTracker.Data.Interfaces;

public interface IFuelLogRepository : IRepository<FuelLog>
{
    Task<IEnumerable<FuelLog>> GetByCarIdAsync(int carId, int userId);
    Task<IEnumerable<FuelLog>> GetByCarIdInRangeAsync(int carId, int userId, DateTime from, DateTime to);
    Task<FuelLog?> GetPreviousLogAsync(int carId, DateTime before);
}
