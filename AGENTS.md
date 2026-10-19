# Quy tắc bắt buộc khi thực hiện task

Mọi task implementation (thêm/sửa chức năng, fix bug, refactor) PHẢI tuân thủ quy trình sau:

## 1. Đọc tài liệu trước khi code

- Đọc toàn bộ `README.md` và TẤT CẢ các file `.md` trong thư mục `rule/`.
- Không viết bất kỳ dòng code nào trước bước này.

## 2. Trích xuất rule áp dụng

- Với task được giao, liệt kê ngắn gọn các rule APPLY trực tiếp (security, API contract, database, testing, git...).
- Implement đúng các rule đã liệt kê.
- Rule ưu tiên hơn cách làm thông thường. Nếu thiếu thông tin → hỏi lại, không tự đoán.

## 3. Tự đối chiếu sau khi xong

- Đối chiếu kết quả với `rule/07-checklist-by-stage.md`.
- Báo cáo: rule nào đã tuân thủ, rule nào bỏ qua và lý do.
