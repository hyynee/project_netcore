# ShopCore – Bộ rule bổ sung

Bộ file này **bổ sung** cho bản "ShopCore coding rules" ban đầu (nền tảng: input, mật khẩu, nhất quán dữ liệu, C#, EF Core, Clean Architecture, Git). Bản gốc chiếm khoảng 40–50% chuẩn production; các file dưới đây lấp phần còn thiếu.

## Mục lục

| File | Nội dung |
|---|---|
| `01-security.md` | Bảo mật nâng cao: JWT, rate limit, CORS, HTTPS, dependency scan |
| `02-operations.md` | Logging, health check, tracing, cấu hình, backup |
| `03-api-contract.md` | ProblemDetails, versioning, phân trang, idempotency, cancellation |
| `04-database.md` | Concurrency token, checkout an toàn, index, UTC, audit |
| `05-testing-quality.md` | Phân tầng test, Testcontainers, analyzers, coverage hợp lý |
| `06-git-ci-docs.md` | Branching, PR, CI/CD, ADR, README, OpenAPI |
| `07-checklist-by-stage.md` | Checklist theo giai đoạn MVP → Portfolio → Production |

## Cách dùng

1. Đừng cố làm hết một lúc. Dùng `07-checklist-by-stage.md` để biết giai đoạn nào cần gì.
2. Mỗi rule quan trọng phải trả lời được 3 câu: **chịu trách nhiệm gì, thất bại khi nào, kiểm chứng bằng cách nào**.
3. Rule không thay thế được code review bởi người có kinh nghiệm, test thật và hiểu nghiệp vụ.

## Điều chỉnh so với bản gốc

- **Rule 13 (bản gốc)** khuyên không cần format response lỗi thống nhất. Với API có client thật, nên thống nhất lỗi bằng ProblemDetails (RFC 9457, thay RFC 7807). Xem `03-api-contract.md`.
- **Rule 12 (bản gốc)**: `User.FindFirst(...)?.Value` có thể null. Phải xử lý null/không hợp lệ rõ ràng. Xem `01-security.md`.
- **Rule 4 (bản gốc)**: "transaction + concurrency phù hợp" cần cụ thể hóa. Xem `04-database.md`.

## Persistence local

Phase 02a uses SQL Server; EF Core InMemory is not supported for persistence tests.
Copy `.env.example` to `.env`, fill in the values and run:

```bash
docker compose up -d
dotnet tool restore
dotnet ef database update --project src/ShopCore.Infrastructure
```

`dotnet ef` reads its connection string in this order: `ConnectionStrings__DefaultConnection`,
`SHOPCORE_CONNECTION_STRING`, or `SQLSERVER_SA_PASSWORD` together with the optional
`SQLSERVER_HOST`, `SQLSERVER_PORT` and `SQLSERVER_DATABASE`. Without any of them the command
fails with an explicit error instead of falling back to a hard coded password.

The API connection string is supplied through
`ConnectionStrings__DefaultConnection`. The SQL Server container maps to port 1433 by
default. `dotnet ef migrations script --idempotent` should be reviewed before a
production migration is applied.

The sample category and product are created by the application at startup in the
`Development` and `Testing` environments only. They are not model seed data, so a
staging or production database never receives them.

### Reverse proxy and rate limiting

The forwarded header middleware is disabled by default. A client can send
`X-Forwarded-For` itself, so trusting it without a known proxy would allow a client
to spoof its address and to escape the rate limit. Enable it only behind a proxy that
overwrites the header:

```bash
export Proxy__TrustForwardedHeaders=true
export Proxy__KnownProxies="10.0.0.10"
export Proxy__KnownNetworks="10.0.0.0/8"
```

The same values can be written as a JSON array in `appsettings.json`
(`"KnownProxies": [ "10.0.0.10" ]`) or as indexed keys
(`Proxy__KnownProxies__0=10.0.0.10`). A single comma separated value is accepted as
well.

`Proxy:TrustForwardedHeaders` without at least one known proxy or network is refused at
startup. The forwarded headers are applied by the application itself and only when the
request really comes from a known proxy, so a direct client cannot spoof its address
with `X-Forwarded-For` and cannot escape the rate limit. When the proxy is trusted, the
right-most address that is not a known proxy wins.

### Authentication local configuration

JWT signing keys must come from environment variables or a secret manager outside local
development. Set at least:

```bash
export Jwt__SigningKey="$(openssl rand -base64 48)"
export Jwt__Issuer="ShopCore"
export Jwt__Audience="ShopCore.Api"
```

For a local development admin, configure user-secrets or environment variables rather than
committing credentials:

```bash
dotnet user-secrets init --project src/ShopCore.Api
dotnet user-secrets set "Identity:DevelopmentAdminEmail" "admin@example.test" \
  --project src/ShopCore.Api
dotnet user-secrets set "Identity:DevelopmentAdminPassword" "<strong-local-password>" \
  --project src/ShopCore.Api
```

`dotnet user-secrets init` only has to run once; the project already declares a
`UserSecretsId`.

The API exposes `POST /api/v1/auth/register`, `POST /api/v1/auth/login`,
`POST /api/v1/auth/refresh`, and authenticated `POST /api/v1/auth/logout`. Refresh tokens
are stored only as SHA-256 hashes, rotated on use, and revoke their family when reuse is
detected. Roles `Customer` and `Admin` are initialized outside Production when a configured
database is available. Production role/user provisioning is deployment-owned.

## Running the tests

The unit tests that need SQL Server start a Testcontainers instance, so Docker has to
run. Without Docker, or without the opt in flag, those tests are skipped instead of
failing:

```bash
SHOPCORE_RUN_SQL_TESTS=true dotnet test
```

The flag is required on purpose: a test run must never silently start a container. The
database tests apply the migrations of `src/ShopCore.Infrastructure` to the container
database, so a broken migration fails the test run and not only a manual `dotnet ef
database update`.

The OpenAPI contracts are linted with Spectral:

```bash
npm ci
npm run lint:openapi
```

## Catalog API

The storefront reads the catalog anonymously:

- `GET /api/v1/products`: active products of active categories, with `categoryId`,
  `search`, an allow-listed `sort` (`newest`, `name`, `price`, `-price`) and bounded
  pagination (`page`, `pageSize`, default 20, maximum 100).
- `GET /api/v1/products/{id}`: one public product. Inactive products and products of an
  inactive category return `404`.
- `GET /api/v1/categories`: the active categories needed by the catalog.

`search` is answered by the product search index, not by a scan over the product names.
The value is split into terms of letters and digits, every term has to match and the last
term also matches as a prefix, so `wireless headphon` finds `Wireless Headphones`. A value
without any term (`%%%`) returns no product. See
`docs/adr/ADR-009-product-search-index.md`.

A price is a whole positive amount. A fractional price is rejected with `400`, and the
database repeats the rule with the `CK_Products_Price_Positive` check constraint, so a
price is never rounded silently.

Administrators use the role protected commands:

- `GET|POST /api/v1/admin/products`, `GET|PUT|DELETE /api/v1/admin/products/{id}`.
  Updates carry the `rowVersion` of the previous read; a stale token returns `409`.
  `DELETE` deactivates the product instead of deleting it.
- `POST /api/v1/admin/products/{id}/inventory-adjustments`: audited stock delta with
  `quantityDelta`, `type`, `reason` and `operationReference`. The delta must not be zero
  and the resulting stock must stay greater than or equal to the reserved quantity.
  The reserved quantity is never modified by this command.
- `GET|POST /api/v1/admin/categories`, `GET|PUT|DELETE /api/v1/admin/categories/{id}`.
  A category without products is deleted; a category that still references products is
  deactivated and the response states which happened.
- `POST /api/v1/admin/search/reindex`: rebuilds the product search index, required when
  products were written before the index existed.

Every write stores the actor in `CreatedBy` and `UpdatedBy`. Every response uses
`application/problem+json` for errors, see `rule/03-api-contract.md`.
The endpoint contract is published in `docs/contracts/phase-03-catalog.yaml` and the
design decisions in `docs/adr/ADR-007-catalog-read-model-and-admin-commands.md`,
`docs/adr/ADR-008-culture-invariant-api-and-string-enums.md` and
`docs/adr/ADR-009-product-search-index.md`.

## API documentation

In Development or Staging, start the API and open:

- Swagger UI: `https://localhost:<port>/swagger`
- Swagger JSON: `https://localhost:<port>/swagger/v1/swagger.json`
- ASP.NET Core OpenAPI JSON (Development): `https://localhost:<port>/openapi/v1.json`

Swagger UI is intentionally disabled in Production. The generated document is versioned
as `v1`; controllers should declare their route, response status codes, validation
requirements, and authorization requirements so the contract remains useful to clients.