using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class IdempotencyRecord : Entity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.InProgress;
    public DateTime ExpiresAt { get; set; }
    public string? ResponsePayload { get; set; }
}
