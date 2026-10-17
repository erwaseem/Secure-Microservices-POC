using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories
{
    public class ProductRepository : EfRepository<Product>, IProductRepository
    {
        public ProductRepository(ProductDb db) : base(db) { }

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
            => _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);
    }

}
