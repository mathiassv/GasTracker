using GasTracker.Data.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace GasTracker.Web.Services;

/// <summary>The signed-in user's display preferences, with the derived unit labels pages need.</summary>
public sealed record UserPreferences(int UserId, string Unit, string CostDistUnit, string CurrencyCode, string CurrencySymbol)
{
    public static readonly UserPreferences Default = new(0, "km", "10km", "SEK", "kr");

    public bool IsImperial => Unit == "miles";
    /// <summary>"km" or "mi".</summary>
    public string DistUnit => IsImperial ? "mi" : "km";
    /// <summary>"L" or "gal".</summary>
    public string VolumeUnit => IsImperial ? "gal" : "L";
    /// <summary>"Liters" or "Gallons".</summary>
    public string VolumeLabel => IsImperial ? "Gallons" : "Liters";
}

public class CurrentUserService(AuthenticationStateProvider authState, UnitConversionService units)
{
    /// <summary>The internal user id from the <c>app_user_id</c> claim, or 0 if not signed in.</summary>
    public async Task<int> GetUserIdAsync()
    {
        var auth = await authState.GetAuthenticationStateAsync();
        return int.TryParse(auth.User.FindFirst("app_user_id")?.Value, out var id) ? id : 0;
    }

    /// <summary>Reads preferences fresh from the database, so changes saved on Profile apply immediately.</summary>
    public async Task<UserPreferences> GetPreferencesAsync(IUnitOfWork uow)
    {
        var userId = await GetUserIdAsync();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user is null) return UserPreferences.Default with { UserId = userId };

        return new UserPreferences(
            userId,
            user.PreferredUnit,
            units.BuildCostDistUnit(user.PreferredUnit, user.StatScale),
            user.CurrencyCode,
            user.CurrencySymbol);
    }
}
