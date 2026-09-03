using System.ComponentModel.DataAnnotations;

namespace PharmacyApi.Models;

public sealed class SellMedicineRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity to sell must be at least 1.")]
    public int Quantity { get; init; }
}