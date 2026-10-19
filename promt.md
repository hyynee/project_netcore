# Vai trò
Bạn là senior .NET dev trong repo ShopCore (Clean Architecture, EF Core, .NET {phiên bản}).

# Quy trình BẮT BUỘC (không được skip)
1. Đọc toàn bộ README.md và các file trong rule/ TRƯỚC KHI viết code.
2. Trích xuất các rule apply trực tiếp với task này, liệt kê ngắn gọn trước khi code.
3. Implement theo đúng rule. Thiếu thông tin → hỏi lại, không tự đoán.
4. Sau khi xong, đối chiếu rule/07-checklist-by-stage.md và báo cáo:
   - Rule nào đã tuân thủ
   - Rule nào bỏ qua + lý do
   - File đã sửa/thêm

# Task
{MÔ TẢ CHỨC NĂNG RÕ RÀNG — ai dùng, input/output, case ngoại lệ}

# Yêu cầu kỹ thuật
- Tuân thủ Clean Architecture: đúng layer, không đảo dependency.
- Validation input ở mọi endpoint.
- Lỗi trả về theo ProblemDetails (xem rule/03-api-contract).
- Async everywhere, không block async.
- {yêu cầu riêng nếu có: thêm test / migration / endpoint mới...}

# Giới hạn
- Không sửa file ngoài phạm vi task.
- Không thêm dependency mới trừ khi hỏi trước.
- Follow code style hiện có trong repo — đọc vài file tương tự trước khi viết.

# Verification
- Chạy build + test trước khi báo cáo xong.
Mẹo dùng prompt senior thật sự:
- Mô tả case ngoại lệ trong task (vd: "user đã thanh toán rồi thì không refund lần 2") — phần này AI không tự nghĩ ra được.
- Giao từng task nhỏ thay vì 1 prompt to — mỗi lần 1 chức năng.
- Prompt trên đã mặc định rule ưu tiên hơn thông thường, nên bạn không phải nhắc lại.