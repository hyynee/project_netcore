# 07 – Checklist theo giai đoạn

Đánh dấu `[x]` khi hoàn thành. Làm tuần tự: xong giai đoạn này mới sang giai đoạn sau.

## Giai đoạn 1 – MVP (chạy đúng)

- [ ] Nullable bật, không dùng `!` để che cảnh báo
- [ ] Controller mỏng, nghiệp vụ nằm ở Application/Domain
- [ ] Request/Response DTO, validate ở backend
- [ ] Đăng ký/đăng nhập bằng ASP.NET Core Identity, mật khẩu được hash
- [ ] JWT có validate issuer/audience/lifetime; key không nằm trong source
- [ ] Giá, tổng tiền, tồn kho do backend quyết định
- [ ] `decimal` cho tiền, `HasPrecision` đã khai báo
- [ ] Phân quyền theo role và quyền sở hữu (user chỉ thấy đơn của mình)
- [ ] EF Core migrations, `AsNoTracking` cho truy vấn đọc, có phân trang
- [ ] `dotnet build` và `dotnet test` chạy xanh

## Giai đoạn 2 – Portfolio mạnh (đáng tin)

- [ ] Exception handler tập trung + ProblemDetails, không lộ stack trace
- [ ] Trừ tồn kho nguyên tử (hoặc rowversion) + `CHECK (Stock >= 0)`
- [ ] Checkout trong một transaction, lưu snapshot giá vào `OrderItem`
- [ ] `Idempotency-Key` cho tạo đơn, unique index
- [ ] Luật chuyển trạng thái đơn trong domain
- [ ] Serilog + correlation/trace ID, không log dữ liệu nhạy cảm
- [ ] Health checks live/ready
- [ ] Rate limiting cho đăng nhập, lockout bật
- [ ] CORS chặt, HTTPS redirect, giới hạn kích thước request
- [ ] Unit test cho domain; integration test với SQL Server thật (Testcontainers)
- [ ] Test đồng thời cho checkout và test authorization (user A vs user B)
- [ ] `TreatWarningsAsErrors`, analyzers, `.editorconfig`
- [ ] CI chạy build + test + format + kiểm tra gói lỗ hổng
- [ ] README rõ ràng, `docker compose up` chạy được, OpenAPI, vài ADR
- [ ] Nhánh + PR + Conventional Commits (kể cả khi làm một mình)

## Giai đoạn 3 – Sẵn sàng production thật

- [ ] Refresh token có xoay vòng và thu hồi; access token ngắn hạn
- [ ] Secret qua secret manager; xoay secret có quy trình
- [ ] Security headers, rà soát theo OWASP Top 10/ASVS
- [ ] Dependabot + secret scanning/push protection
- [ ] OpenTelemetry (trace + metrics), dashboard và cảnh báo
- [ ] Versioning API, changelog, chính sách tương thích ngược
- [ ] Resilience (timeout, retry có backoff, circuit breaker) cho dịch vụ ngoài
- [ ] Outbox/job nền cho email, thông báo, tích hợp thanh toán
- [ ] Index theo truy vấn thực tế, đã xem execution plan, không còn N+1
- [ ] Audit trail và chính sách lưu/xóa dữ liệu cá nhân
- [ ] Backup tự động và **đã thử restore**; có RPO/RTO
- [ ] CI/CD có staging, smoke test, rollback; migration triển khai có kiểm soát
- [ ] Load test cho luồng chính (duyệt hàng, checkout)
- [ ] Code review bởi người có kinh nghiệm; kế hoạch xử lý sự cố (runbook)

## Ba câu hỏi cho mọi đoạn code quan trọng

1. Nó chịu trách nhiệm gì?
2. Trường hợp nào nó có thể thất bại?
3. Làm thế nào để kiểm chứng nó hoạt động đúng?

> Checklist không bảo đảm không có bug. Review, test, kiểm tra bảo mật và hiểu nghiệp vụ vẫn là bắt buộc.