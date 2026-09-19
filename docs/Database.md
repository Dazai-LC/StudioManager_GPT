# Thiết kế cơ sở dữ liệu

CSDL `StudioManager` dùng khóa chính Identity và mã nghiệp vụ duy nhất. Các bảng chính: `NhanVien`, `TaiKhoan`, `KhachHang`, `GoiChup`, `DichVu`, `PhongChup`, `TaiNguyen`, `LichChup`, `LichChupDichVu`, `PhanCongTaiNguyen`, `ThanhToan`, `HoanTien`, `NhatKyHeThong`, `NhatKySaoLuu`.

Tiền dùng `decimal(18,2)`, thời điểm dùng `datetime2(0)`. Index phục vụ tra cứu tên/điện thoại, xung đột lịch, giao dịch và nhật ký. CHECK constraint bảo vệ giá trị âm, trạng thái, thời lượng và khoảng thời gian. Các thao tác lịch/tài chính nhiều bảng được khóa và chạy trong transaction.

Không chỉnh sửa trực tiếp giao dịch tài chính hoặc nhật ký. Không đưa mật khẩu hoặc chuỗi kết nối thật vào source control.
