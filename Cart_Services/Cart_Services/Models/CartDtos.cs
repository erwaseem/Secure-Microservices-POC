namespace Cart_Services.Models
{
    // ── Request DTOs ─────────────────────────────────────────────────────────

    public record AddToCartRequest(
        string  ProductId,
        string  Name,
        string  Brand,
        decimal Price,
        decimal OriginalPrice,
        string  ImageUrl,
        int     Quantity = 1);

    public record UpdateQuantityRequest(
        string ProductId,
        int    Delta);

    public record RemoveItemRequest(
        string ProductId);

    // ── Response DTOs ────────────────────────────────────────────────────────

    public record CartItemDto(
        string  ProductId,
        string  Name,
        string  Brand,
        decimal Price,
        decimal OriginalPrice,
        string  ImageUrl,
        int     Quantity,
        decimal LineTotal,
        decimal LineSaving);

    public record CartDto(
        string           SessionId,
        List<CartItemDto> Items,
        decimal          SubTotal,
        decimal          TotalDiscount,
        decimal          GrandTotal,
        int              TotalItems);
}
