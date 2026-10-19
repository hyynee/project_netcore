# Phase 00 ERD decisions

The following Mermaid ERD is a design baseline, not a generated EF migration.
Foreign keys, unique indexes, check constraints and precision are specified in
Phase 02a.

```mermaid
erDiagram
    ApplicationUser ||--o| Cart : owns
    Cart ||--|{ CartItem : contains
    Product ||--o{ CartItem : referenced_by
    Category ||--o{ Product : groups
    ApplicationUser ||--o{ Order : places
    Order ||--|{ OrderItem : snapshots
    Product ||--o{ OrderItem : snapshot_source
    Order ||--|| Payment : has
    Order ||--o{ InventoryReservation : reserves
    Order ||--o{ OrderStatusHistory : records
    Payment ||--o{ PaymentWebhookEvent : receives
    Order ||--o{ InventoryAdjustment : audits
    Order ||--o{ ReconciliationIssue : raises
    ApplicationUser ||--o{ RefreshToken : owns
    ApplicationUser ||--o{ IdempotencyRecord : submits

    ApplicationUser {
        uuid Id PK
        string Email UK
        string UserName
        datetime CreatedAt
    }
    Category {
        uuid Id PK
        string Name
        bool IsActive
    }
    Product {
        uuid Id PK
        int Stock
        int ReservedQuantity
        binary RowVersion
    }
    Cart {
        uuid Id PK
        uuid UserId FK
        int Version
        binary RowVersion
    }
    CartItem {
        uuid Id PK
        uuid CartId FK
        uuid ProductId FK
        int Quantity
    }
    Order {
        uuid Id PK
        uuid UserId FK
        int CartVersion
        string Status
        decimal TotalAmount
        string Currency
        string RecipientName
        string Phone
        string AddressLine1
        string Ward
        string District
        string Province
        string PostalCode
    }
    Payment {
        uuid Id PK
        uuid OrderId FK
        string Status
        string StripeCheckoutSessionId UK
        string StripePaymentIntentId UK
    }
    PaymentWebhookEvent {
        uuid Id PK
        string StripeEventId UK
        string Status
        string EventType
        datetime ReceivedAt
        datetime ProcessedAt
        int RetryCount
        string LastError
    }
    OrderItem {
        uuid Id PK
        uuid OrderId FK
        uuid ProductId FK
        string ProductName
        decimal UnitPrice
        int Quantity
        decimal LineTotal
        string Currency
    }
    InventoryReservation {
        uuid Id PK
        uuid OrderId FK
        uuid ProductId FK
        int Quantity
        string Status
        string OperationReference UK
        datetime ExpiresAt
    }
    InventoryAdjustment {
        uuid Id PK
        uuid ProductId FK
        uuid OrderId FK
        int QuantityDelta
        string Reason
        string OperationReference UK
    }
    OrderStatusHistory {
        uuid Id PK
        uuid OrderId FK
        string FromStatus
        string ToStatus
        string Reason
        string ActorId
        datetime CreatedAt
        string CorrelationId
    }
    IdempotencyRecord {
        uuid Id PK
        uuid UserId FK
        string Key
        string RequestFingerprint
        string Status
        datetime ExpiresAt
        string ResponsePayload
    }
    ReconciliationIssue {
        uuid Id PK
        uuid OrderId FK
        uuid PaymentId FK
        string Type
        string Status
        string Severity
        string StripeObjectId
        string LastError
        string ResolutionNote
        string ResolvedBy
    }
    RefreshToken {
        uuid Id PK
        uuid UserId FK
        string TokenHash
        string FamilyId
        datetime ExpiresAt
        datetime RevokedAt
        string ReplacedByTokenHash
    }
```

`InventoryReservation` is the reservation detail; `InventoryAdjustment` is the
audit ledger. `OrderItem` stores product name, unit price, quantity, line total
and currency snapshots. Shipping fields are immutable order data. All quantity
fields use integer semantics; money fields use decimal semantics.
