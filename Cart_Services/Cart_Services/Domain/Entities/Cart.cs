namespace Cart_Services.Domain.Entities
{
    /// <summary>Represents a user's cart (one per session/user).</summary>
    public class Cart
    {
        public Guid   Id         { get; set; } = Guid.NewGuid();
        public string SessionId  { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public List<CartItem> Items { get; set; } = new();
    }
}
