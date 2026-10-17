using UI.Models;

namespace UI.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, AuthResponse? Data)> LoginAsync(string username, string password, CancellationToken ct = default);
        Task<(bool Success, string? Error)> RegisterAsync(string username, string password, string fullName, CancellationToken ct = default);
    }
}
