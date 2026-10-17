using System.Net.Http.Json;
using UI.Models;

namespace UI.Services
{
    /// <summary>
    /// Fetches products from ProductService via the API Gateway using HttpClient.
    /// </summary>
    public sealed class ProductCatalogueService : IProductCatalogueService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ProductCatalogueService> _logger;

        public ProductCatalogueService(HttpClient http, ILogger<ProductCatalogueService> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProductViewModel>> GetAllAsync(
            string? category = null,
            string? search   = null,
            CancellationToken ct = default)
        {
            var query = BuildQuery(category, search);
            var url   = $"api/Products{query}";

            try
            {
                var result = await _http.GetFromJsonAsync<List<ProductViewModel>>(url, ct);
                return result ?? new List<ProductViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch products from {Url}", url);
                return new List<ProductViewModel>();
            }
        }

        public async Task<ProductViewModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await _http.GetFromJsonAsync<ProductViewModel>($"api/Products/{id}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch product {Id}", id);
                return null;
            }
        }

        // ── Helpers ----------------------------

        private static string BuildQuery(string? category, string? search)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
                parts.Add($"category={Uri.EscapeDataString(category)}");

            if (!string.IsNullOrWhiteSpace(search))
                parts.Add($"search={Uri.EscapeDataString(search)}");

            return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        }
    }
}
