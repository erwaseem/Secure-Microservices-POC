using System.Net.Http.Json;
using System.Text.Json;
using UI.Models;

namespace UI.Services
{
    /// <summary>Calls Auth-Service via API Gateway for login and registration.</summary>
    public sealed class AuthService : IAuthService
    {
        private readonly HttpClient           _http;
        private readonly ILogger<AuthService> _logger;

        public AuthService(HttpClient http, ILogger<AuthService> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, AuthResponse? Data)> LoginAsync(
            string username, string password, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    "auth/Auth/login",
                    new { username, password },
                    ct);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
                    return (true, null, data);
                }

                var body    = await response.Content.ReadAsStringAsync(ct);
                string? message = null;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var errDoc = JsonDocument.Parse(body);
                        message = errDoc.RootElement.TryGetProperty("error", out var e)
                                  ? e.GetString() : null;
                    }
                    catch (JsonException) { /* non-JSON body — fall through to default */ }
                }
                return (false, message ?? "Login failed.", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login request failed");
                return (false, "Unable to reach auth service. Please try again.", null);
            }
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(
            string username, string password, string fullName, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    "auth/Auth/register",
                    new { username, password, fullName },
                    ct);

                if (response.IsSuccessStatusCode)
                    return (true, null);

                var body    = await response.Content.ReadAsStringAsync(ct);
                string? message = null;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var errDoc = JsonDocument.Parse(body);
                        message = errDoc.RootElement.TryGetProperty("error", out var e)
                                  ? e.GetString() : null;
                    }
                    catch (JsonException) { /* non-JSON body — fall through to default */ }
                }
                return (false, message ?? "Registration failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register request failed");
                return (false, "Unable to reach auth service. Please try again.");
            }
        }
    }
}
