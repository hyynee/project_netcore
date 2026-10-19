namespace ShopCore.Domain.Enums;

public enum ReconciliationIssueType
{
    LatePayment,
    AmountMismatch,
    CurrencyMismatch,
    DuplicateEvent,
    ProcessingFailure
}
