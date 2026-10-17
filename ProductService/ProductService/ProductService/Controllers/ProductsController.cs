using MassTransit;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using Shared.Events;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _repo;
        private readonly IUnitOfWork        _uow;
        private readonly IPublishEndpoint   _publishEndpoint;

        public ProductsController(IProductRepository repo, IUnitOfWork uow, IPublishEndpoint publishEndpoint)
        {
            _repo            = repo;
            _uow             = uow;
            _publishEndpoint = publishEndpoint;
        }

        //  DTOs 

        public record CreateDto(
            string  Sku,
            string  Name,
            string  Brand,
            string  Category,
            decimal Price,
            decimal OriginalPrice,
            double  Rating,
            int     ReviewCount,
            string  ImageUrl,
            string? Badge,
            bool    IsNew);

        public record UpdateDto(
            string?  Name,
            string?  Brand,
            string?  Category,
            decimal? Price,
            decimal? OriginalPrice,
            double?  Rating,
            int?     ReviewCount,
            string?  ImageUrl,
            string?  Badge,
            bool?    IsNew);

        //  GET /api/Products 
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? category,
            [FromQuery] string? search,
            CancellationToken ct)
        {
            var all = await _repo.ListAsync(ct);

            IEnumerable<Product> result = all;

            if (!string.IsNullOrWhiteSpace(category) && category != "all")
                result = result.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Brand.Contains(search, StringComparison.OrdinalIgnoreCase));

            return Ok(result.ToList());
        }

        //  GET /api/Products/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _repo.GetByIdAsync(id, ct)) is { } p ? Ok(p) : NotFound();

        //  POST /api/Products
        [HttpPost]
        public async Task<IActionResult> Create(CreateDto dto, CancellationToken ct)
        {
            if (await _repo.GetBySkuAsync(dto.Sku, ct) is not null)
                return Conflict("SKU already exists.");

            var p = new Product
            {
                Id            = Guid.NewGuid(),
                Sku           = dto.Sku,
                Name          = dto.Name,
                Brand         = dto.Brand,
                Category      = dto.Category,
                Price         = dto.Price,
                OriginalPrice = dto.OriginalPrice,
                Rating        = dto.Rating,
                ReviewCount   = dto.ReviewCount,
                ImageUrl      = dto.ImageUrl,
                Badge         = dto.Badge,
                IsNew         = dto.IsNew,
            };

            await _repo.AddAsync(p, ct);
            await _uow.SaveChangesAsync(ct);

            // 🐰 Publish ProductChangedEvent to RabbitMQ
            await _publishEndpoint.Publish(new ProductChangedEvent(
                p.Id, p.Name, p.Brand, p.Category, p.Price, "created", DateTime.UtcNow
            ), ct);

            return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
        }

        //  PUT /api/Products/{id} 
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateDto dto, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null) return NotFound();

            p.Name          = dto.Name          ?? p.Name;
            p.Brand         = dto.Brand         ?? p.Brand;
            p.Category      = dto.Category      ?? p.Category;
            p.Price         = dto.Price         ?? p.Price;
            p.OriginalPrice = dto.OriginalPrice ?? p.OriginalPrice;
            p.Rating        = dto.Rating        ?? p.Rating;
            p.ReviewCount   = dto.ReviewCount   ?? p.ReviewCount;
            p.ImageUrl      = dto.ImageUrl      ?? p.ImageUrl;
            p.Badge         = dto.Badge         ?? p.Badge;
            p.IsNew         = dto.IsNew         ?? p.IsNew;

            ((IRepository<Product>)_repo).Update(p);
            await _uow.SaveChangesAsync(ct);
            return Ok(p);
        }

        //  DELETE /api/Products/{id} 
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null) return NotFound();

            ((IRepository<Product>)_repo).Remove(p);
            await _uow.SaveChangesAsync(ct);

            //  Publish ProductChangedEvent to RabbitMQ
            await _publishEndpoint.Publish(new ProductChangedEvent(
                p.Id, p.Name, p.Brand, p.Category, p.Price, "deleted", DateTime.UtcNow
            ), ct);

            return NoContent();
        }

        //  POST /api/Products/seed  (idempotent demo data) 
        [HttpPost("seed")]
        public async Task<IActionResult> Seed(CancellationToken ct)
        {
            var seeds = new List<CreateDto>
            {
                new("SKU-001", "Galaxy S24 Ultra 5G (256GB)",           "Samsung",     "electronics", 124999, 149999, 4.6, 12840, "https://m.media-amazon.com/images/I/71Sa3dqTqzL._AC_SX300_SY300_.jpg", "Bestseller", false),
                new("SKU-002", "MacBook Air M3 13\" (8GB/256GB)",       "Apple",       "electronics", 114900, 134900, 4.8,  5420, "https://m.media-amazon.com/images/I/61lsexTCkEL._AC_SX300_SY300_.jpg", "Top Rated",  true),
                new("SKU-003", "WH-1000XM5 Noise Cancelling Headphones","Sony",        "electronics",  24990,  34990, 4.7,  8921, "https://m.media-amazon.com/images/I/71o8Q5XJS5L._AC_SX300_SY300_.jpg", null,         false),
                new("SKU-004", "Air Max 270 React Running Shoes",        "Nike",        "fashion",       8995,  14995, 4.4,  3210, "https://m.media-amazon.com/images/I/61GxNFflEcL._AC_UX395_.jpg",     "40% off",    false),
                new("SKU-005", "Men's 511 Slim Fit Stretch Jeans",       "Levi's",      "fashion",       2799,   4599, 4.3,  6700, "https://m.media-amazon.com/images/I/613nSmNBrfL._AC_UX569_.jpg",     null,         false),
                new("SKU-006", "Duo 7-in-1 Electric Pressure Cooker 6L","Instant Pot", "home",          8499,  12999, 4.6, 22100, "https://m.media-amazon.com/images/I/71V4T8ckrqL._AC_SX300_SY300_.jpg","Deal",       false),
                new("SKU-007", "V15 Detect Absolute Cordless Vacuum",    "Dyson",       "home",         52900,  64900, 4.5,  1870, "https://m.media-amazon.com/images/I/71HkKl+yM6L._AC_SX300_SY300_.jpg","Premium",    true),
                new("SKU-008", "Ultraboost 22 Running Shoes (Unisex)",   "Adidas",      "sports",       12495,  17999, 4.5,  4560, "https://m.media-amazon.com/images/I/71OHxaYOXtL._AC_UX395_.jpg",     null,         false),
                new("SKU-009", "Astrox 100 ZZ Badminton Racket",         "Yonex",       "sports",       16990,  20990, 4.7,   980, "https://m.media-amazon.com/images/I/51MhHi3YOYL._AC_SX300_SY300_.jpg",null,         true),
                new("SKU-010", "Atomic Habits by James Clear",            "Penguin",     "books",          349,    599, 4.8, 48000, "https://m.media-amazon.com/images/I/81wgcld4wxL._AC_UX300_.jpg",     "Bestseller", false),
                new("SKU-011", "Monster Trucks 5-Pack Die-Cast Vehicles", "Hot Wheels",  "toys",           899,   1299, 4.4,  7800, "https://m.media-amazon.com/images/I/81hkWxkAiEL._AC_SX300_SY300_.jpg",null,         false),
                new("SKU-012", "12R 5G (16GB RAM, 256GB Storage)",        "OnePlus",     "electronics",  39999,  49999, 4.5,  9310, "https://m.media-amazon.com/images/I/71nBi-IDAOL._AC_SX300_SY300_.jpg","20% off",    false),
            };

            int added = 0;
            foreach (var dto in seeds)
            {
                if (await _repo.GetBySkuAsync(dto.Sku, ct) is not null) continue;

                var p = new Product
                {
                    Id            = Guid.NewGuid(),
                    Sku           = dto.Sku,
                    Name          = dto.Name,
                    Brand         = dto.Brand,
                    Category      = dto.Category,
                    Price         = dto.Price,
                    OriginalPrice = dto.OriginalPrice,
                    Rating        = dto.Rating,
                    ReviewCount   = dto.ReviewCount,
                    ImageUrl      = dto.ImageUrl,
                    Badge         = dto.Badge,
                    IsNew         = dto.IsNew,
                };
                await _repo.AddAsync(p, ct);
                added++;
            }

            await _uow.SaveChangesAsync(ct);
            return Ok(new { seeded = added, message = $"{added} product(s) seeded." });
        }
    }
}
