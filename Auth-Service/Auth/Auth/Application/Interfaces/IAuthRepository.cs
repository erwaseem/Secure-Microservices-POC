using SecureMicroservicesPOC.Domain.Entities;

namespace SecureMicroservicesPOC.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool>      UsernameExistsAsync(string username, CancellationToken ct = default);
        Task<AppUser>   RegisterAsync(string username, string password, string fullName, CancellationToken ct = default);
        Task<AppUser?>  FindByUsernameAsync(string username, CancellationToken ct = default);
        bool            CheckPassword(AppUser user, string password);
    }
}
