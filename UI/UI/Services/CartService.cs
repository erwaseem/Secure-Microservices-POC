using System.Net.Http.Json;
using System.Text.Json;
using UI.Models;

namespace UI.Services
{
    /// <summary>
    /// HttpClient-backed cart service.  Uses the ASP.NET Core session ID as the
    /// cart identifier and forwards all operations through the API Gateway to Cart_Services.
    /// </summary>
    public sealed class CartService : ICartService
    {
        private readonly HttpClient             _http;
        private readonly IHttpContextAccessor   _httpContextAccessor;
        private readonly ILogger<CartService>   _logger;

        public CartService(
            HttpClient           http,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger)
        {
            _http                = http;
            _httpContextAccessor = httpContextAccessor;
            _logger              = logger;
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public async Task<CartViewModel> GetCartAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<CartViewModel>(CartUrl(), ct);
                return result ?? new CartViewModel { SessionId = SessionId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cart for session {SessionId}", SessionId);
                return new CartViewModel { SessionId = SessionId };
            }
        }

        public async Task<CartViewModel> AddOrIncrementAsync(
            ProductViewModel product, int quantity = 1, CancellationToken ct = default)
        {
            var req = new
            {
                productId     = product.Id.ToString(),
                name          = product.Name,
                brand         = product.Brand,
                price         = product.Price,
                originalPrice = product.OriginalPrice,
                imageUrl      = product.ImageUrl,
                quantity
            };

            try
            {
                var response = await _http.PostAsJsonAsync($"{CartUrl()}/items", req, ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CartViewModel>(cancellationToken: ct);
                return result ?? await GetCartAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item {ProductId} to cart", product.Id);
                return await GetCartAsync(ct);
            }
        }

        public async Task<CartViewModel> UpdateQuantityAsync(
            string productId, int delta, CancellationToken ct = default)
        {
            var req = new { productId, delta };

            try
            {
                var response = await _http.PatchAsJsonAsync($"{CartUrl()}/items", req, ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CartViewModel>(cancellationToken: ct);
                return result ?? await GetCartAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update quantity for {ProductId}", productId);
                return await GetCartAsync(ct);
            }
        }

        public async Task<CartViewModel> RemoveItemAsync(string productId, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.DeleteAsync($"{CartUrl()}/items/{productId}", ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CartViewModel>(cancellationToken: ct);
                return result ?? await GetCartAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove item {ProductId} from cart", productId);
                return await GetCartAsync(ct);
            }
        }

        public async Task<CartViewModel> ClearAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.DeleteAsync(CartUrl(), ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CartViewModel>(cancellationToken: ct);
                return result ?? new CartViewModel { SessionId = SessionId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cart for session {SessionId}", SessionId);
                return new CartViewModel { SessionId = SessionId };
            }
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private string SessionId =>
            _httpContextAccessor.HttpContext?.Session.Id
            ?? throw new InvalidOperationException("No active HTTP session.");

        private string CartUrl() => $"api/Cart/{SessionId}";
    }
}
