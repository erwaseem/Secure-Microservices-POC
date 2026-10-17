using MassTransit;
using Shared.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Listens to ProductChangedEvent published by ProductService.
    /// Triggered when a product is created or deleted.
    /// In production: update search index, notify admins, adjust inventory.
    /// </summary>
    public class ProductChangedConsumer : IConsumer<ProductChangedEvent>
    {
        private readonly ILogger<ProductChangedConsumer> _logger;

        public ProductChangedConsumer(ILogger<ProductChangedConsumer> logger)
            => _logger = logger;

        public Task Consume(ConsumeContext<ProductChangedEvent> context)
        {
            var e = context.Message;

            _logger.LogInformation(
                "[PRODUCT {ChangeType}] Id={Id} | Name={Name} | Brand={Brand} | Category={Category} | Price=₹{Price} | At={At}",
                e.ChangeType.ToUpper(),
                e.ProductId,
                e.ProductName,
                e.Brand,
                e.Category,
                e.Price,
                e.ChangedAtUtc);

            // ── TODO: replace log with real actions ───────────────────────────
            // if (e.ChangeType == "created") await _searchIndex.AddAsync(e);
            // if (e.ChangeType == "deleted") await _searchIndex.RemoveAsync(e.ProductId);

            return Task.CompletedTask;
        }
    }
}
