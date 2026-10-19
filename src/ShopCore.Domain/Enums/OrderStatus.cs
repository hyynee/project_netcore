namespace ShopCore.Domain.Enums;

public enum OrderStatus
{
    PendingPayment,
    Paid,
    Confirmed,
    Shipped,
    Completed,
    Cancelled
}
