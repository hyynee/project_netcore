using ShopCore.Domain.Enums;

namespace ShopCore.Domain.Entities;

public sealed class ReconciliationIssue : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public ReconciliationIssueType Type { get; set; }
    public ReconciliationIssueStatus Status { get; set; } = ReconciliationIssueStatus.Open;
    public ReconciliationIssueSeverity Severity { get; set; } = ReconciliationIssueSeverity.Medium;
    public string? StripeObjectId { get; set; }
    public string? LastError { get; set; }
    public string? ResolutionNote { get; set; }
    public string? ResolvedBy { get; set; }
}
