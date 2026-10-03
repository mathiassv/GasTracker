using System.Text.Json;
using System.Text.Json.Serialization;
using GasTracker.Data.Entities;
using GasTracker.Data.Interfaces;

namespace GasTracker.Web.Services;

/// <summary>JSON export/import of a user's cars and fill-up history.</summary>
public class DataTransferService(ILogger<DataTransferService> logger)
{
    public const int FormatVersion = 1;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<string> ExportJsonAsync(IUnitOfWork uow, int userId)
    {
        var cars = await uow.Cars.GetByUserIdAsync(userId);
        var carDtos = new List<CarExportDto>();

        foreach (var car in cars)
        {
            var logs = await uow.FuelLogs.GetByCarIdAsync(car.Id, userId);
            carDtos.Add(new CarExportDto(
                car.Name,
                car.LicensePlate,
                car.StartingOdometer,
                car.CreatedAt,
                logs.OrderBy(l => l.FilledAt).Select(l => new FuelLogExportDto(
                    l.OdometerReading,
                    l.LitersFilled,
                    l.TotalCost,
                    l.FilledAt,
                    l.IsPartialFillUp,
                    l.Notes
                )).ToList()
            ));
        }

        return JsonSerializer.Serialize(new ExportDto(DateTime.UtcNow, FormatVersion, carDtos), JsonOptions);
    }

    /// <summary>
    /// Imports every car in the file as a new car for <paramref name="userId"/>. The whole file is
    /// validated first and saved in a single SaveChanges, so it imports everything or nothing.
    /// </summary>
    public async Task<ImportResult> ImportAsync(IUnitOfWork uow, int userId, Stream json)
    {
        ExportDto? export;
        try
        {
            export = await JsonSerializer.DeserializeAsync<ExportDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return ImportResult.Fail("Could not parse the file. Make sure it is a valid GasTracker export.");
        }

        if (export is null || export.Version != FormatVersion || export.Cars is null)
            return ImportResult.Fail("Invalid or unsupported file format.");

        var problem = export.Cars.Select(ValidateCar).FirstOrDefault(p => p is not null);
        if (problem is not null)
            return ImportResult.Fail($"Import aborted: {problem}");

        var cars = export.Cars.Select(carDto => new Car
        {
            AppUserId = userId,
            Name = carDto.Name.Trim(),
            LicensePlate = carDto.LicensePlate,
            StartingOdometer = carDto.StartingOdometer,
            CreatedAt = carDto.CreatedAt,
            FuelLogs = (carDto.FuelLogs ?? []).Select(logDto => new FuelLog
            {
                OdometerReading = logDto.OdometerReading,
                LitersFilled = logDto.LitersFilled,
                TotalCost = logDto.TotalCost,
                FilledAt = logDto.FilledAt,
                IsPartialFillUp = logDto.IsPartialFillUp,
                Notes = logDto.Notes
            }).ToList()
        }).ToList();

        try
        {
            foreach (var car in cars)
                await uow.Cars.AddAsync(car);

            // Single SaveChanges = single transaction: all cars and fill-ups, or nothing
            await uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            uow.DiscardChanges();
            logger.LogError(ex, "Import failed for user {UserId}", userId);
            return ImportResult.Fail($"Import failed: {ex.Message}");
        }

        var carsAdded = cars.Count;
        var logsAdded = cars.Sum(c => c.FuelLogs.Count);
        return new ImportResult(true,
            $"Imported {carsAdded} car{(carsAdded != 1 ? "s" : "")} and {logsAdded} fill-up{(logsAdded != 1 ? "s" : "")}.",
            carsAdded, logsAdded);
    }

    /// <summary>Returns a user-facing problem description, or null if the car and its fill-ups are valid.</summary>
    public static string? ValidateCar(CarExportDto? car, int index)
    {
        if (car is null) return $"car #{index + 1} is empty.";
        var label = string.IsNullOrWhiteSpace(car.Name) ? $"car #{index + 1}" : $"\"{car.Name}\"";
        if (string.IsNullOrWhiteSpace(car.Name)) return $"{label} has no name.";
        if (car.Name.Length > 100) return $"{label} has a name longer than 100 characters.";
        if (car.StartingOdometer < 0) return $"{label} has a negative starting odometer.";

        foreach (var log in car.FuelLogs ?? [])
        {
            if (log is null) return $"{label} contains an empty fill-up entry.";
            var when = log.FilledAt.ToString("yyyy-MM-dd");
            if (log.OdometerReading <= car.StartingOdometer)
                return $"{label}: fill-up on {when} has an odometer reading at or below the starting odometer.";
            if (log.LitersFilled <= 0) return $"{label}: fill-up on {when} has no fuel amount.";
            if (log.TotalCost < 0) return $"{label}: fill-up on {when} has a negative cost.";
        }
        return null;
    }
}

public record ImportResult(bool Success, string Message, int CarsAdded = 0, int LogsAdded = 0)
{
    public static ImportResult Fail(string message) => new(false, message);
}

public record ExportDto(
    DateTime ExportedAt,
    int Version,
    List<CarExportDto> Cars);

public record CarExportDto(
    string Name,
    string? LicensePlate,
    decimal StartingOdometer,
    DateTime CreatedAt,
    List<FuelLogExportDto> FuelLogs);

public record FuelLogExportDto(
    decimal OdometerReading,
    decimal LitersFilled,
    decimal TotalCost,
    DateTime FilledAt,
    bool IsPartialFillUp,
    string? Notes);
