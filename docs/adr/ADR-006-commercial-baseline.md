# ADR-006: Commercial and quantity baseline

## Status

Accepted for MVP.

## Context

Currency, reservation lifetime, shipping fields and quantity limits affect
database types, order totals, cart validation and Stripe line items. Leaving
these values implicit would force Phase 02a and Phase 05a to make incompatible
assumptions.

## Decision

- Currency is `VND`, represented as a zero-decimal Stripe currency. The server
  is the only source of currency and converts VND amounts to Stripe minor
  units without fractional digits.
- Checkout reservation lifetime is 30 minutes, matching Stripe Checkout's
  minimum `expires_at`. A shorter business timeout requires proactively
  expiring the Stripe Session and processing its webhook.
- MVP `Tax`, `ShippingFee` and `Discount` are all zero.
- `Order.TotalAmount` is the sum of server-calculated `OrderItem.LineTotal`
  values.
- Shipping is an immutable Vietnamese address snapshot:
  `RecipientName`, `Phone`, `AddressLine1`, `Ward`, `District`, `Province` and
  optional `PostalCode`.
- Display timezone is `Asia/Ho_Chi_Minh`; persisted timestamps remain UTC.
- MVP quantity is an integer. The maximum is 99 units per item and 100 distinct
  items per cart.

## Consequences

- `Product.Stock`, `Product.ReservedQuantity`, cart quantity, order quantity
  and reservation quantity use integer semantics.
- Money remains decimal in the domain and uses zero-decimal VND persistence
  precision.
- Changing currency, shipping, pricing or limits requires requirement, API and
  schema review.
