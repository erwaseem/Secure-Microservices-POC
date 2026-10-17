using Cart_Services.Domain.Entities;

namespace Cart_Services.Application.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default);
        Task<Cart>  GetOrCreateAsync(string sessionId, CancellationToken ct = default);
        Task        SaveAsync(CancellationToken ct = default);
    }
}
