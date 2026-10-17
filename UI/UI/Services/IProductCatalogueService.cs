using UI.Models;

namespace UI.Services
{
    /// <summary>
    /// Provides read-only access to the product catalogue via the API Gateway.
    /// </summary>
    public interface IProductCatalogueService
    {
        Task<IReadOnlyList<ProductViewModel>> GetAllAsync(
            string? category = null,
            string? search   = null,
            CancellationToken ct = default);

        Task<ProductViewModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
