using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Repositories;

public class CarRepository(GasTrackerDbContext context) : BaseRepository<Car>(context), ICarRepository
{
    public async Task<IEnumerable<Car>> GetByUserIdAsync(int userId) =>
        await Context.Cars
            .Where(c => c.AppUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Car?> GetWithFuelLogsAsync(int carId, int userId) =>
        await Context.Cars
            .Include(c => c.FuelLogs.OrderBy(f => f.FilledAt))
            .FirstOrDefaultAsync(c => c.Id == carId && c.AppUserId == userId);
}
