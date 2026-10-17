using MassTransit;
using Shared.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Listens to OrderPlacedEvent published by Cart_Services.
    /// Triggered when a user clicks "Place Order" and the cart is cleared.
    /// In production: send order confirmation email / SMS.
    /// </summary>
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
            => _logger = logger;

        public Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var e = context.Message;

            _logger.LogInformation(
                "[ORDER PLACED] SessionId={SessionId} | Items={Items} | Total=₹{Total} | At={At}",
                e.SessionId,
                e.TotalItems,
                e.GrandTotal,
                e.PlacedAtUtc);

            //  TODO: replace log with real notification 
            // await _emailService.SendOrderConfirmationAsync(e.SessionId, e.GrandTotal);
            // await _smsService.SendAsync($"Your order of ₹{e.GrandTotal} has been placed!");

            return Task.CompletedTask;
        }
    }
}
