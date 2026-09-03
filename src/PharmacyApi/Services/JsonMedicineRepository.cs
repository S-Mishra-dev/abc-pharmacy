using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PharmacyApi.Models;

namespace PharmacyApi.Services;

public sealed class JsonMedicineRepository(IWebHostEnvironment environment, ILogger<JsonMedicineRepository> logger)
    : IMedicineRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new DecimalTwoPlacesConverter() }
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _medicinesPath = Path.Combine(environment.ContentRootPath, "Data", "medicines.json");
    private readonly string _salesPath = Path.Combine(environment.ContentRootPath, "Data", "sales.json");

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_medicinesPath)!);

            if (!File.Exists(_medicinesPath))
            {
                var sampleMedicines = CreateSampleMedicines();
                await WriteJsonAsync(_medicinesPath, sampleMedicines, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created sample medicines data at {Path}", _medicinesPath);
            }

            if (!File.Exists(_salesPath))
            {
                await WriteJsonAsync(_salesPath, Array.Empty<SaleRecord>(), cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created empty sales data at {Path}", _salesPath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<Medicine>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadMedicinesUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var medicines = await ReadMedicinesUnlockedAsync(cancellationToken).ConfigureAwait(false);
            return medicines.FirstOrDefault(m => m.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Medicine> AddAsync(CreateMedicineRequest request, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var medicines = (await ReadMedicinesUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();

            var medicine = new Medicine(
                Id: Guid.NewGuid(),
                FullName: request.FullName.Trim(),
                Notes: request.Notes?.Trim() ?? string.Empty,
                ExpiryDate: request.ExpiryDate,
                Quantity: request.Quantity,
                Price: Math.Round(request.Price, 2, MidpointRounding.AwayFromZero),
                Brand: request.Brand.Trim());

            medicines.Add(medicine);
            await WriteJsonAsync(_medicinesPath, medicines, cancellationToken).ConfigureAwait(false);
            return medicine;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<(Medicine Medicine, SaleRecord Sale)?> SellAsync(
        Guid medicineId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var medicines = (await ReadMedicinesUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = medicines.FindIndex(m => m.Id == medicineId);
            if (index < 0)
            {
                return null;
            }

            var existing = medicines[index];
            if (existing.Quantity < quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock. Available: {existing.Quantity}, requested: {quantity}.");
            }

            var updated = existing with
            {
                Quantity = existing.Quantity - quantity
            };
            medicines[index] = updated;

            var sale = new SaleRecord(
                Id: Guid.NewGuid(),
                MedicineId: updated.Id,
                MedicineName: updated.FullName,
                QuantitySold: quantity,
                TotalPrice: Math.Round(updated.Price * quantity, 2, MidpointRounding.AwayFromZero),
                SaleDate: DateTimeOffset.UtcNow);

            var sales = (await ReadSalesUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            sales.Add(sale);

            await WriteJsonAsync(_medicinesPath, medicines, cancellationToken).ConfigureAwait(false);
            await WriteJsonAsync(_salesPath, sales, cancellationToken).ConfigureAwait(false);

            return (updated, sale);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Medicine>> ReadMedicinesUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_medicinesPath))
        {
            return [];
        }

        await using var stream = File.Open(_medicinesPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var medicines = await JsonSerializer.DeserializeAsync<List<Medicine>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return medicines ?? [];
    }

    private async Task<List<SaleRecord>> ReadSalesUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_salesPath))
        {
            return [];
        }

        await using var stream = File.Open(_salesPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var sales = await JsonSerializer.DeserializeAsync<List<SaleRecord>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return sales ?? [];
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Copy(tempPath, path, overwrite: true);
        File.Delete(tempPath);
    }

    private static List<Medicine> CreateSampleMedicines()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return
        [
            new Medicine(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Paracetamol 500mg",
                "Pain relief and fever reducer. Take after meals.",
                today.AddDays(14),
                8,
                45.50m,
                "Cipla"),
            new Medicine(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Amoxicillin 250mg",
                "Antibiotic. Complete the full course.",
                today.AddDays(120),
                5,
                120.00m,
                "Sun Pharma"),
            new Medicine(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Cetirizine 10mg",
                "Antihistamine for allergy relief.",
                today.AddDays(20),
                25,
                35.75m,
                "Dr. Reddy's"),
            new Medicine(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Omeprazole 20mg",
                "Proton pump inhibitor for acid reflux.",
                today.AddDays(200),
                50,
                89.99m,
                "Abbott"),
            new Medicine(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Ibuprofen 400mg",
                "NSAID for pain and inflammation.",
                today.AddDays(25),
                6,
                55.25m,
                "Pfizer")
        ];
    }

    private sealed class DecimalTwoPlacesConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return decimal.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
            }

            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Math.Round(value, 2, MidpointRounding.AwayFromZero));
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(Math.Round(value, 2, MidpointRounding.AwayFromZero)
                .ToString("F2", CultureInfo.InvariantCulture));
        }
    }
}