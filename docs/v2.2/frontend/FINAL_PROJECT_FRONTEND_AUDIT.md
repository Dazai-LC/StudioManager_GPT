# STUDIOMANAGER v2.2 — BÁO CÁO KIỂM TOÁN FRONTEND TOÀN DỰ ÁN (FINAL AUDIT)
## ATELIER LUMIÈRE — DESKTOP WINFORMS ONLY

**Ngày kiểm toán & đóng băng frontend:** 12/11/2026  
**Workspace:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Frontend`  
**Branch:** `feature/v2.2-frontend` (Baseline `ea7f7d8`)  
**Phạm vi kiểm toán:** Toàn bộ 100% lớp giao diện WinForms (`Forms\`, `Pages\`, `CustomerPages\`, `Controls\`, `Simulation\`, `Contracts\`, `Assets\`), kiểm tra đọc-chỉ-xem SQL Server đang chạy (`localhost\StudioManager`), và đối chiếu 14 hợp đồng DTO bàn giao cho Backend Codex.

---

## A. TOÀN BỘ MÀN HÌNH & HỘP THOẠI ĐƯỢC KIỂM TOÁN (DESKTOP WINFORMS INVENTORY)

### 1. Luồng Khởi tạo & Xác thực (`Forms\`)
1. `ApplicationInitializationForm.cs` — Màn hình splash khởi tạo kết nối hệ thống.
2. `LoginForm.cs` — Cổng đăng nhập hợp nhất (Staff/Admin SQL thực + khung Xem trước Cổng Khách hàng Demo) với `FullCanvasAuthSurface` (`bg_auth_desktop_login.jpg`).
3. `CustomerLoginForm.cs` — Đăng nhập Khách hàng Demo + 2 nút nạp nhanh hồ sơ cô lập (`KH_DEMO_001 Nguyễn Thảo Vy`, `KH_DEMO_002 Trần Minh Quân`).
4. `CustomerRegistrationForm.cs` — Đăng ký tài khoản Khách hàng mới trong phiên Demo (`bg_auth_portal_register.jpg`).
5. `ChangePasswordForm.cs` — Đổi mật khẩu bắt buộc (`PhaiDoiMatKhau`) hoặc tự đổi cho nhân sự SQL Server.
6. `CustomerRecoveryDialog.cs` — Hướng dẫn & tiếp nhận đối chiếu hồ sơ cũ an toàn (trạng thái `PendingVerification`).

### 2. Không gian Khách hàng Cô lập (`CustomerMainForm` & `CustomerPages\`)
7. `CustomerMainForm.cs` — Shell cô lập với sidebar 9 mục và **Top Header Bar 80px** (breadcrumb 2 dòng + tiêu đề phân cấp + CTA `"Đặt lịch ngay"` + nhãn `"CHẾ ĐỘ DEMO CỤC BỘ"`).
8. `CustomerDashboardPage.cs` — Tổng quan khách hàng, banner concept nổi bật, 4 KPI co giãn, gói tiêu biểu & thông báo lọc riêng theo `DemoCustomerId`.
9. `CustomerPackagesPage.cs` — 05 gói chụp (`GC000001..05`), thẻ chọn, panel chi tiết ảnh lớn + bảng so sánh `DataGridView`.
10. `CustomerSpacesResourcesPage.cs` — 3 tab con: **Phòng chụp studio (`P0001..03`)**, **Thiết bị & Đạo cụ (`6+`)**, **Trang phục cho thuê (`4`)** kèm đọc SQL chỉ-xem (`TryLoadLiveRoomsReadOnlyAsync`, `TryLoadLiveResourcesReadOnlyAsync`).
11. `CustomerBookingWizardPage.cs` — Wizard đặt lịch **5 bước** (Gói chụp → Ngày/giờ/phòng → Dịch vụ bổ sung → Liên hệ → Rà soát & Gửi) + Tab **Lịch sử yêu cầu đã gửi**.
12. `CustomerBookingsPage.cs` — Danh sách lịch chụp chính thức, thanh tiến trình 6 giai đoạn & bảo vệ quyền riêng tư khi tài khoản chưa liên kết.
13. `CustomerPaymentsPage.cs` — Thanh toán `VietQrCardControl` minh họa (không quét được tiền thật), form khai báo chứng từ & xem trước biên nhận.
14. `CustomerAlbumsPage.cs` — Album ảnh bàn giao minh họa Demo cho tài khoản đã xác minh & màn hình khóa an toàn khi chưa xác minh.
15. `CustomerProfilePage.cs` — Cập nhật hồ sơ Demo, gửi đối chiếu danh tính & ngăn kéo QA mô phỏng 5 trạng thái (`_qaToggleBtn` mặc định thu gọn).

### 3. Không gian Nhân viên & Quản trị viên (`MainForm` & `Pages\`)
16. `MainForm.cs` — Shell 5 Hub (`dashboard`, `bookings`, `catalog`, `reports`, `system`), `TopCommandBarPanel`, `WorkspaceHostPanel`, `QuickCommandPaletteDialog`.
17. `DashboardPage.cs` — Bàn điều khiển KPI doanh thu 6 tháng, biểu đồ phễu trạng thái lịch, `Call Sheet Ca chụp` & `Nhật ký Vận hành`.
18. `BookingsPage.cs` — Tab `2.1` Lịch chụp SQL Server v2.1 (tạo/sửa/tiến độ/hủy/thanh toán/hoàn tiền/giảm giá/tài nguyên/biên nhận + ảnh xem trước gói).
19. `BookingRequestsPlaceholderPage.cs` — Tab `2.2` Hộp thư Điều phối Yêu cầu đặt lịch trực tuyến (4 KPI, 5 bộ lọc, kiểm tra trùng lịch đọc-chỉ-xem, phân bổ phòng & nhiếp ảnh gia `NV000004..07`, đồng bộ `MockBookings`).
20. `PaymentWorkspacePlaceholderPage.cs` — Tab `2.3` Trung tâm Đối soát VietQR & Tiền mặt thủ công (xem chứng từ qua `ImageViewerForm`, khớp tiền / sai lệch / từ chối / thu tiền mặt).
21. `CrudPage.cs` — Danh mục `KhachHang`, `NhanVien`, `GoiChup`, `DichVu`, `PhongChup`, `TaiNguyen`, `NhatKy` cùng các dialog `SimpleEditDialog`, `BookingHistoryDialog`, `AuditDetailDialog`, `VisualMediaPictureBox` & `AdminMediaManagerDialog`.
22. `ReportsPage.cs` — Báo cáo doanh thu & hiệu suất vận hành.
23. `AccountsPage.cs` — Quản lý tài khoản nội bộ, đặt lại mật khẩu, khóa/mở khóa và Role Badges.
24. `BackupPage.cs` — Kho sao lưu & phục hồi SQL Server (`NhatKySaoLuu`).

### 4. Điều khiển & Hộp thoại dùng chung (`Controls\`)
25. `PhotoMediaControls.cs` (`VisualMediaPictureBox`, `AdminMediaManagerDialog`)
26. `ImageViewerForm.cs` (Lightbox thu phóng + điều hướng gallery + dải thu nhỏ)
27. `ExtendedReceiptPreviewForm.cs` (Biên nhận đa khổ `A4Invoice`, `ThermalK80`, `ThermalK58`, `PdfLayoutPreview`)
28. `VietQrCardControl.cs` (`DrawNonScannableDemoPlate`, nhãn `QR MINH HỌA UI / KHÔNG QUÉT ĐỂ CHUYỂN KHOẢN`)
29. `AtelierNotifications.cs` (`AtelierDialog`, `AtelierToast`, `AtelierInlineAlert`, `StudioBanner`)
30. `RoleVisuals.cs`, `AuthVisualAssets.cs`, `Theme.cs`, `Ui.cs`, `Charts.cs`

---

## B. KIỂM TOÁN BAO PHỦ HÌNH ẢNH (SEED VS LIVE SQL SERVER — MINH BẠCH TUYỆT ĐỐI)

> Kết quả truy vấn đọc-chỉ-xem trực tiếp cơ sở dữ liệu **`StudioManager` (`localhost`)** đối chiếu với **`database/03_SeedData.sql`**:

### 1. Tập dữ liệu Seed chuẩn (`03_SeedData.sql`)
- **Gói chụp (`GC000001..GC000005`):** **5 / 5** có ảnh AI riêng (`100%`).
- **Phòng chụp (`P0001..P0003`):** **3 / 3** có ảnh AI riêng (`100%`).
- **Tài nguyên (`TN000001..TN000010`):** **10 / 10** có ảnh AI riêng (`100%`).
- **Tổng Seed danh mục có ảnh:** **18 / 18 = 100% ảnh AI (`LocalAiDefault`)**.
- **Nền Xác thực (`bg_auth_desktop_login.jpg`, `bg_auth_portal_register.jpg`):** **2 / 2 = 100%**.

### 2. Bản ghi bổ sung phát hiện trên SQL Server thực tế (`localhost\StudioManager`)
Trong quá trình kiểm toán DB thực tế (chế độ đọc-chỉ-xem), ngoài dữ liệu Seed còn có các bản ghi bổ sung sau:

| Bảng | Mã | Tên trong SQL Server | Quyết định xử lý | Trạng thái ảnh |
|---|---|---|---|---|
| `PhongChup` | `P5A94A4` | `Phòng 4D` | Bản ghi thật có nghĩa → Đã tạo ảnh AI studio 4D (`room_P5A94A4_cover_ai.jpg`) + sửa bug tiền tố `P0` → `P` trong `CrudPage.cs` | **`LocalAiDefault` (Ảnh AI)** |
| `TaiNguyen` | `TNA1DC12` | `Đèn trung thu` | Đạo cụ có tên cụ thể → Đã tạo ảnh AI đèn lồng trung thu (`res_TNA1DC12_cover_ai.jpg`) | **`LocalAiDefault` (Ảnh AI)** |
| `TaiNguyen` | `TN501515` | `Micro` | Thiết bị thu âm có tên cụ thể → Đã tạo ảnh AI micro thu âm (`res_TN501515_cover_ai.jpg`) | **`LocalAiDefault` (Ảnh AI)** |
| `GoiChup` | `GC9CD624` | `test` | Bản ghi QA test mơ hồ (`50.000đ`, mô tả `"test"`) → **Chủ ý giữ Fallback minh bạch** (`"Chưa có ảnh chụp riêng"`) | **`SystemFallback`** |
| `TaiNguyen` | `TNDA9E3F` | `Đèn test` | Bản ghi QA test mơ hồ (`"test Đèn"`) → **Chủ ý giữ Fallback minh bạch** (`"Chưa có ảnh chụp riêng"`) | **`SystemFallback`** |
| `DichVu` | `DV9C0D30`, `DV06EBD7` | `test`, `Thuê đèn` | Dịch vụ bổ sung trong bảng tính không sử dụng thẻ ảnh danh mục | *Không áp dụng ảnh* |

- **Tổng kết toàn kho ảnh đóng gói trên đĩa (`src\StudioManager.WinForms\Assets\`):** **25 tệp JPEG** (**23 ảnh AI độ phân giải cao** + **2 tấm đồ họa Fallback GDI+**).
- **Tổng kết thực thể danh mục hiện diện trong DB (Seed + Live SQL):** **21 / 23 bản ghi có ảnh AI (91,3%)**, **2 / 23 bản ghi test dùng Fallback trung thực (8,7%)**.

---

## C. CÁC LỖI GIAO DIỆN / UX / MAPPING ĐÃ PHÁT HIỆN & KHẮC PHỤC TRONG ĐỢT KIỂM TOÁN CUỐI

1. **[P0 — ĐÃ SỬA] Top Header `CustomerMainForm` quá chật (`58px`) & lỗi margin cắt chữ `_topTitleLabel`:**
   - **Hiện trạng trước sửa:** Top bar cao chỉ `58px` với `_topPathLabel` đơn dòng chật hẹp; khi nâng lên `80px` ở lần dựng đầu, do default `Margin(3,3,3,3)` trong `TableLayoutPanel` làm tụt chiều cao label khiến nửa dưới tiêu đề lớn bị cắt.
   - **Khắc phục:** Thiết lập thanh header `80px` đồng bộ với logo `80px` bên trái, `Margin = Padding.Empty`, breadcrumb monospace (`ATELIER LUMIÈRE // ...`) + tiêu đề phân cấp cỡ `13.2pt Bold` (`45px` không gian dọc, căn giữa theo trục dọc), nút `"Đặt lịch ngay"` + pill `"CHẾ ĐỘ DEMO CỤC BỘ"` căn giữa sạch sẽ ở cả `1040×660`, `1366×768` và `1920×1080`.
