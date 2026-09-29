# Nghiệm thu vận hành — FR05 đến FR11

Tài liệu này là checklist cho branch `feature/spec-v2.1-master-data`. Mọi mục chỉ được đánh dấu **Pass** sau khi chạy trên Windows với SQL Server thật tại đúng commit được kiểm thử.

## 1. Chuẩn bị

```powershell
git pull --ff-only
dotnet clean StudioManager.sln
dotnet restore StudioManager.sln
dotnet build StudioManager.sln
dotnet test StudioManager.sln
```

Kỳ vọng: build thành công và toàn bộ test pass. Kiểm tra `appsettings.json` trỏ đúng database kiểm thử. Không thực hiện restore trên database duy nhất nếu chưa tạo được backup đọc được.

## 2. Lịch chụp và tiến độ — AT12–AT23

1. Tạo lịch mới có dữ liệu hợp lệ. Lịch xuất hiện trong danh sách, chi tiết, dashboard và nhật ký.
2. Tạo một lịch trùng nhiếp ảnh gia hoặc phòng trong khoảng giao nhau: bị từ chối. Hai lịch chỉ giáp giờ kết thúc/bắt đầu: được chấp nhận.
3. Lọc lần lượt theo mã/khách/SĐT, ngày, nhiếp ảnh gia, phòng, gói và trạng thái; mỗi bộ lọc phải trả đúng dữ liệu.
4. Sửa lịch **Đã đặt**: lưu được. Sửa lịch đang xử lý bằng Nhân viên: bị chặn; bằng Quản trị viên phải nhập lý do. Lịch Hoàn thành hoặc Đã hủy: bị chặn.
5. Nếu lịch có tài nguyên đang phân công, sửa thời gian sang một khoảng làm quá tải tài nguyên: bị từ chối. Sửa sang khoảng hợp lệ: phần Tài nguyên hiển thị đúng thời gian mới.
6. Chuyển trạng thái từng bước từ Đã đặt đến Hoàn thành. Không được bỏ bước hoặc quay lùi. Hủy lịch bắt buộc có lý do và đúng quyền/trạng thái.

## 3. Tài nguyên và dịch vụ thuê — AT24–AT25

1. Mở Chi tiết lịch → tab **Tài nguyên** → phân công một tài nguyên với số lượng hợp lệ. Phân công vượt tồn khả dụng trong cùng khoảng: bị từ chối.
2. Chọn **Đánh dấu đã trả** hoặc **Hủy phân công**. Thao tác lần hai trên cùng dòng phải bị từ chối.
3. Tạo tài nguyên có `Dịch vụ thuê liên kết`; phân công tài nguyên đó. Hộp xác nhận dịch vụ thuê phải xuất hiện.
   - Chọn **Có**: tab Dịch vụ xuất hiện dòng dịch vụ thuê với số lượng bằng số lượng tài nguyên; tổng tiền cập nhật và nhật ký ghi nhận.
   - Chọn **Không**: chỉ có phân công tài nguyên, không thêm dòng dịch vụ.
4. Nếu dòng dịch vụ thuê đã tồn tại, hệ thống phải dừng và hướng dẫn quản lý dòng dịch vụ tại tab Dịch vụ, không tự cộng trùng.

## 4. Dịch vụ và tài chính — AT26–AT35

1. Thêm dịch vụ với số lượng dương; chi tiết hiển thị snapshot tên/đơn vị/đơn giá và tổng thanh toán tăng.
2. Xóa dịch vụ làm tổng thanh toán nhỏ hơn số đã thu: bị từ chối. Thử giảm giá lớn hơn tạm tính, hoặc giảm giá dương không có lý do: bị từ chối.
3. Thu tiền với `Đặt cọc`, `Thu bổ sung`, `Thanh toán còn lại`; số tiền bằng 0, âm, loại thu lạ hoặc vượt còn lại: bị từ chối.
4. Hủy lịch có khoản đã thu; chỉ Quản trị viên hoàn tiền được. Lý do và số tiền dương là bắt buộc, tổng hoàn không được vượt tổng đã thu.
5. Sau thu/hoàn, kiểm tra tab **Lịch sử tài chính**, tổng Đã thu/Còn lại và preview biên nhận. Nhật ký có `THU_TIEN` hoặc `HOAN_TIEN`.

## 5. Báo cáo, CSV và nhật ký — AT36–AT38

1. Vào **Báo cáo**, chọn một khoảng ngày hợp lệ. Kiểm tra các thẻ: doanh thu lịch Hoàn thành, tổng thu, hoàn tiền, thực thu, công nợ và số lịch/trạng thái.
2. Đổi ngày bắt đầu lớn hơn ngày kết thúc: bị từ chối. Đăng nhập Nhân viên: menu Báo cáo không xuất hiện.
3. Xuất **Excel Workbook** từ Khách hàng, Lịch chụp và Báo cáo. Mở trực tiếp bằng Excel: mỗi trường phải nằm ở cột riêng, tiếng Việt hiển thị đúng và font là Times New Roman. Lựa chọn CSV UTF-8 vẫn có cho trao đổi dữ liệu; CSV không mang thông tin font.
4. Trong **Nhật ký**, tìm `XOA_KHACH_HANG`, `NGUNG_SU_DUNG_DANH_MUC`, `THU_TIEN`, `HOAN_TIEN`, `PHAN_CONG_TAI_NGUYEN`. Chọn dòng và **Xem chi tiết**: kiểm tra được lý do, giá trị cũ và giá trị mới trong vùng cuộn.

## 6. Sao lưu và phục hồi — AT39–AT40

1. Chỉ Quản trị viên thấy menu Sao lưu. Hộp lưu sẽ mở tại thư mục Backup mặc định của SQL Server; chọn vị trí đó hoặc một thư mục mà **dịch vụ SQL Server** có quyền ghi. Với SQL Server Express, backup không dùng COMPRESSION. Sao lưu thành công và thêm một dòng lịch sử.
2. Chọn đường dẫn không tồn tại hoặc đuôi khác `.bak`: thao tác bị từ chối trước khi gọi SQL Server.
3. Chỉ trên database kiểm thử: tạo backup, thêm một khách hàng có tên đánh dấu, phục hồi backup qua xác nhận hai bước, ứng dụng khởi động lại. Khách đánh dấu phải biến mất sau restore.
4. Sau thất bại/success, trang Sao lưu hiển thị thời điểm, loại thao tác, trạng thái, tài khoản, tệp và thông báo.

## 7. UI/DPI — AT41

Kiểm tra Login, Dashboard, Lịch chụp, Chi tiết lịch, Báo cáo, Nhật ký, Sao lưu tại 100%, 125% và 150% DPI. Không được có text che/cắt, nút giả, tràn panel, hoặc thanh cuộn ngang trong sidebar. Thanh cuộn dọc sidebar được phép khi danh sách menu dài hơn chiều cao cửa sổ.

## Kết quả cần gửi lại

- Commit SHA đã test.
- Kết quả build/test.
- Pass/Fail cho từng phần 2–7, kèm ảnh hoặc mô tả lỗi ngắn nếu Fail.
