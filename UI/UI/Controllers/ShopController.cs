using Microsoft.AspNetCore.Mvc;
using UI.Services;

namespace UI.Controllers
{
    // ShopController — thin controller, delegates to services.


    /// <summary>
    /// Handles the product listing page and all cart operations.
    /// All business logic is delegated to injected services — this controller
    /// is intentionally thin: validate input, call service, redirect or return view.
    /// </summary>
    public class ShopController : Controller
    {
        private readonly IProductCatalogueService _catalogue;
        private readonly ICartService             _cart;

        private readonly IConfiguration _config;

        public ShopController(
            IProductCatalogueService catalogue,
            ICartService cart,
            IConfiguration config)
        {
            _catalogue = catalogue;
            _cart      = cart;
            _config    = config;
        }

        // GET /Shop 
        /// <summary>Product listing with optional category filter, keyword search and sort.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? category,
            string? search,
            string? sort,
            CancellationToken ct)
        {
            // Fetch from API Gateway → ProductService
            var products = await _catalogue.GetAllAsync(category, search, ct);

            // Client-side sort (ProductService returns all; UI sorts locally)
            var sorted = sort switch
            {
                "price_asc"  => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "rating"     => products.OrderByDescending(p => p.Rating),
                _            => products.OrderByDescending(p => p.ReviewCount), // default: popular
            };

            // Fetch cart count from API Gateway → Cart_Services
            var cart = await _cart.GetCartAsync(ct);

            ViewBag.Category   = category ?? "all";
            ViewBag.Search     = search   ?? string.Empty;
            ViewBag.Sort       = sort     ?? "popular";
            ViewBag.CartCount  = cart.TotalItems;
            ViewBag.GatewayUrl = _config["ApiGateway:BaseUrl"] ?? "http://localhost:5020";

            return View(sorted.ToList());
        }

        // POST /Shop/AddToCart 
        /// <summary>Adds one unit (or more) of a product to the cart, then returns to the listing.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            Guid productId, int quantity = 1, CancellationToken ct = default)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Index", "Shop") });

            var product = await _catalogue.GetByIdAsync(productId, ct);
            if (product is null)
                return NotFound();

            await _cart.AddOrIncrementAsync(product, quantity, ct);

            TempData["ToastMessage"] = $"\"{TruncateName(product.Name)}\" added to cart.";
            return RedirectToAction(nameof(Index));
        }

        //  POST /Shop/BuyNow 
        /// <summary>Adds the product to the cart and immediately navigates to the cart page.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(Guid productId, CancellationToken ct = default)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Cart", "Shop") });

            var product = await _catalogue.GetByIdAsync(productId, ct);
            if (product is null)
                return NotFound();

            await _cart.AddOrIncrementAsync(product, 1, ct);

            return RedirectToAction(nameof(Cart));
        }

        // GET /Shop/Details/{id} 
        /// <summary>Returns product details as JSON for the client-side detail modal.</summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var product = await _catalogue.GetByIdAsync(id, ct);
            if (product is null)
                return NotFound();

            return Json(new
            {
                product.Id,
                product.Brand,
                product.Name,
                product.Category,
                product.Price,
                product.OriginalPrice,
                product.DiscountPercent,
                product.Rating,
                product.ReviewCount,
                product.ImageUrl,
                product.Badge,
                product.IsNew,
            });
        }

        //  GET /Shop/Cart 
        /// <summary>Displays the cart with a full order summary.</summary>
        [HttpGet]
        public async Task<IActionResult> Cart(CancellationToken ct)
            => View(await _cart.GetCartAsync(ct));

        //  POST /Shop/UpdateQuantity 
        /// <summary>Increments or decrements the quantity of a cart line. Removes the line when qty reaches 0.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            string productId, int delta, CancellationToken ct = default)
        {
            await _cart.UpdateQuantityAsync(productId, delta, ct);
            return RedirectToAction(nameof(Cart));
        }

        // POST /Shop/RemoveItem 
        /// <summary>Removes a line from the cart entirely.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(
            string productId, CancellationToken ct = default)
        {
            await _cart.RemoveItemAsync(productId, ct);
            TempData["ToastMessage"] = "Item removed from cart.";
            return RedirectToAction(nameof(Cart));
        }

        //  POST /Shop/PlaceOrder 
        /// <summary>Clears the cart to simulate a successful order placement.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CancellationToken ct = default)
        {
            await _cart.ClearAsync(ct);
            TempData["OrderSuccess"] = true;
            return RedirectToAction(nameof(Cart));
        }

        //  Private helpers 

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("jwt_token"));

        private static string TruncateName(string name, int maxLength = 40) =>
            name.Length <= maxLength ? name : string.Concat(name.AsSpan(0, maxLength), "…");
    }
}