2. **[P0 — ĐÃ SỬA] `CrudPage.cs` bỏ sót mã phòng ngoài chuẩn `P0*` (như `P5A94A4 — Phòng 4D`):**
   - **Hiện trạng:** Điều kiện cũ kiểm tra `raw.StartsWith("P0", ...)` khiến phòng tạo động từ hệ thống (`P5A94A4`, mã hex) không khớp trực tiếp vào `VisualAssetService`.
   - **Khắc phục:** Mở rộng điều kiện nhận diện sang `raw.StartsWith("P", ...)` và fallback theo cột có tiêu đề chứa `"Mã"`, giúp tất cả phòng chụp/gói/tài nguyên tự sinh khớp chính xác `VisualAssetService.GetGallery(code)`.
3. **[P1 — ĐÃ SỬA] Phủ ảnh AI cho 3 bản ghi thật phát hiện từ SQL Server (`P5A94A4`, `TNA1DC12`, `TN501515`):**
   - Sinh mới 3 ảnh biên tập Atelier Lumière không watermark/text (`1264×848`), đăng ký vào `VisualAssetService.RealAiPhotographyTargets` & `BuildBaselineEntries()` và cập nhật test kiểm kê `FrontendV22Tests` (25 tệp JPEG).
4. **[P1 — ĐÃ SỬA] Rò rỉ ký tự `&` biến thành phím tắt gạch chân/mất dấu `&` trên một số `Label`:**
   - Bổ sung `UseMnemonic = false` cho các `Label` có dấu `&` tại `CustomerSpacesResourcesPage` (`"TRANG PHỤC & ĐẠO CỤ"`), `DashboardPage` (`"STUDIO CALL SHEET & LIVE STREAM"`, `"Lịch trực phòng máy & Hoạt động"`, `"Danh sách Lịch chụp & Đơn hàng Gần nhất"`), `CustomerBookingWizardPage`, `CustomerBookingsPage`, và `CustomerAlbumsPage`.
