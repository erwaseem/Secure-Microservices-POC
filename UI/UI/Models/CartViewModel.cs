using System.Text.Json.Serialization;

namespace UI.Models
{
    /// <summary>The full cart, passed as the view-model to Cart.cshtml.</summary>
    public class CartViewModel
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<CartItemViewModel> Items { get; set; } = new();

        public decimal SubTotal      => Items.Sum(i => i.OriginalPrice * i.Quantity);
        public decimal TotalDiscount => Items.Sum(i => i.LineSaving);
        public decimal GrandTotal    => SubTotal - TotalDiscount;
        public int     TotalItems    => Items.Sum(i => i.Quantity);
    }
}
