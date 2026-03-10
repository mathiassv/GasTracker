using GasTracker.Web.Services;

namespace GasTracker.Services.Tests;

public class UnitConversionServiceTests
{
    private static UnitConversionService Sut() => new();

    [Fact]
    public void KmToMiles_ConvertsCorrectly()
    {
        var miles = Sut().KmToMiles(1.60934m);
        Assert.Equal(1.0m, Math.Round(miles, 4)); // ~1 mile
    }

    [Fact]
    public void MilesToKm_ConvertsCorrectly()
    {
        var km = Sut().MilesToKm(1m);
        Assert.Equal(1.60934m, km);
    }

    [Fact]
    public void KmToMiles_RoundTrip_PreservesValue()
    {
        var original = 500m;
        var miles = Sut().KmToMiles(original);
        var backToKm = Sut().MilesToKm(miles);
        Assert.Equal(original, Math.Round(backToKm, 4));
    }

    [Fact]
    public void LitersToGallons_ConvertsCorrectly()
    {
        var gallons = Sut().LitersToGallons(3.78541m);
        Assert.Equal(1.0m, Math.Round(gallons, 4));
    }

    [Fact]
    public void GallonsToLiters_ConvertsCorrectly()
    {
        var liters = Sut().GallonsToLiters(1m);
        Assert.Equal(3.78541m, liters);
    }

    [Fact]
    public void LitersToGallons_RoundTrip_PreservesValue()
    {
        var original = 50m;
        var gallons = Sut().LitersToGallons(original);
        var backToLiters = Sut().GallonsToLiters(gallons);
        Assert.Equal(original, Math.Round(backToLiters, 4));
    }

    [Fact]
    public void ConvertDistance_KmUnit_ReturnsSameValue()
    {
        var result = Sut().ConvertDistance(100m, "km");
        Assert.Equal(100m, result);
    }

    [Fact]
    public void ConvertDistance_MilesUnit_ConvertsFromKm()
    {
        var result = Sut().ConvertDistance(1.60934m, "miles");
        Assert.Equal(1.0m, Math.Round(result, 4));
    }

    [Fact]
    public void ConvertDistanceToKm_KmUnit_ReturnsSameValue()
    {
        var result = Sut().ConvertDistanceToKm(100m, "km");
        Assert.Equal(100m, result);
    }

    [Fact]
    public void ConvertDistanceToKm_MilesUnit_ConvertsToKm()
    {
        var result = Sut().ConvertDistanceToKm(1m, "miles");
        Assert.Equal(1.60934m, result);
    }

    [Fact]
    public void ConvertVolume_KmUnit_ReturnsSameValue()
    {
        var result = Sut().ConvertVolume(50m, "km");
        Assert.Equal(50m, result);
    }

    [Fact]
    public void ConvertVolume_MilesUnit_ConvertsFromLiters()
    {
        var result = Sut().ConvertVolume(3.78541m, "miles");
        Assert.Equal(1.0m, Math.Round(result, 4));
    }
}
