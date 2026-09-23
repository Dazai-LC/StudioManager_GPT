# Ma trận truy vết StudioManager 2.1 — Stage 0

`Evidence` hiện là audit tĩnh tại commit `fad0cf5`; không phải kết quả test. `Not verified` không được hiểu là Pass.

## FR01-FR11

| Requirement | Hiện trạng/điểm code | Test/gate | Evidence | Trạng thái |
|---|---|---|---|---|
| FR01 | `AuthService`, `LoginForm`, `AccountsPage`, `TaiKhoan` | AT01-AT06 | Có khung, còn direct repository/audit/quyền Service thiếu | Partial / Not verified |
| FR02 | `CrudPage`, `SaveSimpleAsync`, `DeactivateAsync` | AT07-AT09 | Có CRUD/xóa có điều kiện; thiếu history/audit chuẩn | Partial / Not verified |
| FR03 | `CrudPage`, lookup `NhanVien` | AT10 | Có trạng thái; thiếu service guard đầy đủ | Partial / Not verified |
| FR04 | `CrudPage`, `SimpleEntitySpec`, lookups | AT11 | Có schema/CRUD; validation & audit chưa đầy đủ | Partial / Not verified |
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
| Giai đoạn 2 | AT01-AT06 | Not verified |
| Giai đoạn 3 | AT07-AT11 | Not verified |
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
