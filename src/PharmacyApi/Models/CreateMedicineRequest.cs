using System.ComponentModel.DataAnnotations;

namespace PharmacyApi.Models;

public sealed class CreateMedicineRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 200 characters.")]
    public string FullName { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string Notes { get; init; } = string.Empty;

    [Required(ErrorMessage = "Expiry date is required.")]
    public DateOnly ExpiryDate { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be zero or greater.")]
    public int Quantity { get; init; }

    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Price must be between 0.01 and 999999.99.")]
    public decimal Price { get; init; }

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Brand must be between 1 and 200 characters.")]
    public string Brand { get; init; } = string.Empty;
}