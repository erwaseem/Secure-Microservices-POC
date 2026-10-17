using Cart_Services.Application.Interfaces;
using Cart_Services.Domain.Entities;
using Cart_Services.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cart_Services.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartDb _db;

        public CartRepository(CartDb db) => _db = db;

        public Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default)
            => _db.Carts
                  .Include(c => c.Items)
                  .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        public async Task<Cart> GetOrCreateAsync(string sessionId, CancellationToken ct = default)
        {
            var cart = await GetBySessionAsync(sessionId, ct);
            if (cart is not null) return cart;

            cart = new Cart { SessionId = sessionId };
            _db.Carts.Add(cart);
            // Don't save here — let the caller's SaveAsync commit everything in one transaction
            return cart;
        }

        public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
