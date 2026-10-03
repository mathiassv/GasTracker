using GasTracker.Data.Entities;
using GasTracker.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GasTracker.Services.Tests;

public class FuelCalculatorServiceTests
{
    private static FuelCalculatorService Sut() => new(NullLogger<FuelCalculatorService>.Instance);

    private static FuelLog MakeLog(decimal odometer, decimal liters, decimal cost, DateTime? filledAt = null) =>
        new() { OdometerReading = odometer, LitersFilled = liters, TotalCost = cost, FilledAt = filledAt ?? DateTime.UtcNow };

    private static FuelLog MakePartial(decimal odometer, decimal liters, decimal cost) =>
        new() { OdometerReading = odometer, LitersFilled = liters, TotalCost = cost, FilledAt = DateTime.UtcNow, IsPartialFillUp = true };

    [Fact]
    public void DistanceSincePrevious_NoPrevious_ReturnsNull()
    {
        var result = Sut().DistanceSincePrevious(MakeLog(1400, 35, 52), null);
        Assert.Null(result);
    }

    [Fact]
    public void DistanceSincePrevious_WithPrevious_ReturnsDistance()
    {
        var prev = MakeLog(1000, 40, 60);
        var curr = MakeLog(1400, 35, 52);
        var result = Sut().DistanceSincePrevious(curr, prev);
        Assert.Equal(400m, result);
    }

    [Fact]
    public void DistanceSincePrevious_NonPositiveDistance_ReturnsNull()
    {
        var prev = MakeLog(1400, 40, 60);
        var curr = MakeLog(1000, 35, 52); // odometer went backwards
        var result = Sut().DistanceSincePrevious(curr, prev);
        Assert.Null(result);
    }

    [Fact]
    public void LitersPer100Km_ZeroDistance_ReturnsNull()
    {
        var result = Sut().LitersPer100Km(40, 0);
        Assert.Null(result);
    }

    [Fact]
    public void LitersPer100Km_ValidInputs_ReturnsCorrectValue()
    {
        // 40L over 400km = 10 L/100km
        var result = Sut().LitersPer100Km(40, 400);
        Assert.Equal(10m, result);
    }

    [Fact]
    public void CostPerKm_ZeroDistance_ReturnsNull()
    {
        var result = Sut().CostPerKm(60, 0);
        Assert.Null(result);
    }

    [Fact]
    public void CostPerKm_ValidInputs_ReturnsCorrectValue()
    {
        // $60 over 400km = $0.15/km
        var result = Sut().CostPerKm(60, 400);
        Assert.Equal(0.15m, result);
    }

    [Fact]
    public void MilesPerGallon_ZeroGallons_ReturnsNull()
    {
        var result = Sut().MilesPerGallon(250, 0);
        Assert.Null(result);
    }

    [Fact]
    public void MilesPerGallon_ValidInputs_ReturnsCorrectValue()
    {
        // 300 miles / 10 gallons = 30 MPG
        var result = Sut().MilesPerGallon(300, 10);
        Assert.Equal(30m, result);
    }

    [Fact]
    public void CostPerMile_ZeroDistance_ReturnsNull()
    {
        var result = Sut().CostPerMile(60, 0);
        Assert.Null(result);
    }

    [Fact]
    public void CostPerMile_ValidInputs_ReturnsCorrectValue()
    {
        // $30 over 300 miles = $0.10/mile
        var result = Sut().CostPerMile(30, 300);
        Assert.Equal(0.1m, result);
    }

    // --- AccumulatedStats ---

    [Fact]
    public void AccumulatedStats_PartialFillUp_ReturnsNull()
    {
        var logs = new List<FuelLog>
        {
            MakeLog(1000, 40, 60),
            MakePartial(1400, 10, 15)
        };
        Assert.Null(Sut().AccumulatedStats(logs, 1));
    }

