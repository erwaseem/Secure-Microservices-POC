using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data
{
    public class ProductDb : DbContext
    {
        public ProductDb(DbContextOptions<ProductDb> options) : base(options) { }
        public DbSet<Product>        Products        => Set<Product>();
        public DbSet<ProductComment> ProductComments => Set<ProductComment>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Product>(e =>
            {
                e.Property(p => p.Sku).HasMaxLength(64);
                e.Property(p => p.Name).HasMaxLength(200);
                e.Property(p => p.Brand).HasMaxLength(100);
                e.Property(p => p.Category).HasMaxLength(100);
                e.Property(p => p.ImageUrl).HasMaxLength(500);
                e.Property(p => p.Badge).HasMaxLength(50);
                e.Property(p => p.Price).HasColumnType("decimal(18,2)");
                e.Property(p => p.OriginalPrice).HasColumnType("decimal(18,2)");
            });

            b.Entity<ProductComment>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.ProductId);
                e.Property(c => c.Username).HasMaxLength(100);
                e.Property(c => c.Comment).HasMaxLength(1000);
                e.HasOne(c => c.Product)
                 .WithMany()
                 .HasForeignKey(c => c.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
