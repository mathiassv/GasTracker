using GasTracker.Data.Entities;

namespace GasTracker.Web.Services;

public class FuelCalculatorService(ILogger<FuelCalculatorService> logger)
{
    /// <summary>Distance since previous fill-up in km, or null if first entry.</summary>
    public decimal? DistanceSincePrevious(FuelLog current, FuelLog? previous)
    {
        if (previous is null) return null;
        var distance = current.OdometerReading - previous.OdometerReading;
        if (distance <= 0)
        {
            logger.LogWarning("Non-positive distance computed for log {Id}", current.Id);
            return null;
        }
        return distance;
    }

    /// <summary>Liters per 100 km.</summary>
    public decimal? LitersPer100Km(decimal litersFilled, decimal distanceKm)
    {
        if (distanceKm <= 0) return null;
        return litersFilled / distanceKm * 100m;
    }

    /// <summary>Cost per km in user's currency.</summary>
    public decimal? CostPerKm(decimal totalCost, decimal distanceKm)
    {
        if (distanceKm <= 0) return null;
        return totalCost / distanceKm;
    }

    /// <summary>Miles per gallon (US). distanceMiles and gallonsUS must be positive.</summary>
    public decimal? MilesPerGallon(decimal distanceMiles, decimal gallonsUS)
    {
        if (gallonsUS <= 0) return null;
        return distanceMiles / gallonsUS;
    }

    /// <summary>Cost per mile in user's currency.</summary>
    public decimal? CostPerMile(decimal totalCost, decimal distanceMiles)
    {
        if (distanceMiles <= 0) return null;
        return totalCost / distanceMiles;
    }
}