    [Fact]
    public void AccumulatedStats_FirstLog_NoStartingOdometer_ReturnsNull()
    {
        var logs = new List<FuelLog> { MakeLog(1000, 40, 60) };
        Assert.Null(Sut().AccumulatedStats(logs, 0));
    }

    [Fact]
    public void AccumulatedStats_FirstLog_WithStartingOdometer_ReturnsStats()
    {
        var logs = new List<FuelLog> { MakeLog(1400, 40, 60) };
        var result = Sut().AccumulatedStats(logs, 0, startingOdometer: 1000);
        Assert.NotNull(result);
        Assert.Equal(400m, result.Value.DistanceKm);
        Assert.Equal(40m, result.Value.TotalLiters);
        Assert.Equal(60m, result.Value.TotalCost);
    }

    [Fact]
    public void AccumulatedStats_AllPrecedingArePartials_NoStartingOdometer_ReturnsNull()
    {
        var logs = new List<FuelLog>
        {
            MakePartial(1000, 10, 15),
            MakeLog(1400, 40, 60)
        };
        Assert.Null(Sut().AccumulatedStats(logs, 1));
    }

    [Fact]
    public void AccumulatedStats_AllPrecedingArePartials_WithStartingOdometer_IncludesPartials()
    {
        var logs = new List<FuelLog>
        {
            MakePartial(1000, 10, 15),
            MakeLog(1400, 40, 60)
        };
        var result = Sut().AccumulatedStats(logs, 1, startingOdometer: 800);
        Assert.NotNull(result);
        Assert.Equal(600m, result.Value.DistanceKm);  // 1400 - 800
        Assert.Equal(50m, result.Value.TotalLiters);  // 10 + 40
        Assert.Equal(75m, result.Value.TotalCost);    // 15 + 60
    }

    [Fact]
    public void AccumulatedStats_TwoConsecutiveFullFillUps_ReturnsSingleLogStats()
    {
        var logs = new List<FuelLog>
        {
            MakeLog(1000, 40, 60),
            MakeLog(1400, 35, 52)
        };
        var result = Sut().AccumulatedStats(logs, 1);
        Assert.NotNull(result);
        Assert.Equal(400m, result.Value.DistanceKm);
        Assert.Equal(35m, result.Value.TotalLiters);
        Assert.Equal(52m, result.Value.TotalCost);
    }

    [Fact]
    public void AccumulatedStats_OnePartialBeforeFullFillUp_IncludesPartialFuelAndCost()
    {
        var logs = new List<FuelLog>
        {
            MakeLog(1000, 40, 60),                              // full
            MakePartial(1200, 10, 15),                          // partial
            MakeLog(1400, 35, 52)                               // full
        };
        var result = Sut().AccumulatedStats(logs, 2);
        Assert.NotNull(result);
        Assert.Equal(400m, result.Value.DistanceKm);   // 1400 - 1000
        Assert.Equal(45m, result.Value.TotalLiters);   // 10 + 35
        Assert.Equal(67m, result.Value.TotalCost);     // 15 + 52
    }

    [Fact]
    public void AccumulatedStats_TwoPartialsBeforeFullFillUp_AccumulatesAllFuel()
    {
        var logs = new List<FuelLog>
        {
            MakeLog(1000, 40, 60),
            MakePartial(1100, 8,  12),
            MakePartial(1250, 12, 18),
            MakeLog(1400, 30, 45)
        };
        var result = Sut().AccumulatedStats(logs, 3);
        Assert.NotNull(result);
        Assert.Equal(400m, result.Value.DistanceKm);   // 1400 - 1000
        Assert.Equal(50m, result.Value.TotalLiters);   // 8 + 12 + 30
        Assert.Equal(75m, result.Value.TotalCost);     // 12 + 18 + 45
    }

