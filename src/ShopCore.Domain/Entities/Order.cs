using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class Order : Entity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int CartVersion { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public Payment Payment { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<InventoryReservation> InventoryReservations { get; set; } = [];
    public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<ReconciliationIssue> ReconciliationIssues { get; set; } = [];
}
