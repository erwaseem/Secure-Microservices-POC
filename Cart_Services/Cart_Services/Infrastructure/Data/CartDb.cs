using Cart_Services.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart_Services.Infrastructure.Data
{
    public class CartDb : DbContext
    {
        public CartDb(DbContextOptions<CartDb> options) : base(options) { }

        public DbSet<Cart>     Carts     => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Cart>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.SessionId).IsUnique();
                e.Property(c => c.SessionId).HasMaxLength(256);
                e.HasMany(c => c.Items)
                 .WithOne(i => i.Cart)
                 .HasForeignKey(i => i.CartId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<CartItem>(e =>
            {
                e.HasKey(i => i.Id);
                e.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
                e.Property(i => i.ProductId).HasMaxLength(64);
                e.Property(i => i.Name).HasMaxLength(200);
                e.Property(i => i.Brand).HasMaxLength(100);
                e.Property(i => i.ImageUrl).HasMaxLength(500);
                e.Property(i => i.Price).HasColumnType("decimal(18,2)");
                e.Property(i => i.OriginalPrice).HasColumnType("decimal(18,2)");
            });
        }
    }
}
