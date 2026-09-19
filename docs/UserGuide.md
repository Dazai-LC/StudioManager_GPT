# Hướng dẫn sử dụng nhanh

## Nhân viên

1. Đăng nhập và xem dashboard để biết lịch hôm nay, lịch sắp tới và công nợ.
2. Vào **Lịch chụp → Tạo lịch**, chọn khách hàng, gói, thời gian, nhiếp ảnh gia và phòng. Hệ thống từ chối nếu trùng lịch.
3. Chọn một lịch và dùng **Bước tiếp** để cập nhật đúng chuỗi: Đã đặt → Đã chụp → Đang chỉnh sửa → Chờ giao → Đã giao → Hoàn thành.
4. Dùng **Thu tiền** để ghi đặt cọc hoặc thanh toán tiếp. Số tiền không được vượt số còn lại.
5. Hủy lịch Đã đặt trước giờ bắt đầu bằng nút **Hủy lịch** và nhập lý do.

## Quản trị viên

Ngoài chức năng nhân viên, Quản trị viên quản lý nhân sự/danh mục/tài khoản, xem báo cáo và nhật ký, hoàn tiền cho lịch đã hủy, sao lưu và phục hồi.

## Sao lưu và phục hồi

- Sao lưu: chọn **Sao lưu**, nhập đường dẫn `.bak` mà dịch vụ SQL Server có quyền ghi.
- Phục hồi: chọn tệp `.bak`, xác nhận hai bước; ứng dụng thay thế dữ liệu và tự khởi động lại. Có thể dùng `database/RestoreDatabase.sql` khi cần phục hồi thủ công.

## Xử lý lỗi kết nối

Kiểm tra SQL Server đang chạy, tên instance trong `appsettings.json`, chế độ xác thực và chứng chỉ (`TrustServerCertificate=True` cho môi trường học tập). Nếu dùng SQL Login, thay chuỗi kết nối bằng `Server=...;Database=StudioManager;User Id=...;Password=...;TrustServerCertificate=True` và không commit mật khẩu thật.
