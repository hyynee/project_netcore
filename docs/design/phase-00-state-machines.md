# Phase 00 state machines

The diagrams below are the visual form of the transition tables in
[phase-00-requirements-and-design.md](../../phases/phase-00-requirements-and-design.md).
Any transition not shown is rejected with `409 Conflict`.

## Order status

```mermaid
stateDiagram-v2
    [*] --> PendingPayment
    PendingPayment --> Paid: verified Stripe success + active reservation
    PendingPayment --> Cancelled: expiry/cancel
    PendingPayment --> Cancelled: late success guard
    Paid --> Confirmed: admin
    Paid --> Cancelled: admin cancellation
    Confirmed --> Shipped: fulfillment
    Confirmed --> Cancelled: admin cancellation
    Shipped --> Completed: delivery complete
    Cancelled --> [*]
    Completed --> [*]
```

Late success does not use the `PendingPayment -> Paid` transition. It leaves
the order cancelled and opens a reconciliation issue.

## Payment status

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Succeeded: verified success + guards
    Pending --> Failed: terminal failure
    Pending --> Expired: session expired
    Succeeded --> RefundRequired: paid/confirmed cancellation
    RefundRequired --> Refunded: manual refund confirmed
    RefundRequired --> PartiallyRefunded: partial manual refund confirmed
```

Late payment is intentionally represented as `Succeeded` plus an open
`ReconciliationIssue`; it does not reopen the order.

## Inventory reservation status

```mermaid
stateDiagram-v2
    [*] --> Active
    Active --> Consumed: payment accepted
    Active --> Released: expiry/failure/cancel
    Consumed --> Released: approved cancellation/refund policy
```

Every side effect is protected by an order/operation reference and may be
applied at most once.
