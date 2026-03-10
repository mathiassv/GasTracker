namespace GasTracker.Web.Services;

public class UnitConversionService
{
    private const decimal KmPerMile = 1.60934m;
    private const decimal LitersPerGallon = 3.78541m;

    public decimal KmToMiles(decimal km) => km / KmPerMile;
    public decimal MilesToKm(decimal miles) => miles * KmPerMile;
    public decimal LitersToGallons(decimal liters) => liters / LitersPerGallon;
    public decimal GallonsToLiters(decimal gallons) => gallons * LitersPerGallon;

    /// <summary>Convert stored km to display unit (PreferredUnit).</summary>
    public decimal ConvertDistance(decimal km, string unit) =>
        unit == "miles" ? KmToMiles(km) : km;

    /// <summary>Convert display unit to km for storage.</summary>
    public decimal ConvertDistanceToKm(decimal value, string unit) =>
        unit == "miles" ? MilesToKm(value) : value;

    /// <summary>Convert stored liters to display unit (PreferredUnit).</summary>
    public decimal ConvertVolume(decimal liters, string unit) =>
        unit == "miles" ? LitersToGallons(liters) : liters;

    /// <summary>Convert display unit to liters for storage.</summary>
    public decimal ConvertVolumeToLiters(decimal value, string unit) =>
        unit == "miles" ? GallonsToLiters(value) : value;

    // ── CostDistanceUnit helpers ─────────────────────────────────────────────

    /// <summary>Derives the internal costDistUnit key from PreferredUnit + StatScale.</summary>
    public string BuildCostDistUnit(string preferredUnit, int statScale) =>
        preferredUnit == "miles"
            ? statScale switch { 1 => "mile", 100 => "100miles", _ => "10miles" }
            : statScale switch { 1 => "km", 100 => "100km", _ => "10km" };

    public bool IsMetricCostUnit(string costDistUnit) => !costDistUnit.Contains("mile");

    /// <summary>Scale cost/km to the configured cost distance unit.</summary>
    public decimal CostPerDistUnit(decimal costPerKm, string costDistUnit) => costDistUnit switch
    {
        "km"       => costPerKm,
        "10km"     => costPerKm * 10m,
        "100km"    => costPerKm * 100m,
        "mile"     => costPerKm * KmPerMile,
        "10miles"  => costPerKm * KmPerMile * 10m,
        "100miles" => costPerKm * KmPerMile * 100m,
        _          => costPerKm * 10m
    };

    /// <summary>Scale liters/km to liters per N km. Returns null for mile-based units (use MPG instead).</summary>
    public decimal? LitersPerDistUnit(decimal liters, decimal distKm, string costDistUnit)
    {
        if (distKm <= 0 || !IsMetricCostUnit(costDistUnit)) return null;
        return costDistUnit switch
        {
            "km"    => liters / distKm,
            "10km"  => liters / distKm * 10m,
            "100km" => liters / distKm * 100m,
            _       => liters / distKm * 10m
        };
    }

    /// <summary>Human-readable denominator label, e.g. "10 km", "100 km", "mi".</summary>
    public string CostDistLabel(string costDistUnit) => costDistUnit switch
    {
        "km"       => "km",
        "10km"     => "10 km",
        "100km"    => "100 km",
        "mile"     => "mi",
        "10miles"  => "10 mi",
        "100miles" => "100 mi",
        _          => "10 km"
    };

    /// <summary>Efficiency metric label matching the cost distance unit.</summary>
    public string EfficiencyLabel(string costDistUnit) => costDistUnit switch
    {
        "km"    => "L/km",
        "10km"  => "L/10km",
        "100km" => "L/100km",
        _       => "MPG"   // mile-based
    };
}
