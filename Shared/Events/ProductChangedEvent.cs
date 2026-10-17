namespace Shared.Events
{
    /// <summary>
    /// Published by ProductService when a product is created or deleted.
    /// Consumed by NotificationService / future InventoryService.
    /// </summary>
    public record ProductChangedEvent(
        Guid     ProductId,
        string   ProductName,
        string   Brand,
        string   Category,
        decimal  Price,
        string   ChangeType,      // "created" | "deleted"
        DateTime ChangedAtUtc
    );
}
