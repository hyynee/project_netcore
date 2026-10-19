# ShopCore Backend SRS Baseline

> Baseline reconstructed from the approved SRS supplied during project planning. This file is the
> repository source of truth for the requirement identifiers referenced by the phase documents.

## Scope

ShopCore is a modular-monolith e-commerce backend using ASP.NET Core Web API, SQL Server,
ASP.NET Core Identity/JWT and Stripe-hosted Checkout. COD is superseded by Stripe.

## Functional requirements

| ID | Requirement |
|---|---|
| FR-01 | Guests and customers can browse active categories and products. |
| FR-02 | Catalog supports bounded pagination, filtering and allow-listed sorting. |
| FR-03 | Guests can maintain a local cart and customers can maintain a server cart. |
| FR-04 | Customers can register and authenticate securely. |
| FR-05 | Protected APIs enforce identity, role and ownership boundaries. |
| FR-06 | Guest cart merge is validated, bounded and retry-safe. |
| FR-07 | Customers can create orders through an idempotent checkout workflow. |
| FR-08 | Stripe Checkout Session and verified webhooks determine payment outcome; COD is not implemented. |
| FR-09 | Customers can view only their own orders and order details. |
| FR-10 | Admins can list and transition orders according to the state machine. |
| FR-11 | Admins can manage categories. |
| FR-12 | Admins can manage products and activation state. |
| FR-13 | Admins can adjust inventory with reason and audit history. |
| FR-14 | Checkout reservation prevents overselling under concurrent requests. |
| FR-15 | API returns consistent validation, conflict, authorization and server-error responses. |
| FR-16 | The system records audit, operational and security-relevant events without secrets. |
| FR-17 | Business rules have unit, integration and API/concurrency test coverage by risk. |
| FR-18 | SQL Server persistence, migrations and API integration behavior are verified. |
| FR-19 | The project has documented build, test, deployment, rollback and operational procedures. |

## Business rules

| ID | Rule |
|---|---|
| BR-01 | Prices, totals, roles and stock are decided by the backend, never by the client. |
| BR-02 | Guest cart data is untrusted input and is validated during merge/checkout. |
| BR-03 | Checkout requires an authenticated customer and a current cart version. |
| BR-04 | Retries with the same idempotency key/fingerprint do not create duplicate orders. |
| BR-05 | Order items store product name, unit price, quantity, line total and currency snapshots. |
| BR-06 | Reserve increments `ReservedQuantity` only when `Stock - ReservedQuantity >= quantity`. |
| BR-07 | Consume decrements both `Stock` and `ReservedQuantity`; release decrements only `ReservedQuantity`. |
| BR-08 | Release, consume, cancellation and refund inventory effects are applied at most once. |
| BR-09 | User-scoped queries use the authenticated user claim, never a request-supplied user id. |
| BR-10 | Admin operations require policy/role authorization and optimistic concurrency handling. |
| BR-11 | Historical products/categories/orders are deactivated or transitioned, not silently hard-deleted. |
| BR-12 | Invalid order/payment/reservation transitions are rejected; verified Stripe events are authoritative. |
| BR-13 | Input limits, DTO binding, pagination and status codes follow the API contract. |
| BR-14 | Secrets, card data, tokens and sensitive payloads are not committed or logged. |

## Approved MVP decisions

- Currency: `VND`; store `Currency = "VND"` and convert to Stripe zero-decimal minor units.
- Shipping snapshot: `RecipientName`, `Phone`, `AddressLine1`, `Ward`, `District`,
  `Province`, optional `PostalCode`.
- Reservation TTL: 30 minutes, aligned with the minimum Stripe Checkout `expires_at`.
- Pricing: `Tax = 0`, `ShippingFee = 0`, `Discount = 0`; `Order.TotalAmount = SUM(OrderItem.LineTotal)`.
- Display timezone: `Asia/Ho_Chi_Minh`; persisted timestamps remain UTC.
- Quantity limits: integer quantities, maximum 99 units per item and 100 distinct items per cart.
- Refund: manual Stripe Dashboard refund; local payment state becomes `RefundRequired`.
- Late payment after release/cancellation: retain order as `Cancelled`, set payment to
  `Succeeded`, create `ReconciliationIssue`, do not fulfill or re-reserve.
- Cart concurrency: `Cart.Version` is a numeric business version incremented after each successful
  mutation; `Cart.RowVersion` is a separate SQL Server optimistic concurrency token. Checkout
  snapshots `Order.CartVersion` and rejects stale versions.
