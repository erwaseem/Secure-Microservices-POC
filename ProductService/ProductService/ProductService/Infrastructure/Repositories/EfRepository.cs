using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Infrastructure.Data;
using System.Linq.Expressions;

namespace ProductService.Infrastructure.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        protected readonly ProductDb _db;
        protected readonly DbSet<T> _set;
        public EfRepository(ProductDb db) { _db = db; _set = db.Set<T>(); }

        public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => _set.FindAsync([id], ct).AsTask();
        public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) => await _set.AsNoTracking().ToListAsync(ct);
        public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);
        public async Task<T> AddAsync(T entity, CancellationToken ct = default) { await _set.AddAsync(entity, ct); return entity; }
        public void Update(T entity) => _set.Update(entity);
        public void Remove(T entity) => _set.Remove(entity);
    }
}
