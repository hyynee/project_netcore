# ADR-001: Use Stripe-hosted Checkout

## Status

Accepted for MVP.

## Context

ShopCore needs online payment without handling card numbers, CVC, or other
payment-instrument data. The payment result must remain authoritative outside
the browser redirect flow.

## Decision

Use Stripe-hosted Checkout Sessions through an application-owned
`IPaymentGateway` abstraction.

- The application creates `Order(PendingPayment)` and an active inventory
  reservation in a local transaction.
- Stripe is called only after that transaction commits.
- The client receives only the Checkout URL and never supplies the final total,
  currency, payment status, or Stripe secret.
- Only a verified Stripe webhook can move an order to `Paid`.
- Local and staging environments use Stripe test mode. Production uses live
  mode through a secret manager.

## Consequences

- PCI scope is reduced because card data is handled by Stripe.
- The success redirect is UX only and cannot confirm payment.
- Session creation requires a stable Stripe idempotency key.
- Payment creation failures and uncertain timeouts require reconciliation.
- The application must store Stripe object identifiers, not card data.

## Rejected alternatives

- COD: superseded by the approved Stripe decision.
- Direct card collection: rejected because it expands PCI scope and secret
  handling.
- Treating the redirect as payment confirmation: rejected because it is
  client-controlled and can arrive before asynchronous payment settlement.
