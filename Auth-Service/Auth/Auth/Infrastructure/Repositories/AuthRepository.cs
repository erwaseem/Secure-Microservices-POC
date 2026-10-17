using Microsoft.EntityFrameworkCore;
using SecureMicroservicesPOC.Application.Interfaces;
using SecureMicroservicesPOC.Domain.Entities;
using SecureMicroservicesPOC.Infrastructure.Data;

namespace SecureMicroservicesPOC.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AuthDb _db;

        public AuthRepository(AuthDb db) => _db = db;

        public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
            => _db.Users.AnyAsync(u => u.Username == username, ct);

        public async Task<AppUser> RegisterAsync(
            string username, string password, string fullName, CancellationToken ct = default)
        {
            var user = new AppUser
            {
                Username   = username,
                Password   = password,   // plain text — demo only
                FullName   = fullName,
                Role       = "User",
                CreatedUtc = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public Task<AppUser?> FindByUsernameAsync(string username, CancellationToken ct = default)
            => _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        public bool CheckPassword(AppUser user, string password)
            => user.Password == password;
    }
}
