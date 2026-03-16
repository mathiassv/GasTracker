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

    /// <summary>
    /// For a full fill-up at <paramref name="index"/> in <paramref name="logs"/> (ordered ascending
    /// by FilledAt), sums fuel and cost back to the previous full fill-up and returns the totals
    /// together with the distance driven since that anchor.
    /// Returns null when: the log is a partial fill-up; there is no previous full fill-up to anchor
    /// distance against; or the computed distance is non-positive.
    /// </summary>
    public (decimal TotalLiters, decimal DistanceKm, decimal TotalCost)? AccumulatedStats(
        IList<FuelLog> logs, int index)
    {
        if (index < 0 || index >= logs.Count) return null;
        var current = logs[index];
        if (current.IsPartialFillUp) return null;

        // Walk backwards to find the previous full fill-up (the anchor)
        int anchorIndex = -1;
        for (int i = index - 1; i >= 0; i--)
        {
            if (!logs[i].IsPartialFillUp)
            {
                anchorIndex = i;
                break;
            }
        }

        // No previous full fill-up → no reliable baseline
        if (anchorIndex == -1) return null;

        // Sum fuel and cost for this run: every log from anchor+1 up to and including current
        decimal totalLiters = 0;
        decimal totalCost = 0;
        for (int i = anchorIndex + 1; i <= index; i++)
        {
            totalLiters += logs[i].LitersFilled;
            totalCost += logs[i].TotalCost;
        }

        var distanceKm = current.OdometerReading - logs[anchorIndex].OdometerReading;
        if (distanceKm <= 0)
        {
            logger.LogWarning("Non-positive distance computed for log {Id}", current.Id);
            return null;
        }

        return (totalLiters, distanceKm, totalCost);
    }
}
