# Yêu cầu triển khai

Nguồn chuẩn là tài liệu đặc tả phiên bản 2.0 được cung cấp cùng đề bài. Project triển khai FR01–FR11: xác thực/tài khoản, khách hàng, nhân viên, danh mục, lịch chụp, tiến độ, dịch vụ-tài chính, báo cáo, tài nguyên, nhật ký và sao lưu/phục hồi.

Các quyết định cốt lõi:

- Hoàn tiền chỉ dành cho Quản trị viên.
- Nhân viên chỉ hủy lịch Đã đặt và chưa bắt đầu; Quản trị viên có ngoại lệ trước Hoàn thành.
- Khoảng thời gian là nửa mở; hai lịch giáp ranh không trùng.
- Giá được snapshot tại lịch; thay đổi danh mục không sửa lịch sử.
- Tiến độ đi đúng một bước, không quay lại và không bỏ qua.
- Trạng thái thanh toán được tính từ giao dịch, không nhập thủ công.
- Dữ liệu có lịch sử được ngừng sử dụng thay vì xóa vật lý.
