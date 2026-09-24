# Ma trận truy vết StudioManager 2.1 — Stage 0

`Evidence` hiện là audit tĩnh tại commit `fad0cf5`; không phải kết quả test. `Not verified` không được hiểu là Pass.

## FR01-FR11

| Requirement | Hiện trạng/điểm code | Test/gate | Evidence | Trạng thái |
|---|---|---|---|---|
| FR01 | `AuthService`, `LoginForm`, `AccountsPage`, `TaiKhoan` | AT01-AT06 | Login, PBKDF2, bắt đổi mật khẩu, guard Admin, khóa/mở khóa transactional và audit đã có; nghiệm thu Phase 1 đã được người dùng xác nhận | Pass — user acceptance |
| FR02 | `CrudPage`, `SaveSimpleAsync`, `DeactivateAsync` | AT07-AT09 | CRUD, xóa có điều kiện, lịch sử liên quan và audit lifecycle đã được nghiệm thu Phase 2 | Pass — user acceptance |
| FR03 | `CrudPage`, lookup `NhanVien` | AT10 | Trạng thái nhân viên và lọc lookup dữ liệu còn hoạt động đã được nghiệm thu Phase 2 | Pass — user acceptance |
| FR04 | `CrudPage`, `SimpleEntitySpec`, lookups | AT11 | Validation dữ liệu nền, liên kết tài nguyên–dịch vụ và audit lifecycle đã được nghiệm thu Phase 2 | Pass — user acceptance |
| FR05 | `BookingService`, `SqlStudioRepository`, `BookingsPage` | AT12-AT19, AT22-AT23 | Có core flow; TOP 500/query-ID/filter/resource-sync sai/chưa đủ | Partial / Not verified |
| FR06 | `BusinessRules`, `UpdateBookingStatusAsync` | AT20-AT21 | Chuyển một bước có mặt; UI/history/evidence thiếu | Partial / Not verified |
| FR07 | `FinanceService`, booking child dialogs, SQL finance methods | AT26-AT35 | Có một phần; mutation services/discount/summary/audit thiếu | Partial / Not verified |
| FR08 | `GetReportAsync`, `ReportsPage`, `GetDashboardAsync` | AT36 | Aggregate chưa tách đúng khỏi list limit/evidence thiếu | Partial / Not verified |
| FR09 | `AssignResourceAsync`, resource tab | AT24-AT25 | Chỉ assign hoàn chỉnh một phần; thiếu return/cancel/rental workflow | Partial / Not verified |
| FR10 | `AuditAsync`, `NhatKyHeThong` | AT38 | Coverage/view/filter chưa đủ | Partial / Not verified |
| FR11 | `BackupAsync`, `RestoreAsync`, `BackupPage` | AT39-AT40 | Có thao tác kỹ thuật; thiếu two-step/history/audit guard/evidence | Partial / Not verified |

## BR01-BR24

| BR | Hướng thực thi Stage tiếp theo | Gate/evidence cần có |
|---|---|---|
| BR01 | Service sinh/kiểm tra mã, DB UNIQUE | AT05, test unique |
| BR02-BR04 | Booking/category service + FK/active lookup | AT10-AT15 |
| BR05-BR07 | Time rule + transactional conflict recheck | AT12-AT16 |
| BR08-BR11 | Finance/booking-service invariants tập trung | AT26-AT30 |
| BR12-BR14 | Payment/refund service + serialized transaction | AT29-AT34 |
| BR15-BR18 | Role/status transition service | AT17-AT23 |
| BR19 | Soft deactivate/delete guard theo lịch sử | AT08-AT11 |
| BR20-BR21 | Resource capacity + confirmed rental workflow | AT24-AT25 |
| BR22-BR24 | Immutable financial/audit records, transaction + post-commit success | AT29-AT40 |

## AT01-AT41

| Gate | Acceptance tests | Hiện trạng evidence |
|---|---|---|
| Giai đoạn 2 | AT01-AT06 | Pass — user acceptance trên Windows/SQL Server, 23/09/2026; branch `feature/spec-v2.1-accounts-security`, commit `3b740092` |
| Giai đoạn 3 | AT07-AT11 | Pass — user acceptance trên Windows/SQL Server, 24/09/2026; branch `feature/spec-v2.1-master-data`, remote commit `e3d00fd` |
| Giai đoạn 4 | AT12-AT23 | Not verified |
| Giai đoạn 5 | AT24-AT25 | Not verified |
| Giai đoạn 6 | AT26-AT35 | Not verified |
| Giai đoạn 7 | AT36-AT40 | Not verified |
| Giai đoạn 8 | AT41 | Not verified |

## Evidence phải thu thập ở mỗi gate

- Commit SHA của source được build/test.
- Kết quả `dotnet restore`, `dotnet build`, `dotnet test` trên Windows.
- Script tạo database sạch và seed chạy thành công (với giai đoạn có SQL).
- Kết quả từng AT liên quan: Pass, Fail hoặc Not verified, kèm dữ liệu/ảnh/log tối thiểu.
- Smoke check UI DPI 100%, 125%, 150% cho Login, Main, Bookings, Booking Detail, Reports và Backup/Restore trước bàn giao.

## Tiến độ source — branch `feature/spec-v2.1-compliance`

- UI đã không còn gọi `AppFacade.Repository`; các màn hình dùng Application Service phù hợp.
- Tra cứu chi tiết lịch theo ID dùng query ID riêng, không lấy một danh sách giới hạn rồi tìm trong bộ nhớ.
- Báo cáo aggregate chuyển sang SQL và dùng các mốc ngày đặc tả; booking detail refresh summary sau mutation.
- Thêm workflow xóa dịch vụ có guard tổng phải thu, trả/hủy resource assignment, audit discount/account/password và log backup/restore.
- Tất cả mục trên vẫn là **Implemented / Not verified** cho tới khi build, chạy SQL và AT liên quan trên Windows ở commit được ghi nhận.

## Evidence đã nhận

- Windows/.NET 8: `dotnet clean`, `dotnet restore` và `dotnet build StudioManager.sln` thành công trên branch `feature/spec-v2.1-compliance`.
- Windows/.NET 8: `dotnet test StudioManager.sln` thành công: **10/10 tests passed**, 0 failed, 0 skipped (23/09/2026).
- SQL migration `05_UpgradeToSpec2_1.sql`: lần chạy đầu bị chặn trước khi tạo filtered index do session SQL Server tắt `QUOTED_IDENTIFIER`. Script đã được cập nhật để tự bật các SET options bắt buộc và lần chạy lại đã hoàn tất không lỗi trên `StudioManager` (23/09/2026).
- Phase 1 / FR01: người dùng đã xác nhận Pass các luồng login, đổi mật khẩu bắt buộc, tạo/reset tài khoản, khóa/mở khóa, guard self-action và audit sau khi kéo branch `feature/spec-v2.1-accounts-security` (23/09/2026). Không suy diễn kết quả này sang các gate AT07–AT41.
- Phase 2 / FR02–FR04: người dùng đã xác nhận Pass checklist dữ liệu nền trên Windows/SQL Server, gồm lifecycle danh mục, phân quyền menu Nhân viên, validation và tra cứu nhật ký sau khi chạy đúng branch `feature/spec-v2.1-master-data` (24/09/2026). Không suy diễn kết quả này sang các gate AT12–AT41.