    [Fact]
    public void AccumulatedStats_IntermediateFullFillUp_OnlyCountsSinceLastFull()
    {
        var logs = new List<FuelLog>
        {
            MakeLog(1000, 40, 60),
            MakeLog(1400, 35, 52),                              // full — anchor
            MakePartial(1550, 10, 15),
            MakeLog(1700, 25, 37)                               // full — evaluated
        };
        var result = Sut().AccumulatedStats(logs, 3);
        Assert.NotNull(result);
        Assert.Equal(300m, result.Value.DistanceKm);   // 1700 - 1400
        Assert.Equal(35m, result.Value.TotalLiters);   // 10 + 25
        Assert.Equal(52m, result.Value.TotalCost);     // 15 + 37
    }

    // ── Segments / Totals ───────────────────────────────────────────────────

    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static FuelLog At(int id, int day, decimal odometer, decimal liters, decimal cost, bool partial = false) =>
        new() { Id = id, OdometerReading = odometer, LitersFilled = liters, TotalCost = cost,
                FilledAt = T0.AddDays(day), IsPartialFillUp = partial };

    [Fact]
    public void Segments_UnorderedInput_ReturnsFullFillUpsOldestFirst()
    {
        var logs = new[]
        {
            At(3, 20, 1700, 25, 37),
            At(1, 0, 1000, 40, 60),
            At(2, 10, 1400, 35, 52, partial: true)
        };
        var segments = Sut().Segments(logs, startingOdometer: 500);

        Assert.Equal([1, 3], segments.Select(s => s.Log.Id));
        Assert.Equal(500m, segments[0].DistanceKm);   // 1000 - starting 500
        Assert.Equal(700m, segments[1].DistanceKm);   // 1700 - 1000
        Assert.Equal(60m, segments[1].TotalLiters);   // partial 35 + 25
    }

    [Fact]
    public void Totals_AreDistanceWeighted_NotAverageOfRatios()
    {
        // 100 km @ 10 L (10 L/100km) and 900 km @ 45 L (5 L/100km)
        var segments = new[]
        {
            new FuelSegment(At(1, 0, 0, 0, 0), 10, 100, 20),
            new FuelSegment(At(2, 1, 0, 0, 0), 45, 900, 90)
        };
        var totals = Sut().Totals(segments);

        Assert.NotNull(totals);
        Assert.Equal(55m, totals.Value.TotalLiters);
        Assert.Equal(1000m, totals.Value.DistanceKm);
        Assert.Equal(110m, totals.Value.TotalCost);
        // 5.5 L/100km overall — a naive mean of ratios would give 7.5
    }

    [Fact]
    public void Totals_Empty_ReturnsNull()
    {
        Assert.Null(Sut().Totals([]));
    }

    // ── OdometerBounds ───────────────────────────────────────────────────────

    private static readonly FuelLog[] BoundsLogs =
    [
        At(1, 0, 1000, 40, 60),
        At(2, 10, 1400, 35, 52),
        At(3, 20, 1700, 25, 37)
    ];

    [Fact]
    public void OdometerBounds_NewestEntry_BoundedOnlyBelow()
    {
        var (min, max) = Sut().OdometerBounds(BoundsLogs, 500, T0.AddDays(30));
        Assert.Equal(1700m, min);
        Assert.Null(max);
    }

    [Fact]
    public void OdometerBounds_BackdatedEntry_BoundedByNeighbours()
    {
        var (min, max) = Sut().OdometerBounds(BoundsLogs, 500, T0.AddDays(5));
        Assert.Equal(1000m, min);
        Assert.Equal(1400m, max);
    }

    [Fact]
    public void OdometerBounds_BeforeFirstEntry_UsesStartingOdometer()
    {
        var (min, max) = Sut().OdometerBounds(BoundsLogs, 500, T0.AddDays(-1));
        Assert.Equal(500m, min);
        Assert.Equal(1000m, max);
    }

    [Fact]
    public void OdometerBounds_ExcludesLogBeingEdited()
    {
        var (min, max) = Sut().OdometerBounds(BoundsLogs, 500, T0.AddDays(10), excludeLogId: 2);
        Assert.Equal(1000m, min);
        Assert.Equal(1700m, max);
    }
}
