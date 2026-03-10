namespace GasTracker.Data.Entities;

public class Car
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public required string Name { get; set; }
    public string? LicensePlate { get; set; }
    public decimal StartingOdometer { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser AppUser { get; set; } = null!;
    public ICollection<FuelLog> FuelLogs { get; set; } = [];
}
