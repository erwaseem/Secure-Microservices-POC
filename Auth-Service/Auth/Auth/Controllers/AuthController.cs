using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SecureMicroservicesPOC.Application.Interfaces;
using Shared.Events;
using System.Security.Claims;

namespace SecureMicroservicesPOC.Controllers
{
    [Route("auth/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository  _repo;
        private readonly IJwtTokenService _jwt;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthController(IAuthRepository repo, IJwtTokenService jwt, IPublishEndpoint publishEndpoint)
        {
            _repo            = repo;
            _jwt             = jwt;
            _publishEndpoint = publishEndpoint;
        }

        // ------ POST /auth/AuthController/register -----------
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { error = "Username and password are required." });

            if (await _repo.UsernameExistsAsync(dto.Username, ct))
                return BadRequest(new { error = "Username already exists." });

            var user = await _repo.RegisterAsync(dto.Username, dto.Password, dto.FullName ?? string.Empty, ct);

            //  Publish UserRegisteredEvent to RabbitMQ
            await _publishEndpoint.Publish(new UserRegisteredEvent(
                user.Username,
                user.FullName,
                DateTime.UtcNow
            ), ct);

            return Ok(new { message = "Registered successfully.", username = user.Username });
        }

        // -- POST /auth/AuthController/login --
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
        {
            var user = await _repo.FindByUsernameAsync(dto.Username, ct);
            if (user is null || !_repo.CheckPassword(user, dto.Password))
                return Unauthorized(new { error = "Invalid username or password." });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name,           user.Username),
                new("fullName",                user.FullName),
                new(ClaimTypes.Role,           user.Role),
            };

            var token = _jwt.GenerateToken(claims, TimeSpan.FromHours(8));
            return Ok(new
            {
                access_token = token,
                username     = user.Username,
                fullName     = user.FullName,
                role         = user.Role
            });
        }
    }

    public record RegisterDto(string Username, string Password, string? FullName);
    public record LoginDto(string Username, string Password);
}
