namespace Shared.Events
{
    /// <summary>
    /// Published by Auth-Service when a new user registers successfully.
    /// Consumed by NotificationService to send a welcome message.
    /// </summary>
    public record UserRegisteredEvent(
        string   Username,
        string   FullName,
        DateTime RegisteredAtUtc
    );
}
