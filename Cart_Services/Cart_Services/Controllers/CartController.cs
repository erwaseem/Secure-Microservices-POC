using Cart_Services.Application.Interfaces;
using Cart_Services.Domain.Entities;
using Cart_Services.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Events;

namespace Cart_Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository  _repo;
        private readonly IPublishEndpoint _publishEndpoint;

        public CartController(ICartRepository repo, IPublishEndpoint publishEndpoint)
        {
            _repo            = repo;
            _publishEndpoint = publishEndpoint;
        }

        // ------- GET /api/cart/{sessionId} --------
        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetCart(string sessionId, CancellationToken ct)
        {
            var cart = await _repo.GetOrCreateAsync(sessionId, ct);
            return Ok(MapToDto(cart));
        }

        // ------- POST /api/cart/{sessionId}/items ---------
        [HttpPost("{sessionId}/items")]
        public async Task<IActionResult> AddItem(
            string sessionId,
            [FromBody] AddToCartRequest req,
            CancellationToken ct)
        {
            if (req.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");

            var cart     = await _repo.GetOrCreateAsync(sessionId, ct);
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);

            if (existing is not null)
            {
                existing.Quantity      += req.Quantity;
                existing.Price          = req.Price;
                existing.OriginalPrice  = req.OriginalPrice;
                cart.UpdatedUtc         = DateTime.UtcNow;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    CartId        = cart.Id,
                    ProductId     = req.ProductId,
                    Name          = req.Name,
                    Brand         = req.Brand,
                    Price         = req.Price,
                    OriginalPrice = req.OriginalPrice,
                    ImageUrl      = req.ImageUrl,
                    Quantity      = req.Quantity,
                });
                cart.UpdatedUtc = DateTime.UtcNow;
            }
            try
            {
                await _repo.SaveAsync(ct);
            }
            catch(Exception e)
            {
                throw e;
            }
            return Ok(MapToDto(cart));
        }

        // --------- PATCH /api/cart/{sessionId}/items --------
        [HttpPatch("{sessionId}/items")]
        public async Task<IActionResult> UpdateQuantity(
            string sessionId,
            [FromBody] UpdateQuantityRequest req,
            CancellationToken ct)
        {
            var cart = await _repo.GetBySessionAsync(sessionId, ct);
            if (cart is null) return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);
            if (item is null) return NotFound("Item not found in cart.");

            item.Quantity += req.Delta;

            if (item.Quantity <= 0)
                cart.Items.Remove(item);

            cart.UpdatedUtc = DateTime.UtcNow;
            await _repo.SaveAsync(ct);
            return Ok(MapToDto(cart));
        }

        // ----- DELETE /api/cart/{sessionId}/items/{productId} -------
        [HttpDelete("{sessionId}/items/{productId}")]
        public async Task<IActionResult> RemoveItem(
            string sessionId,
            string productId,
            CancellationToken ct)
        {
            var cart = await _repo.GetBySessionAsync(sessionId, ct);
            if (cart is null) return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is null) return NotFound("Item not found in cart.");

            cart.Items.Remove(item);
            cart.UpdatedUtc = DateTime.UtcNow;
            await _repo.SaveAsync(ct);
            return Ok(MapToDto(cart));
        }

        // ---------- DELETE /api/cart/{sessionId} ------
        // This is triggered when the user places an order (PlaceOrder in UI).
        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> ClearCart(string sessionId, CancellationToken ct)
        {
            var cart = await _repo.GetBySessionAsync(sessionId, ct);
            if (cart is null) return NotFound("Cart not found.");

            // Snapshot totals before clearing
            var dto = MapToDto(cart);

            cart.Items.Clear();
            cart.UpdatedUtc = DateTime.UtcNow;
            await _repo.SaveAsync(ct);

            //  Publish OrderPlacedEvent to RabbitMQ
            await _publishEndpoint.Publish(new OrderPlacedEvent(
                sessionId,
                dto.GrandTotal,
                dto.TotalItems,
                DateTime.UtcNow
            ), ct);

            return Ok(MapToDto(cart));
        }

        // ---------- Private helpers --------------

        private static CartDto MapToDto(Cart cart)
        {
            var items = cart.Items.Select(i => new CartItemDto(
                i.ProductId,
                i.Name,
                i.Brand,
                i.Price,
                i.OriginalPrice,
                i.ImageUrl,
                i.Quantity,
                i.Price         * i.Quantity,
                (i.OriginalPrice - i.Price) * i.Quantity
            )).ToList();

            decimal subTotal      = items.Sum(i => i.OriginalPrice * i.Quantity);
            decimal totalDiscount = items.Sum(i => i.LineSaving);
            decimal grandTotal    = subTotal - totalDiscount;
            int     totalItems    = items.Sum(i => i.Quantity);

            return new CartDto(cart.SessionId, items, subTotal, totalDiscount, grandTotal, totalItems);
        }
    }
}
