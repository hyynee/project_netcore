using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class InventoryReservation : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Active;
    public string OperationReference { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
