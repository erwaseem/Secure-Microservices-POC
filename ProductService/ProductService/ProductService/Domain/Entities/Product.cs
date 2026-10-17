using Microsoft.EntityFrameworkCore;

namespace ProductService.Domain.Entities
{
    [Index(nameof(Sku), IsUnique = true)]
    [Index(nameof(Name))]
    [Index(nameof(Category))]
    public class Product
    {
        public Guid     Id            { get; set; }
        public string   Sku           { get; set; } = null!;
        public string   Name          { get; set; } = null!;
        public string   Brand         { get; set; } = string.Empty;
        public string   Category      { get; set; } = string.Empty;
        public decimal  Price         { get; set; }
        public decimal  OriginalPrice { get; set; }
        public double   Rating        { get; set; }
        public int      ReviewCount   { get; set; }
        public string   ImageUrl      { get; set; } = string.Empty;
        public string?  Badge         { get; set; }
        public bool     IsNew         { get; set; }
        public DateTime CreatedUtc    { get; set; } = DateTime.UtcNow;
    }
}
