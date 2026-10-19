using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class OrderStatusHistory : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
