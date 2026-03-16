namespace GasTracker.Data.Entities;

public class AppUser
{
    public int Id { get; set; }
    public required string GoogleSubjectId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string PreferredUnit { get; set; } = "km";
    public int StatScale { get; set; } = 10;
    public string CurrencyCode { get; set; } = "SEK";
    public string CurrencySymbol { get; set; } = "kr";
    public string Theme { get; set; } = "light";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Car> Cars { get; set; } = [];
}
