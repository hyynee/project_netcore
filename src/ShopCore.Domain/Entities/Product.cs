namespace ShopCore.Domain.Entities;

public sealed class Product : Entity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int Stock { get; set; }
    public int ReservedQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = [];
    public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = [];
}
