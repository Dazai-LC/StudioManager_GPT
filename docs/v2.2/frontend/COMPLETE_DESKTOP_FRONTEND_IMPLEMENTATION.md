# STUDIOMANAGER v2.2 — TÀI LIỆU HOÀN THIỆN FRONTEND DESKTOP
## ATELIER LUMIÈRE — COMPLETE C# WINFORMS IMPLEMENTATION SPECIFICATION

**Workspace:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Frontend`  
**Branch:** `feature/v2.2-frontend`  
**Stack:** C# 12 / .NET 8 / Windows Forms (`PerMonitorV2` DPI, Code-First GDI+)

---

## 1. ĐIỂM VÀO HỆ THỐNG & ĐĂNG NHẬP HAI CỔNG (AUTH SURFACE REDESIGN)

Ứng dụng khởi động tại [`Program.cs`](../../src/StudioManager.WinForms/Program.cs) mở [`LoginForm`](../../src/StudioManager.WinForms/Forms/LoginForm.cs) làm cổng vào duy nhất có thanh chuyển nhanh giữa hai không gian (`AccessMode.StaffAdminSql` và `AccessMode.CustomerDemo`):

| Thành phần Auth | Tệp tin | Nền & Hiệu ứng | Hành vi & Phạm vi |
|---|---|---|---|
| Cổng chung & Staff/Admin SQL | [`LoginForm.cs`](../../src/StudioManager.WinForms/Forms/LoginForm.cs) | `FullCanvasAuthSurface` (`bg_auth_desktop_login.jpg`) + `ElevatedFrostedAuthCard` | Xác thực SQL Server thực (`AppFacade.Auth.LoginAsync`), giữ nguyên kiểm tra `PhaiDoiMatKhau` qua [`ChangePasswordForm`](../../src/StudioManager.WinForms/Forms/ChangePasswordForm.cs). |
| Cổng Khách hàng (Demo) | [`CustomerLoginForm.cs`](../../src/StudioManager.WinForms/Forms/CustomerLoginForm.cs) | `FullCanvasAuthSurface` (`bg_auth_portal_register.jpg`) + `ElevatedFrostedAuthCard` | Có 2 nút nạp nhanh hồ sơ mẫu (`Nguyễn Thảo Vy` — đã xác minh, `Trần Minh Quân` — chưa liên kết) -> mở [`CustomerMainForm`](../../src/StudioManager.WinForms/Forms/CustomerMainForm.cs). |
| Đăng ký Tài khoản Khách | [`CustomerRegistrationForm.cs`](../../src/StudioManager.WinForms/Forms/CustomerRegistrationForm.cs) | `FullCanvasAuthSurface` (`bg_auth_portal_register.jpg`) + `ElevatedFrostedAuthCard` | Kiểm tra hợp lệ họ tên, SĐT Việt Nam, Email, độ mạnh mật khẩu; trả về `CreatedDemoSession` để mở ngay cổng Khách hàng. |
| Khôi phục & Xác minh sở hữu | [`CustomerRecoveryDialog.cs`](../../src/StudioManager.WinForms/Forms/CustomerRecoveryDialog.cs) | Dialog chuẩn Atelier Lumière | Giải thích quy trình xác minh sở hữu an toàn; không tự động cấp quyền xem lịch sử bằng thông tin công khai. |
| Chrome Auth dùng chung | [`AuthChrome.cs`](../../src/StudioManager.WinForms/Forms/AuthChrome.cs) & [`AuthVisualAssets.cs`](../../src/StudioManager.WinForms/AuthVisualAssets.cs) | `ModernInputContainer`, `AuthChromeFactory` | Dải header ribbon, thanh chuyển cổng Access Mode và các ô nhập vector đồng nhất độ cao & DPI. |

---

## 2. HỆ THỐNG BIỂU TƯỢNG VAI TRÒ THỐNG NHẤT (`RoleVisuals.cs`)

Triển khai đồng bộ tại [`RoleVisuals.cs`](../../src/StudioManager.WinForms/RoleVisuals.cs) qua `RoleVisualResolver`, `RoleBadgePill` và `AttachRoleIconComboBox`:

- **Quản trị viên (`QUAN_TRI_VIEN`):** Huy hiệu khiên + vương miện, sắc vàng hổ phách (`Ochre`).
- **Nhân viên Studio (`NHAN_VIEN`):** Thẻ định danh nhân sự, sắc xanh xám (`Slate/Teal`).
- **Nhiếp ảnh gia (`NHIEP_ANH_GIA`):** Ống kính máy ảnh / khẩu độ, sắc đất nung (`Terracotta`) — dùng trên bảng nhân sự, inspector lịch chụp, combo phân công ở Tab `2.2` và màn hình khách hàng.
- **Khách hàng (`KHACH_HANG`):** Hồ sơ người dùng, sắc xanh lá xô thơm (`Sage`) — dùng trên sidebar Cổng Khách hàng, `BookingsPage` và danh mục `KhachHang`.

---

## 3. KHÔNG GIAN KHÁCH HÀNG CÔ LẬP (EXPERIENCE A — `CustomerMainForm`)

[`CustomerMainForm`](../../src/StudioManager.WinForms/Forms/CustomerMainForm.cs) sử dụng header top bar `80px` phân cấp (breadcrumb + tiêu đề mục + CTA `"Đặt lịch ngay"` + nhãn `"CHẾ ĐỘ DEMO CỤC BỘ"`), thanh điều hướng trái chuyên biệt **9 mục điều hướng** trỏ tới **8 trang giao diện `CustomerPages\`** chạy trên kho mô phỏng [`DesktopDemoSessionStore`](../../src/StudioManager.WinForms/Simulation/DesktopDemoSessionStore.cs):

1. **`overview` → [`CustomerDashboardPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerDashboardPage.cs):** Lời chào cá nhân hóa, banner ảnh studio nổi bật, 4 thẻ KPI cân đối, danh sách gói nổi bật và thông báo gần nhất.
2. **`packages` → [`CustomerPackagesPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerPackagesPage.cs):** Trưng bày 05 gói chụp (`GC000001..05`) với ảnh bìa lớn `VisualMediaPictureBox` (nhấp để phóng to `ImageViewerForm`), bảng so sánh tính năng và nút chuyển thẳng vào Wizard kèm mã gói.
3. **`spaces` → [`CustomerSpacesResourcesPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerSpacesResourcesPage.cs):** Phân tách rõ 3 tab con: **Phòng chụp (`P0001..03`)**, **Thiết bị & Đạo cụ (6 mục)**, **Trang phục cho thuê (4 mục kèm `DV000002`)**.
4. **`wizard` & `requests` → [`CustomerBookingWizardPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerBookingWizardPage.cs):** Quy trình đặt lịch **5 bước** (Chọn gói → Ngày/giờ/phòng ưu tiên & số người → Dịch vụ bổ sung → Thông tin liên hệ & concept → Xem tóm tắt chi phí tạm tính & Gửi yêu cầu `REQ-202611-xxx`) + tab theo dõi & hủy yêu cầu đủ điều kiện.
5. **`bookings` → [`CustomerBookingsPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerBookingsPage.cs):** Danh sách lịch chính thức (`LCDEMO-202611-001`, `LCDEMO-202610-018`), tiến trình xử lý, nhiếp ảnh gia phụ trách (kèm pill vai trò), ảnh phòng chụp. Bị chặn an toàn khi tài khoản chưa xác minh quyền sở hữu (`Unlinked`, `PendingVerification`, `RejectedVerification`).
6. **`payments` → [`CustomerPaymentsPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerPaymentsPage.cs):** Hiển thị [`VietQrCardControl`](../../src/StudioManager.WinForms/Controls/VietQrCardControl.cs) với cú pháp sao chép `PHOENIX [Mã]`, thông báo rõ không tự động chốt lịch khi quét QR, form khai báo đã chuyển khoản kèm đính kèm file ảnh chứng từ và nút xem biên nhận đa khổ [`ExtendedReceiptPreviewForm`](../../src/StudioManager.WinForms/Controls/ExtendedReceiptPreviewForm.cs).
7. **`albums` → [`CustomerAlbumsPage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerAlbumsPage.cs):** Trình diễn album bàn giao (`ALB-202610-018`) cho tài khoản đã xác minh kèm `ImageViewerForm` thu phóng; hiển thị thẻ bảo vệ quyền riêng tư khi chưa liên kết.
8. **`profile` → [`CustomerProfilePage.cs`](../../src/StudioManager.WinForms/CustomerPages/CustomerProfilePage.cs):** Cập nhật hồ sơ Demo và bộ mô phỏng đầy đủ **5 trạng thái liên kết quyền sở hữu** (`Unlinked`, `PendingVerification`, `VerifiedOwnership`, `RejectedVerification`, `AuthorizedManualLink`) phản ánh tức thì trên toàn bộ các tab Lịch chụp & Album.

