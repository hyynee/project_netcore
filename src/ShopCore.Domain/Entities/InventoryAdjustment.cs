namespace ShopCore.Domain.Entities;

public sealed class InventoryAdjustment : Entity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string OperationReference { get; set; } = string.Empty;
}
