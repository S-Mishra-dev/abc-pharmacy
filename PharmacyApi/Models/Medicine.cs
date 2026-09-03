using System.Text.Json.Serialization;

namespace PharmacyApi.Models;

public sealed record Medicine(
    Guid Id,
    string FullName,
    string Notes,
    DateOnly ExpiryDate,
    int Quantity,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    decimal Price,
    string Brand);