namespace PharmacyApi.Models;

public sealed record SaleRecord(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    int QuantitySold,
    decimal TotalPrice,
    DateTimeOffset SaleDate);