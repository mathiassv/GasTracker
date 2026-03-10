namespace GasTracker.Data.Entities;

public class FuelLog
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public decimal OdometerReading { get; set; }
    public decimal LitersFilled { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime FilledAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Car Car { get; set; } = null!;
}