5. **[P2 — ĐÃ SỬA] Bố cục Step 5 Wizard & Sticky Summary Panel:**
   - Trong `CustomerBookingWizardPage`: bổ sung tự động co giãn chiều rộng `confirmCard` & `ruleCard` ở Bước 5 bằng `TextRenderer.MeasureText` để không cắt chữ ở `1040×660`; tinh chỉnh chiều cao ảnh thu nhỏ sticky panel (`150 → 122px`) để dòng `TỔNG TẠM TÍNH` hiển thị trọn vẹn ngay trên màn hình `1366×768` không bị khuất mép dưới.
   - Trong `CustomerAlbumsPage`: tăng chiều cao `sampleBanner` (`52 → 58px`) để không cấn 2 dòng text ở cửa sổ tối thiểu.

---

## D. TỒN ĐỌNG MỞ & RANH GIỚI BÀN GIAO CHO BACKEND (OPEN ISSUES / BACKEND PENDING)

Các điểm sau **không phải lỗi cắt/vỡ UI**, mà thuộc phạm vi giai đoạn Backend Codex hoặc ghi nhận môi trường máy hiện tại:

1. **Tầng lưu trữ SQL Server cho luồng Khách hàng (`DesktopDemoSessionStore` → `AppFacade` + `SqlStudioRepository`):**
   - Các thao tác đăng ký khách hàng, gửi yêu cầu đặt lịch `REQ-*`, duyệt chuyển đổi Tab `2.2`, khai báo VietQR & đối soát Tab `2.3`, upload/đổi bìa trong `AdminMediaManagerDialog`, và lưu nhật ký kiểm toán tương ứng đang chạy trên bộ nhớ phiên Demo (zero SQL writes theo chỉ thị đóng băng Backend). Chi tiết ánh xạ 14 hợp đồng `CTR-01..14` đã chốt trong [`CODEX_BACKEND_HANDOFF.md`](./CODEX_BACKEND_HANDOFF.md).
