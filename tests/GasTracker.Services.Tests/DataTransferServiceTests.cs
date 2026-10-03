using System.Text;
using GasTracker.Data;
using GasTracker.Data.Entities;
using GasTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GasTracker.Services.Tests;

public class DataTransferServiceTests : IDisposable
{
    private readonly GasTrackerDbContext _db;
    private readonly UnitOfWork _uow;
    private readonly DataTransferService _sut = new(NullLogger<DataTransferService>.Instance);
    private readonly int _userId;
    private readonly int _otherUserId;

    public DataTransferServiceTests()
    {
        var options = new DbContextOptionsBuilder<GasTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new GasTrackerDbContext(options);
        _uow = new UnitOfWork(_db);

        var user = new AppUser { GoogleSubjectId = "u1", Email = "u1@test", DisplayName = "U1" };
        var other = new AppUser { GoogleSubjectId = "u2", Email = "u2@test", DisplayName = "U2" };
        _db.AppUsers.AddRange(user, other);
        _db.SaveChanges();
        _userId = user.Id;
        _otherUserId = other.Id;
    }

    public void Dispose() => _uow.Dispose();

    private static Stream Json(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    private void SeedCar(int userId, string name)
    {
        _db.Cars.Add(new Car
        {
            AppUserId = userId,
            Name = name,
            LicensePlate = "ABC123",
            StartingOdometer = 1000,
            FuelLogs =
            [
                new FuelLog { OdometerReading = 1500, LitersFilled = 40, TotalCost = 700, FilledAt = new DateTime(2026, 1, 1), Notes = "first" },
                new FuelLog { OdometerReading = 1800, LitersFilled = 20, TotalCost = 350, FilledAt = new DateTime(2026, 1, 10), IsPartialFillUp = true }
            ]
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Export_ThenImport_RoundTripsCarsAndFillUps()
    {
        SeedCar(_userId, "Volvo");
        SeedCar(_otherUserId, "Not mine");

        var json = await _sut.ExportJsonAsync(_uow, _userId);
        Assert.DoesNotContain("Not mine", json);

        var result = await _sut.ImportAsync(_uow, _userId, Json(json));

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, result.CarsAdded);
        Assert.Equal(2, result.LogsAdded);

        var cars = await _db.Cars.Include(c => c.FuelLogs).Where(c => c.AppUserId == _userId).ToListAsync();
        Assert.Equal(2, cars.Count); // original + imported copy
        var imported = cars.OrderBy(c => c.Id).Last();
        Assert.Equal("Volvo", imported.Name);
        Assert.Equal("ABC123", imported.LicensePlate);
        Assert.Equal(1000m, imported.StartingOdometer);
        Assert.Equal(2, imported.FuelLogs.Count);
        Assert.Contains(imported.FuelLogs, l => l.IsPartialFillUp && l.OdometerReading == 1800m);
        Assert.Contains(imported.FuelLogs, l => l.Notes == "first" && l.TotalCost == 700m);
    }

    [Fact]
    public async Task Import_InvalidCarAnywhereInFile_ImportsNothing()
    {
        const string json = """
            { "version": 1, "exportedAt": "2026-01-01T00:00:00Z", "cars": [
              { "name": "Good", "startingOdometer": 0, "createdAt": "2026-01-01T00:00:00Z",
                "fuelLogs": [ { "odometerReading": 100, "litersFilled": 10, "totalCost": 150, "filledAt": "2026-01-02T00:00:00Z" } ] },
              { "name": "Bad", "startingOdometer": 0, "createdAt": "2026-01-01T00:00:00Z",
                "fuelLogs": [ { "odometerReading": 100, "litersFilled": 0, "totalCost": 150, "filledAt": "2026-01-02T00:00:00Z" } ] }
            ] }
            """;

        var result = await _sut.ImportAsync(_uow, _userId, Json(json));

        Assert.False(result.Success);
        Assert.Contains("\"Bad\"", result.Message);
        Assert.Empty(_db.Cars);
    }

    [Theory]
    [InlineData("""{ "version": 2, "cars": [] }""")]
    [InlineData("""{ "version": 1 }""")]
    [InlineData("not json at all")]
    [InlineData("""{ "version": 1, "cars": [ null ] }""")]
    [InlineData("""{ "version": 1, "cars": [ { "name": "Car", "startingOdometer": 0, "fuelLogs": [ null ] } ] }""")]
    public async Task Import_UnsupportedOrMalformedFile_Fails(string json)
    {
        var result = await _sut.ImportAsync(_uow, _userId, Json(json));
        Assert.False(result.Success);
        Assert.Empty(_db.Cars);
    }

    [Theory]
    [InlineData("", 0, 100, 10, 1, "has no name")]
    [InlineData("Car", -1, 100, 10, 1, "negative starting odometer")]
    [InlineData("Car", 500, 400, 10, 1, "at or below the starting odometer")]
    [InlineData("Car", 0, 100, 0, 1, "no fuel amount")]
    [InlineData("Car", 0, 100, 10, -1, "negative cost")]
    public void ValidateCar_RejectsBadData(string name, decimal startOdo, decimal odo, decimal liters, decimal cost, string expected)
    {
        var car = new CarExportDto(name, null, startOdo, DateTime.UtcNow,
            [new FuelLogExportDto(odo, liters, cost, DateTime.UtcNow, false, null)]);

        var problem = DataTransferService.ValidateCar(car, 0);

        Assert.NotNull(problem);
        Assert.Contains(expected, problem);
    }

    [Fact]
    public void ValidateCar_ValidCar_ReturnsNull()
    {
        var car = new CarExportDto("Car", null, 0, DateTime.UtcNow,
            [new FuelLogExportDto(100, 10, 150, DateTime.UtcNow, false, null)]);
        Assert.Null(DataTransferService.ValidateCar(car, 0));
    }
}
