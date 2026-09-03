using PharmacyApi.Models;

namespace PharmacyApi.Services;

public interface IMedicineRepository
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Medicine>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Medicine> AddAsync(CreateMedicineRequest request, CancellationToken cancellationToken = default);

    Task<(Medicine Medicine, SaleRecord Sale)?> SellAsync(
        Guid medicineId,
        int quantity,
        CancellationToken cancellationToken = default);
}