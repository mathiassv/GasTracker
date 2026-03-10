using GasTracker.Data.Entities;
using GasTracker.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GasTracker.Services.Tests;

public class FuelCalculatorServiceTests
{
    private static FuelCalculatorService Sut() => new(NullLogger<FuelCalculatorService>.Instance);

    private static FuelLog MakeLog(decimal odometer, decimal liters, decimal cost, DateTime? filledAt = null) =>
        new() { OdometerReading = odometer, LitersFilled = liters, TotalCost = cost, FilledAt = filledAt ?? DateTime.UtcNow };

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
}
