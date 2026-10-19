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