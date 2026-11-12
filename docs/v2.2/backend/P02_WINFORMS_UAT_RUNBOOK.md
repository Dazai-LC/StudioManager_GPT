# P02 — WinForms regression runbook

Ngày: 12/11/2026. Tất cả scenario SQL-backed bên dưới: **BLOCKED — CHƯA CHẠY**. Source và unit tests không thay kết quả tương tác WinForms.

Target revision: dùng instance hiện có `localhost\SQLEXPRESS`, observed `@@SERVERNAME = MSI\SQLEXPRESS`, MachineName `MSI`, InstanceName `SQLEXPRESS`; bỏ đề xuất cài PHOENIXP02. Database duy nhất `StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e`, Run ID `7d70a08ac15349acbd207f82c8349f6e`. Metadata SQL Server 2022 Express/no user DB do Owner cung cấp; chưa được runbook kiểm chứng runtime. Default `MSSQLSERVER`, `StudioManager` và mọi target khác đều bị cấm.

## Điều kiện trước khi chạy

Owner duyệt instance/database/permissions trong `P02_ISOLATED_SQL_APPROVAL_PLAN.md`; metadata và SQL suite đạt; có bộ synthetic ADMIN/STAFF/forced/locked accounts và các danh mục/khách/lịch UAT. Fixture SQL suite yêu cầu DB rỗng nên phải hoàn tất/cleanup suite trước khi chuẩn bị UAT. Seed UAT là DML cần approval, không dùng seed studio hoặc dữ liệu thật.

Tạo output copy WinForms riêng bằng build/publish có phiên bản và manifest; chỉ thay `appsettings.json` trong output copy bằng test target đã verify. Kiểm tra nội dung target có thể công bố (server/database, không credential); không launch root app/default production config. Chạy bằng Windows principal test quyền giới hạn. Ghi SHA/branch, target metadata và Windows identity test trong biên bản nội bộ, không ghi password/hash. Không chụp credential vào ảnh.

Identity đề xuất: `MSI\PhoenixP02Runner` thuộc `MSI\PhoenixP02Tests`, chỉ có quyền database test đã review. Identity sysadmin hiện tại chỉ provisioning, không chạy UI. Trước launch phải chạy metadata Fact ở READ_ONLY dưới token runner thật theo approval plan: sysadmin/db_owner=0, không server-wide write/DDL/backup, đủ object DML và không access user DB khác; NULL/mismatch thì STOP. WinForms không tự gọi SQL test harness: operator phải kiểm tra appsettings của output copy khớp chính xác server/database đã qua gate. Không nới quyền để vượt lỗi SQL.

Trình tự: 11 SQL facts pass và fixture cleanup → Owner duyệt seed UAT synthetic → review output copy/config → launch bằng runner → ghi checklist. Không seed UAT trước suite vì fixture yêu cầu 14 bảng rỗng. Cờ SQL test chỉ đặt trong shell test, xóa trong finally; không dùng để cấu hình UI. Lệnh account/login/grants và từng execution approval nằm trong `P02_ISOLATED_SQL_APPROVAL_PLAN.md`.

## Checklist tương tác và kết quả mong đợi

