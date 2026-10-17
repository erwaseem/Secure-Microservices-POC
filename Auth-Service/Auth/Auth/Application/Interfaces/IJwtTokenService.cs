using System.Security.Claims;

namespace SecureMicroservicesPOC.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(IEnumerable<Claim> claims, TimeSpan? lifetime = null);
    }
}
