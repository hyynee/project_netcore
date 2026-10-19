# ADR-005: Handle late payment and refunds explicitly

## Status

Accepted for MVP.

## Context

A payment can settle after its inventory reservation has expired or after an
order was cancelled. Automatically re-reserving or fulfilling that order can
oversell inventory.

## Decision

Use two explicit operational queues:

- A deliberate cancellation of a `Paid` or `Confirmed` order sets
  `PaymentStatus=RefundRequired`, releases/restores inventory exactly once, and
  appears in the admin refund queue.
- A late payment after release or cancellation keeps
  `OrderStatus=Cancelled`, sets `PaymentStatus=Succeeded`, creates an open
  `ReconciliationIssue` of type `LatePaymentAfterRelease` or
  `CancelledOrderPayment`, and does not fulfill or re-reserve the order.

Refunds are manual through the Stripe Dashboard in the MVP. They are not
silently treated as automatic refunds.

## Consequences

- `RefundRequired` is a payment status, not an order status.
- Admin operations must expose both the refund queue and reconciliation queue.
- The state machine rejects `Cancelled -> Paid`.
- Late-payment behavior is observable and auditable.
