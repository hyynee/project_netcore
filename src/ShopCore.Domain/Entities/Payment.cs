using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class Payment : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public ICollection<PaymentWebhookEvent> WebhookEvents { get; set; } = [];
    public ICollection<ReconciliationIssue> ReconciliationIssues { get; set; } = [];
}
