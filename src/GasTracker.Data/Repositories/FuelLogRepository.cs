using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Data.Repositories;

public class FuelLogRepository(GasTrackerDbContext context) : BaseRepository<FuelLog>(context), IFuelLogRepository
{
    public async Task<IEnumerable<FuelLog>> GetByCarIdAsync(int carId, int userId) =>
        await Context.FuelLogs
            .Where(f => f.CarId == carId && f.Car.AppUserId == userId)
            .OrderBy(f => f.FilledAt)
            .ToListAsync();

    public async Task<IEnumerable<FuelLog>> GetByCarIdInRangeAsync(int carId, int userId, DateTime from, DateTime to) =>
        await Context.FuelLogs
            .Where(f => f.CarId == carId && f.Car.AppUserId == userId
                && f.FilledAt >= from && f.FilledAt <= to)
            .OrderBy(f => f.FilledAt)
            .ToListAsync();

    public async Task<FuelLog?> GetPreviousLogAsync(int carId, int userId, DateTime before) =>
        await Context.FuelLogs
            .Where(f => f.CarId == carId && f.Car.AppUserId == userId && f.FilledAt < before)
            .OrderByDescending(f => f.FilledAt)
            .FirstOrDefaultAsync();
}
