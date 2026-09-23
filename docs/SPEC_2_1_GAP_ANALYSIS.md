# Audit gap so với đặc tả StudioManager 2.1

## Phạm vi audit

- Nguồn đặc tả: `Bao_cao_dac_ta_StudioManager_v2.1_AgentReady(1).docx`, phiên bản 2.1 ngày 23/09/2026.
- Source of truth: FR01-FR11, BR01-BR24, AT01-AT41 và Phụ lục A-C của đặc tả.
- Baseline audit: commit `fad0cf5` (`fix/login-form-clarity`). Các branch UI/experiment khác không được đưa vào audit này.
- Kết quả này chỉ là đọc source tĩnh. Không có mục nào được đánh dấu Pass nếu chưa có build/test SQL/Windows trên commit của branch này.

## Hiện trạng kiến trúc

Luồng hiện có là `WinForms -> AppFacade -> IStudioRepository -> SqlStudioRepository -> SQL Server`. Tuy nhiên `AppFacade` công khai `Repository`; nhiều màn hình gọi repository trực tiếp (`AccountsPage`, `CrudPage`, `BookingsPage`, `ReportsPage`, `BackupPage`). Điều này vi phạm ranh giới bắt buộc tại Phụ lục A.2: UI phải gọi Application Service, còn Service chịu trách nhiệm quyền, quy tắc và điều phối transaction.

## Gap theo nhóm chức năng

| Nhóm | Hiện có | Gap cần xử lý trước khi Done |
|---|---|---|
| FR01 Tài khoản | PBKDF2, login, đổi mật khẩu bắt buộc, tạo/reset/khóa ở mức cơ bản | Chưa có AccountService; create/reset/lock không audit đầy đủ, UI gọi repository trực tiếp, thiếu revalidate session cho thao tác nhạy cảm. |
| FR02 Khách hàng | CRUD đơn giản và xóa có điều kiện không có lịch | Chưa có lịch sử khách hàng/entry point rõ ràng; xóa chưa có audit/transaction theo yêu cầu. |
| FR03-FR04 Nhân viên/danh mục | Danh mục, trạng thái và lookup cơ bản | Generic CRUD cho phép logic quá rộng; validation nghiệp vụ, quyền Service và audit ngừng dùng chưa đầy đủ. |
| FR05 Lịch chụp | Create/update/cancel, snapshot giá gói, conflict half-open, transaction ở repository | `GetBookingAsync` lấy từ `TOP(500)` thay vì truy vấn ID; filter FR05.5 chưa đủ; cập nhật chưa đồng bộ resource assignment; guard/lookup hoạt động chưa được kiểm tra lại đủ trong transaction. |
| FR06 Tiến độ | Chỉ tiến một bước và chặn trạng thái kết thúc | UI không có màn hình tiến độ/lịch sử tách riêng; quyền/session ở Service chưa hoàn chỉnh. |
| FR07 Dịch vụ/tài chính | Thêm dịch vụ, giảm giá, thu/hoàn và preview biên nhận cơ bản | Thiếu sửa/xóa dịch vụ an toàn, refresh summary chuẩn, audit giảm giá, transaction cho giảm giá, đảm bảo bất biến cho mọi mutation dịch vụ. |
| FR08 Báo cáo | Dashboard và report tổng hợp cơ bản | Có phụ thuộc danh sách giới hạn và các cách tính chưa được xác minh hoàn toàn theo mốc `HoanThanhLuc` / `NgayGiaoDich`; thiếu báo cáo trạng thái và bộ lọc đầy đủ. |
| FR09 Tài nguyên | Gán resource với kiểm tra capacity overlap | Thiếu workflow Trả/Hủy phân công, actor/history, prompt xác nhận dịch vụ thuê, và đồng bộ khi reschedule. |
| FR10 Nhật ký | Có `NhatKyHeThong` và audit cho một số lịch/thu/hoàn | Thiếu coverage account, reset/lock, discount, danh mục, backup/restore; viewer chưa có before/after/reason và filter đầy đủ. |
| FR11 Backup/restore | Có backup và restore kỹ thuật | Backup chưa guard quyền ở Service/repository đầy đủ và chưa ghi `NhatKySaoLuu`; restore thiếu UI xác nhận hai bước, audit success/fail, lịch sử và test DB thử nghiệm. |

## Gap đặc biệt phải loại bỏ

1. `SearchBookingsAsync` dùng `SELECT TOP(500)` và `GetBookingAsync` tìm ID trong danh sách đó. Đây là anti-pattern “silent TOP N”; phải thay bằng truy vấn ID chuyên biệt và phân trang/giới hạn minh bạch cho list.
2. `DashboardPage` và report suy ra dữ liệu từ danh sách booking giới hạn; aggregate phải dùng query tổng hợp đúng mốc thời gian trong SQL/repository.
3. `SetDiscountAsync`, account operations, deactivate và backup/restore có coverage audit/transaction chưa đáp ứng đầy đủ BR10, BR22-BR24 và FR10-FR11.
4. Repository hiện trộn nhiều domain bằng generic string `entity/type`; cần tách contract/service theo nghiệp vụ trước khi mở rộng để tránh UI né kiểm tra quyền.
5. Test hiện có chỉ là một số unit test cho `BusinessRules`; chưa có evidence cho AT01-AT41.

## Kết luận Stage 0

Hiện trạng **không đủ điều kiện Done** cho bất kỳ gate tổng thể nào của đặc tả 2.1. Các thay đổi tiếp theo phải thực hiện theo giai đoạn 1-8, cập nhật ma trận truy vết và giữ trạng thái `Not verified` khi chưa có evidence chạy thực tế.
