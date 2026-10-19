# ADR-004: Verify and process Stripe webhooks idempotently

## Status

Accepted for MVP.

## Context

Stripe webhook deliveries can be duplicated, delayed, or delivered out of
order. Deserializing or processing an unverified body would allow a caller to
forge payment state.

## Decision

Read the raw request body and verify `Stripe-Signature` before deserializing or
running business logic. Persist the verified event with a unique Stripe event
ID, acknowledge quickly, and process it through an idempotent application
service/job.

Supported payment events include:

- `checkout.session.completed`
- `checkout.session.async_payment_succeeded`
- `checkout.session.async_payment_failed`
- `checkout.session.expired`
- `payment_intent.payment_failed` for attempt telemetry only

Success events must verify payment status, amount, currency, current order
state, and active reservation. Duplicate or already processed events are
no-ops. Out-of-order events must never move state backwards.

## Consequences

- Signature verification failures do not reach payment business logic.
- Event processing can be retried from `Received` or `Failed`.
- Amount/currency mismatch creates a reconciliation incident and never marks
  an order as paid.
- A late success after release/cancellation keeps the order cancelled, creates
  a reconciliation issue, and requires manual Stripe Dashboard refund.