---

## 4. KHÔNG GIAN NHÂN VIÊN & QUẢN TRỊ VIÊN (EXPERIENCE B — `MainForm`)

[`MainForm.cs`](../../src/StudioManager.WinForms/Forms/MainForm.cs) bảo toàn 5 Hub (`dashboard`, `bookings`, `catalog`, `reports`, `system`) với các điểm nâng cấp đồng bộ v2.2:

- **Tab `2.1` ([`BookingsPage.cs`](../../src/StudioManager.WinForms/Pages/BookingsPage.cs)):** Giữ nguyên toàn bộ nghiệp vụ SQL v2.1 (`CreateAsync`, `EditAsync`, `AddPaymentAsync`, `AddRefundAsync`, `PrintReceiptAsync`), bổ sung ảnh xem trước gói chụp (`VisualMediaPictureBox`) ở inspector và nút **"Xem đa khổ (A4 / K80 / K58)"** trong `ReceiptPreviewForm`.
- **Tab `2.2` ([`BookingRequestsPlaceholderPage.cs`](../../src/StudioManager.WinForms/Pages/BookingRequestsPlaceholderPage.cs)):** Workspace điều phối yêu cầu trực tuyến đọc dữ liệu trực tiếp từ `DesktopDemoSessionStore`, 4 KPI cards dàn đều, 5 tab lọc trạng thái, chọn phòng & nhiếp ảnh gia hoạt động (`NV000004..07` kèm icon máy ảnh Terracotta), kiểm tra xung đột lịch đọc-chỉ-xem, phản hồi khách, ghi chú nội bộ và hộp thoại **Xem trước chuyển đổi thành Lịch chụp**.
- **Tab `2.3` ([`PaymentWorkspacePlaceholderPage.cs`](../../src/StudioManager.WinForms/Pages/PaymentWorkspacePlaceholderPage.cs)):** Trung tâm đối soát VietQR & Tiền mặt đồng bộ từ `DesktopDemoSessionStore`, xem ảnh chứng từ qua `ImageViewerForm`, nhập số tiền thực nhận & ghi chú đối soát, các nút hành động **Khớp thủ công VietQR**, **Đánh dấu sai lệch**, **Thu tiền mặt**, **Từ chối** và mở [`ExtendedReceiptPreviewForm`](../../src/StudioManager.WinForms/Controls/ExtendedReceiptPreviewForm.cs) (`A4Invoice`, `ThermalK80`, `ThermalK58`, `PdfLayoutPreview`).
- **Danh mục (`GoiChup`, `PhongChup`, `TaiNguyen` trong [`CrudPage.cs`](../../src/StudioManager.WinForms/Pages/CrudPage.cs)):** Inspector hiển thị `VisualMediaPictureBox` tương ứng từng mã (`GC...`, `P...`, `TN...`) + nút mở [`AdminMediaManagerDialog`](../../src/StudioManager.WinForms/Controls/PhotoMediaControls.cs) (Quản trị viên được tải ảnh từ máy & chỉnh thứ tự/bìa trong Demo; Nhân viên chỉ xem).

