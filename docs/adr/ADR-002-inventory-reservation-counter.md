# ADR-002: Reserve inventory with a counter and conditional update

## Status

Accepted for MVP.

## Context

Two checkout requests can observe the same available quantity. A transaction
around a read and a later write does not by itself prevent overselling.

## Decision

Keep `Product.Stock` as physical stock and `Product.ReservedQuantity` as an
atomic counter. Define:

```text
Available = Stock - ReservedQuantity
```

Reserve with a conditional update equivalent to:

```text
ReservedQuantity += quantity
WHERE Stock - ReservedQuantity >= quantity AND IsActive = 1
```

The affected-row count is the concurrency result. Zero affected rows becomes
`409 Conflict`. `InventoryReservation` records the order-level reservation;
`InventoryAdjustment` remains an audit ledger and is not used as the
reservation source of truth.

## Consequences

- Concurrent checkout cannot make the counter exceed available stock.
- The database must enforce non-negative and
  `ReservedQuantity <= Stock` checks.
- Reserve, consume and release operations need an operation reference so
  retries are idempotent.
- SQL Server integration and concurrency tests are mandatory; EF Core
  InMemory is not sufficient.
