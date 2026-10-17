namespace Cart_Services.Domain.Entities
{
    /// <summary>Represents a single line item in a cart.</summary>
    public class CartItem
    {
        public Guid    Id { get; set; } = Guid.NewGuid();
        public Guid    CartId  { get; set; }
        public string  ProductId { get; set; } = string.Empty;  // Guid stored as string for flexibility
        public string  Name   { get; set; } = string.Empty;
        public string  Brand  { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string  ImageUrl{ get; set; } = string.Empty;
        public int     Quantity { get; set; } = 1;

        // Navigation
        public Cart Cart { get; set; } = null!;
    }
}
