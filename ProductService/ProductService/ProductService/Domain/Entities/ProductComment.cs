namespace ProductService.Domain.Entities
{
    /// <summary>A user comment on a product.</summary>
    public class ProductComment
    {
        public int      Id          { get; set; }
        public Guid     ProductId   { get; set; }
        public string   Username    { get; set; } = string.Empty;
        public string   Comment     { get; set; } = string.Empty;
        public DateTime CreatedUtc  { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product Product { get; set; } = null!;
    }
}
