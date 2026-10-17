namespace ProductService.Contracts
{
    public record ProductCreated(
     Guid Id,
     string Sku,
     string Name,
     decimal Price,
     DateTime OccurredUtc
 );
}
