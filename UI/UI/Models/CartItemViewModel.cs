using System.Text.Json.Serialization;

namespace UI.Models
{
    /// <summary>Represents a single line in the shopping cart.</summary>
    public class CartItemViewModel
    {
        [JsonPropertyName("productId")]
        public string  ProductId     { get; set; } = string.Empty;

        [JsonPropertyName("brand")]
        public string  Brand         { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string  Name          { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price         { get; set; }

        [JsonPropertyName("originalPrice")]
        public decimal OriginalPrice { get; set; }

        [JsonPropertyName("imageUrl")]
        public string  ImageUrl      { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int     Quantity      { get; set; }

        /// <summary>Total cost for this line at the discounted price.</summary>
        public decimal LineTotal  => Price * Quantity;

        /// <summary>Total saving on this line vs. original price.</summary>
        public decimal LineSaving => (OriginalPrice - Price) * Quantity;
    }
}
