using System.Globalization;

namespace GasTracker.Web.Services;

public class DisplayFormatter
{
    // sv-SE: thousands = thin space, decimal = comma  e.g. "1 234,56"
    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("sv-SE");

    /// <summary>Format a number with Swedish separators. Default 1 decimal place.</summary>
    public string Num(decimal value, int decimals = 1) =>
        value.ToString($"N{decimals}", _culture);

    /// <summary>Format currency with symbol placed after the value: "1 234,56 kr"</summary>
    public string Currency(decimal value, string symbol) =>
        $"{value.ToString("N2", _culture)} {symbol}";

    /// <summary>Format a rate (cost/km, cost/mi) to 2 decimal places with symbol after.</summary>
    public string Rate(decimal value, string symbol, string perUnit) =>
        $"{value.ToString("N2", _culture)} {symbol}/{perUnit}";
}
