using UI.Models;

namespace UI.Services
{
    /// <summary>
    /// Manages the current user's shopping cart via the Cart_Services API Gateway route.
    /// </summary>
    public interface ICartService
    {
        Task<CartViewModel>  GetCartAsync(CancellationToken ct = default);
        Task<CartViewModel>  AddOrIncrementAsync(ProductViewModel product, int quantity = 1, CancellationToken ct = default);
        Task<CartViewModel>  UpdateQuantityAsync(string productId, int delta, CancellationToken ct = default);
        Task<CartViewModel>  RemoveItemAsync(string productId, CancellationToken ct = default);
        Task<CartViewModel>  ClearAsync(CancellationToken ct = default);
    }
}