2. **Màn hình Admin/Staff khi mở trực tiếp từ harness không đăng nhập SQL:**
   - Khi mở trực tiếp `DashboardPage` hoặc `CrudPage` trong harness mà `AppFacade.Session == null`, bảng lưới giữ trạng thái trống/chờ SQL (đây là hành vi bảo vệ truy cập đúng thiết kế). Toàn bộ chức năng đọc/ghi SQL cho Staff/Admin vẫn giữ nguyên không chỉnh sửa.
3. **Bản ghi dữ liệu kiểm thử trong SQL cục bộ (`GC9CD624 test`, `TNDA9E3F Đèn test`, giá `GC000001` đã sửa thành `1.000.000đ` so với `1.500.000đ` ở Seed):**
   - Frontend hoàn toàn thích ứng đọc-chỉ-xem (`TryLoadLivePackagesReadOnlyAsync`, `TryLoadLiveRoomsReadOnlyAsync`, `TryLoadLiveResourcesReadOnlyAsync`) và trả về `SystemFallback` minh bạch cho 2 mã test này. Backend cần cân nhắc dọn dữ liệu test trước khi đóng gói sản xuất.
4. **Quy trình Debug lock tại thời điểm kiểm toán:**
   - Trên máy host tồn tại tiến trình `StudioManager.WinForms.exe` (PID `12552`, chạy từ `dotnet run` phiên trước) giữ khóa tệp `src\StudioManager.WinForms\bin\Debug\net8.0-windows\StudioManager.WinForms.exe`. Nhờ vậy cấu hình `Release` được biên dịch lại đầy đủ `0 Warning(s), 0 Error(s)` và chạy kiểm thử `SmokeVerify` + `44/44` xUnit pass; chỉ cần đóng tiến trình Debug đang mở trước khi bấm F5 / Debug Build lại trong Visual Studio.

