using System.Text.Json.Serialization;

namespace UI.Models
{
    public class ProductViewModel
    {
        [JsonPropertyName("id")]
        public Guid    Id            { get; set; }

        [JsonPropertyName("sku")]
        public string  Sku           { get; set; } = string.Empty;

        [JsonPropertyName("brand")]
        public string  Brand         { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string  Name          { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string  Category      { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price         { get; set; }

        [JsonPropertyName("originalPrice")]
        public decimal OriginalPrice { get; set; }

        [JsonPropertyName("rating")]
        public double  Rating        { get; set; }

        [JsonPropertyName("reviewCount")]
        public int     ReviewCount   { get; set; }

        [JsonPropertyName("imageUrl")]
        public string  ImageUrl      { get; set; } = string.Empty;

        [JsonPropertyName("badge")]
        public string? Badge         { get; set; }

        [JsonPropertyName("isNew")]
        public bool    IsNew         { get; set; }

        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc   { get; set; }

        public int DiscountPercent =>
            OriginalPrice > 0
                ? (int)Math.Round((1 - Price / OriginalPrice) * 100)
                : 0;
    }
}
