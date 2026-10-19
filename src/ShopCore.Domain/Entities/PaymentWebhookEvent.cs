using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class PaymentWebhookEvent : Entity
{
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public string StripeEventId { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Received;
    public string EventType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
