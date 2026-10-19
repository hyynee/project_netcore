# ADR-003: Use layered idempotency boundaries

## Status

Accepted for MVP.

## Context

Clients, Stripe, and webhook delivery can all retry independently. A single
idempotency mechanism cannot safely cover all three retry boundaries.

## Decision

Use three distinct keys:

1. API `Idempotency-Key`, scoped to the authenticated user and request
   fingerprint, with a unique database constraint and stored result.
2. A stable Stripe idempotency key derived from the checkout operation when
   creating a Checkout Session.
3. Stripe `event.id`, stored as a unique `PaymentWebhookEvent.StripeEventId`.

The same API key with a different request fingerprint returns a conflict.
Already-completed operations return their stored result; in-progress operations
are not duplicated.

## Consequences

- Each boundary can be retried without creating a second order, session, or
  webhook side effect.
- Idempotency records need an expiry/retention policy.
- Unique-constraint violations are translated to a documented conflict response.
- Tests must cover same-key retry, same-key/different-payload, duplicate event,
  and concurrent checkout requests.
