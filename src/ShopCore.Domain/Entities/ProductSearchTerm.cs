namespace ShopCore.Domain.Entities;

public sealed class ProductSearchTerm
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Term { get; set; } = string.Empty;
}
