using Microsoft.EntityFrameworkCore;
using SecureMicroservicesPOC.Domain.Entities;

namespace SecureMicroservicesPOC.Infrastructure.Data
{
    public class AuthDb : DbContext
    {
        public AuthDb(DbContextOptions<AuthDb> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Username).IsUnique();
                e.Property(u => u.Username).HasMaxLength(100);
                e.Property(u => u.Password).HasMaxLength(200);
                e.Property(u => u.FullName).HasMaxLength(200);
                e.Property(u => u.Role).HasMaxLength(50);
            });
        }
    }
}
