# ADR-007: Catalog read model and admin catalog commands

## Status

Accepted for MVP.

## Context

The storefront must list the catalog without exposing internal inventory state,
while administrators need to correct stock, rename categories and retire
products without destroying order history. The catalog is the first
high-traffic read path, so the query has to be shaped by the database.

## Decision

### Public read model

- `GET /api/v1/products`, `GET /api/v1/products/{id}` and `GET /api/v1/categories`
  are anonymous, rate limited and read a dedicated projection. The public DTO
  exposes `availableQuantity = Stock - ReservedQuantity` only; `Stock` and
  `ReservedQuantity` stay in the admin DTO.
- Queries run with `AsNoTracking`, filter `IsActive` on the product and on the
  category, and paginate in the database with `Skip`/`Take`.
- Sort keys are an allow-list (`newest`, `name`, `price`, `-price`). An unknown
  key is a `400`, never a dynamic `OrderBy` on user input.
- Page size is bounded (default 20, maximum 100) and every page is returned with
  `items`, `page`, `pageSize`, `totalCount` and `totalPages`.
- Name search is answered by the product search index described in ADR-009, not
  by a `LIKE` scan over the product names.
- A price is a whole positive amount. The request validation rejects a
  fractional price with a `400` and the database repeats the rule with the
  `CK_Products_Price_Positive` check constraint, so a price can never be
  rounded silently by the `decimal(18, 0)` column.

### Admin commands

- Admin endpoints require the `Admin` role. The role check is repeated in the
  service layer so a controller cannot expose admin data by accident.
- Products are never hard deleted. `DELETE /api/v1/admin/products/{id}`
  deactivates the product so that order history keeps its reference.
- A category is hard deleted only when no product references it. When products
  still reference the category, the category is deactivated instead and the
  response states which happened.
- `Product.RowVersion` is a `rowversion` concurrency token. Admin updates send
  the last seen token; a mismatch is rejected with `409 Conflict` so two
  administrators cannot overwrite each other.
- Stock is never edited as a raw number on an update. Inventory changes go
  through `POST /api/v1/admin/products/{id}/inventory-adjustments`, which
  appends an audit row (product, delta, type, actor, timestamp, reason and
  operation reference) and never touches `ReservedQuantity`. The resulting stock
  must stay greater than or equal to the reserved quantity.
- Every write stores the actor. `CreatedBy` and `UpdatedBy` are filled from the
  authenticated user of the request, so the audit columns are not `NULL` for
  rows that are written through the API.
- `POST /api/v1/admin/search/reindex` rebuilds the product search index when
  products were written before the index existed (ADR-009).

### Index strategy

`Products` is indexed on `(IsActive, Name)`, `(IsActive, CategoryId, Name)`,
`(IsActive, Price)`, `(IsActive, CreatedAt)` and the existing
`(CategoryId, IsActive)`. `Categories` is indexed on `(IsActive, Name)`. The
indexes match the filters and the allow-listed sort keys so the database can
satisfy filtering, ordering and pagination without a sort spill.

## Consequences

- The public catalog cannot leak reserved quantities, but it also cannot show
  an exact stock number. That is intentional.
- The sample category and product are created by the application at startup in
  the `Development` and `Testing` environments only. They are not model seed
  data any more, so a staging or production database never receives them.
- Admin writes need the rowVersion of the previous read, so every admin client
  has to round-trip the token.
- The inventory ledger only grows. Retention and reporting are separate
  concerns from this phase.
- The admin endpoints are rate limited on top of the role check. The limit is
  counted per client address and per signed in admin.
- Forwarded headers are only applied when the request comes from a known proxy
  (`Proxy:TrustForwardedHeaders` together with `Proxy:KnownProxies` or
  `Proxy:KnownNetworks`). The application applies them itself
  (`KnownProxyForwardedHeadersMiddleware`) instead of relying on the framework
  middleware, because the framework middleware uses the known addresses only to
  skip proxy hops and would accept a spoofed `X-Forwarded-For` from any direct
  client. Without the check a client could spoof its own address and bypass the
  limit. When the proxy is trusted, the right-most address that is not a known
  proxy wins, so a spoofed left-most entry is ignored.
