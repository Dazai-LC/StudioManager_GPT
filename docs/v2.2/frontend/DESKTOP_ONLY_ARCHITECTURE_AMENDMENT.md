# STUDIOMANAGER v2.2 — ARCHITECTURE AMENDMENT: DESKTOP-ONLY PIVOT
## C# / WINDOWS FORMS (.NET 8) UNIFIED DESKTOP APPLICATION (ATELIER LUMIÈRE)

**Mã tài liệu:** `DEC-01-DESKTOP-ONLY` (Thay thế `DEC-01` và `DEC-01-R2`)  
**Ngày ban hành:** 11/11/2026  
**Workspace:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Frontend`  
**Git Branch:** `feature/v2.2-frontend`  
**Baseline Commit:** `ea7f7d8`  
**Ngôn ngữ & Stack duy nhất:** C# 12 / Windows Forms / .NET 8 (`PerMonitorV2` DPI, Code-First GDI+)  
**Trạng thái:** CHÍNH THỨC ÁP DỤNG CHO TOÀN BỘ FRONTEND v2.2 & GIAI ĐOẠN BACKEND HANDOFF

---

## 1. QUYẾT ĐỊNH KIẾN TRÚC CỐT LÕI (AUTHORITATIVE PIVOT)

Chủ sở hữu dự án đã quyết định **chấm dứt toàn bộ hướng phát triển Web Customer Portal** (bao gồm cả nguyên mẫu Next.js trước đây và bản Blazor C# dở dang) để thống nhất đồ án tốt nghiệp StudioManager v2.2 thành **một ứng dụng Desktop Windows Forms (C# / .NET 8) hoàn chỉnh duy nhất** gồm hai trải nghiệm tách biệt:

1. **Experience A — Không gian Khách hàng (Customer Desktop Experience — Frontend Demo):**
   - Khởi chạy từ thanh chọn cổng trên màn hình đăng nhập (`AccessMode.CustomerDemo`) thông qua [`CustomerLoginForm`](../../src/StudioManager.WinForms/Forms/CustomerLoginForm.cs), [`CustomerRegistrationForm`](../../src/StudioManager.WinForms/Forms/CustomerRegistrationForm.cs), [`CustomerRecoveryDialog`](../../src/StudioManager.WinForms/Forms/CustomerRecoveryDialog.cs) và vỏ ứng dụng riêng [`CustomerMainForm`](../../src/StudioManager.WinForms/Forms/CustomerMainForm.cs).
   - Toàn bộ dữ liệu vận hành trong trạng thái **Frontend Simulation / Local Demo** trên [`DesktopDemoSessionStore`](../../src/StudioManager.WinForms/Simulation/DesktopDemoSessionStore.cs) kết hợp danh mục chuẩn seed từ [`SeedGroundedCatalogRepository`](../../src/StudioManager.WinForms/Simulation/SeedGroundedCatalogRepository.cs). **Không ghi xuống SQL Server.**
   - Bảo toàn ranh giới cách ly danh tính (5 trạng thái `CustomerIdentityLinkState`): khách mới đăng ký mặc định ở trạng thái `Unlinked` hoặc `PendingVerification`, tuyệt đối không tự mở khóa lịch sử hay album khách khác (`KH000001..30`) chỉ bằng SĐT hoặc Email.

2. **Experience B — Không gian Điều hành Nội bộ (Staff & Admin Workspace):**
   - Khởi chạy từ [`LoginForm`](../../src/StudioManager.WinForms/Forms/LoginForm.cs) (`AccessMode.StaffAdminSql`) xác thực qua `AppFacade` + SQL Server hiện hữu (`TaiKhoan`, `NhanVien`) để vào [`MainForm`](../../src/StudioManager.WinForms/Forms/MainForm.cs).
   - Bảo toàn 100% 5 Hub chính (`dashboard`, `bookings`, `catalog`, `reports`, `system`) và toàn bộ luồng nghiệp vụ v2.1 (`BookingsPage`, `CrudPage`, `ReportsPage`, `AccountsPage`, `BackupPage`).
   - Tab `2.2` ([`BookingRequestsPlaceholderPage`](../../src/StudioManager.WinForms/Pages/BookingRequestsPlaceholderPage.cs)) và Tab `2.3` ([`PaymentWorkspacePlaceholderPage`](../../src/StudioManager.WinForms/Pages/PaymentWorkspacePlaceholderPage.cs)) đọc-ghi trực tiếp trên `DesktopDemoSessionStore` trong cùng phiên chạy Desktop để kiểm thử luồng khép kín giữa Khách hàng ↔ Lễ tân/Admin mà không làm thay đổi bảng thật `LichChup` hay `ThanhToan`.

---

## 2. HIỆN TRẠNG CÁC TÀI SẢN CŨ ĐƯỢC BẢO TỒN (PRESERVED ARTIFACTS)

Theo chỉ đạo nghiêm ngặt về bảo toàn lịch sử kỹ thuật, quá trình chuyển hướng Desktop-Only **KHÔNG xóa, ghi đè hay tái sử dụng** mã nguồn Web cũ làm mã chạy:

| Thành phần cũ | Vị trí | Trạng thái bảo tồn |
|---|---|---|
| Nguyên mẫu Web Next.js | Git branch `archive/v2.2-phase02-nextjs` | Bảo tồn nguyên vẹn trong lịch sử Git |
| Dự án Blazor dở dang | `src/StudioManager.CustomerPortal/` (untracked) | Bảo tồn nguyên trạng, không tham chiếu vào `StudioManager.sln` build chạy |
| Backend SQL / Domain / Application / Infrastructure | `src/StudioManager.Domain`, `Application`, `Infrastructure`, `database/*.sql` | Strictly Read-Only trong suốt giai đoạn Frontend |

---

## 3. RÀNG BUỘC THANH TOÁN & BẢO MẬT GIAO DIỆN

- **Chính sách thanh toán duy nhất:** VietQR tĩnh/mô phỏng với cú pháp bắt buộc `PHOENIX [Mã]`, chuyển khoản thủ công, nhân viên đối soát sao kê thủ công và thu tiền mặt tại quầy. **Tuyệt đối không tích hợp bất kỳ SDK, webhook hay cổng thanh toán tự động nào bên thứ ba.**
- **Chuẩn hiển thị khách hàng (UX Copy Hygiene):** Toàn bộ giao diện khách hàng (`CustomerPages/`, `Customer*Form`) không để lộ mã yêu cầu kỹ thuật nội bộ như `DEC-03`, `CTR-xx` hay `BG-AUTH-xx` trên văn bản giao diện; mã kỹ thuật chỉ tồn tại trong kiến trúc code, `Contracts/` và tài liệu bàn giao Backend.

