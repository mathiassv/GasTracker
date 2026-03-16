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
    public void AccumulatedStats_FirstLog_ReturnsNull()
    {
        var logs = new List<FuelLog> { MakeLog(1000, 40, 60) };
        Assert.Null(Sut().AccumulatedStats(logs, 0));
    }

    [Fact]
    public void AccumulatedStats_AllPrecedingArePartials_ReturnsNull()
    {
        var logs = new List<FuelLog>
        {
            MakePartial(1000, 10, 15),
            MakeLog(1400, 40, 60)
        };
        Assert.Null(Sut().AccumulatedStats(logs, 1));
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
}
