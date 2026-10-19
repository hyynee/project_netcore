namespace ShopCore.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Expired,
    RefundRequired,
    Refunded,
    PartiallyRefunded
}
