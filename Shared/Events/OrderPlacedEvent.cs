namespace Shared.Events
{
    /// <summary>
    /// Published by Cart_Services when a user places an order (cart is cleared).
    /// Consumed by NotificationService to send an order confirmation.
    /// </summary>
    public record OrderPlacedEvent(
        string   SessionId,
        decimal  GrandTotal,
        int      TotalItems,
        DateTime PlacedAtUtc
    );
}
