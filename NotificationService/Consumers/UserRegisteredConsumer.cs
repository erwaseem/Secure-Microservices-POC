using MassTransit;
using Shared.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Listens to UserRegisteredEvent published by Auth-Service.
    /// Triggered when a new user registers successfully.
    /// In production: send a welcome email.
    /// </summary>
    public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
    {
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
            => _logger = logger;

        public Task Consume(ConsumeContext<UserRegisteredEvent> context)
        {
            var e = context.Message;

            _logger.LogInformation(
                "[USER REGISTERED] Username={Username} | FullName={FullName} | At={At}",
                e.Username,
                e.FullName,
                e.RegisteredAtUtc);

            // ── TODO: replace log with real notification ──────────────────────
            // await _emailService.SendWelcomeEmailAsync(e.Username, e.FullName);

            return Task.CompletedTask;
        }
    }
}