| ID | Thao tác trên UI | Backend/source liên quan | Kết quả phải ghi nhận |
|---|---|---|---|
| AUTH-01 | Login ADMIN rồi mở MainForm | LoginForm → Auth.LoginAsync → init/dashboard | Role do account SQL quyết định; không writable Session |
| AUTH-02 | Logout rồi login STAFF | Auth.SignOut và registry | Phiên cũ không dùng lại; menu và quyền STAFF đúng |
| AUTH-03 | Sai mật khẩu, account khóa | AuthService và SQL account projection | Không vào shell, không khôi phục session cũ |
| AUTH-04 | Login account bắt buộc đổi, hủy form | Restricted session và forced API | Không mở MainForm; phiên bị hủy |
| AUTH-05 | Hoàn tất đổi bắt buộc | Password CAS + rotation | Đổi thành công, mở shell bằng phiên mới; password cũ không đăng nhập được |
| AUTH-06 | Gây init failure bằng test double/fault trong output test đã duyệt | Initialization catch → SignOut | Thông báo an toàn; nút trở lại login; session không còn active; không dừng/restart SQL services hoặc sửa instance config |
| PWD-01 | Ordinary change: sai current password | ChangePasswordForm → ordinary API | Bị từ chối, hash test không đổi; không có target ID client |
| PWD-02 | New/confirm không khớp, rồi khớp | Validation, CAS, session rotation | Sai không ghi; đúng đổi self, các account khác không đổi |
| PWD-03 | ADMIN reset account STAFF, login STAFF | Administration reset riêng | Forced-change; phiên STAFF cũ bị từ chối ở command sau |
| DATA-01 | CRUD khách synthetic, tìm/history; xóa hồ sơ có/không lịch | Administration + repository | Quyền/trạng thái/history đúng; không xóa hồ sơ có lịch |
| DATA-02 | ADMIN tạo/sửa/ngừng nhân viên/account/gói/dịch vụ | Guards và CRUD | STAFF bị chặn chức năng admin; current-role checks đúng |
| BOOK-01 | Tạo lịch, phòng/thợ hợp lệ; thử xung đột | BookingService + transaction allocation | Giáp ranh được, giao nhau bị chặn; thợ nghỉ/phòng ngừng không nhận lịch mới |
| BOOK-02 | Tạo lịch giá A; đổi catalog thành B; chỉ sửa giờ/phòng/thợ | Snapshot policy và UpdateBookingAsync | TenGoiChot/GiaGoiChot vẫn A; tổng/công nợ/receipt không vô tình theo B |
| BOOK-03 | Đổi gói; hủy xác nhận, thiếu lý do, nhận quote rồi đổi catalog trước lưu | Explicit quote intent và locked read | Hủy/thiếu/stale không ghi; valid quote đổi giá có audit riêng |
| BOOK-04 | Dịch vụ/số lượng/giảm giá, phân công/trả/hủy tài nguyên | BookingSupport + transactions | Không vượt capacity/tổng thu; attribution/audit đúng |
| MONEY-01 | Thu nhiều lần; thử vượt công nợ | Finance + serializable SQL | Tổng không vượt giới hạn; audit và ledger khớp |
| MONEY-02 | Hủy lịch, hoàn tiền ADMIN; thử STAFF | Refund guard/state/limit | STAFF bị chặn; hoàn không vượt đã thu trừ đã hoàn |
| REPORT-01 | Mở dashboard/báo cáo sau các thao tác | Dashboard/Reporting | Đối chiếu số liệu với ledger synthetic; STAFF không xem báo cáo admin |
| UI-01 | Login, forced/ordinary password, booking quote, error dialog ở 1040×660 và 1366×768, DPI 100/150% | WinForms layout/async state | Không cắt nút/input, Tab/Enter/Esc đúng, nút không cho double submit; thông báo không lộ raw error/path |
| RESTORE-01 | Backup rồi restore vào DB rehearsal khác | Backup/Restore flow | Chỉ sau approval admin/storage riêng; kiểm dữ liệu/phiên/permissions sau restore, không restore đè studio |

Mỗi dòng lưu thời điểm, bước thực tế, expected/actual, actor synthetic, target, query đối chiếu đã được duyệt và PASS/FAIL/BLOCKED. Ảnh hỗ trợ UI chỉ là evidence bổ sung; `DrawToBitmap` không thay tương tác và không chứng minh UAT.

## Giới hạn và dừng

Không có quyền runner backup/restore là chủ đích; phản hồi SQL từ chối phải an toàn. Không cấp sysadmin cho runner để bypass harness. Rehearsal admin dùng môi trường/destination riêng và không chạy qua mutation harness. Nếu nghiệp vụ/khóa SQL sai, giữ evidence và phân tích trước sửa; không chuyển sang database studio.

SQLEXPRESS dùng chung host/tài nguyên với SQL studio; không stop/restart service để mô phỏng lỗi, không stress/load test. Nếu schema/identity/permissions/config đổi, dừng và quay lại metadata review. UAT/backup/restore chưa nằm trong approval cho việc chuẩn bị kế hoạch này.

P03/service/customer persistence không thuộc runbook. Phương án Windows Service tương lai không thay trạng thái BLOCKED của authority production hiện tại.
