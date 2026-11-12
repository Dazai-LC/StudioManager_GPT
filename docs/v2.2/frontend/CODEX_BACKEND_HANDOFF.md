# STUDIOMANAGER v2.2 — TÀI LIỆU BÀN GIAO CHO CODEX (BACKEND & SQL SERVER)
## CODEX BACKEND & DATABASE IMPLEMENTATION HANDOFF

**Người chuẩn bị:** Frontend Team (Antigravity)  
**Đối tượng tiếp nhận:** Codex (Backend / Infrastructure / SQL Server Developer)  
**Branch hiện tại:** `feature/v2.2-frontend`  
**Nguyên tắc bàn giao:** Toàn bộ tầng Frontend WinForms & Contracts (`src\StudioManager.WinForms\Contracts\`) đã hoàn tất và nghiệm thu; giai đoạn tiếp theo sẽ chuyển đổi `DesktopDemoSessionStore` sang dịch vụ SQL Server thực tế trong `StudioManager.Domain`, `StudioManager.Application`, `StudioManager.Infrastructure` và script `database/`.

---

## 1. HIỆN TRẠNG DATABASE HIỆN HỮU (`database/02_CreateSchema.sql` & `03_SeedData.sql`)

Cơ sở dữ liệu hiện có **14 bảng vật lý** (chưa bị chỉnh sửa ở giai đoạn Frontend):
1. `NhanVien` (`NhanVienId`, `MaNhanVien`, `HoTen`, `SoDienThoai`, `ChucVu` ∈ `TIEP_NHAN | NHIEP_ANH_GIA | KHAC`, `TrangThai` ∈ `DANG_LAM | NGUNG_LAM`)
2. `TaiKhoan` (`TaiKhoanId`, `NhanVienId`, `TenDangNhap`, `MatKhauHash`, `VaiTro` ∈ `QUAN_TRI_VIEN | NHAN_VIEN`, `TrangThai`, `PhaiDoiMatKhau`, `LanDangNhapCuoi`)
3. `KhachHang` (`KhachHangId`, `MaKhachHang`, `HoTen`, `SoDienThoai`, `DiaChi`, `Email`, `GhiChu`)
4. `GoiChup`, `DichVu`, `PhongChup`, `TaiNguyen`
5. `LichChup`, `LichChupDichVu`, `PhanCongTaiNguyen`
6. `ThanhToan` (`LoaiThu` ∈ `DAT_COC | BO_SUNG | CON_LAI`), `HoanTien`
7. `NhatKyHeThong`, `NhatKySaoLuu`

**Dữ liệu Seed chuẩn (`03_SeedData.sql`):**
- Tài khoản nội bộ: `admin` (`QUAN_TRI_VIEN`), `nhanvien1`, `nhanvien2` (`NHAN_VIEN`).
- Nhiếp ảnh gia: `NV000004..NV000007` (`DANG_LAM`), **`NV000008` Ngô Đức Long là `NGUNG_LAM` (tuyệt đối không đưa vào dropdown phân công lịch mới)**.
- Gói chụp: `GC000001..GC000005`; Dịch vụ: `DV000001..DV000008`; Phòng: `P0001..P0003`; Tài nguyên: `TN000001..TN000010`; Khách mẫu: `KH000001..KH000030`.

---

## 2. ÁNH XẠ CONTRACTS FRONTEND → BẢNG & DỊCH VỤ BACKEND CẦN BỔ SUNG (CTR-01..CTR-14)

Các DTO Frontend đang chờ triển khai Backend nằm trong `src\StudioManager.WinForms\Contracts\`:
- [`CustomerIdentityContracts.cs`](../../src/StudioManager.WinForms/Contracts/CustomerIdentityContracts.cs)
- [`CatalogAndMediaContracts.cs`](../../src/StudioManager.WinForms/Contracts/CatalogAndMediaContracts.cs)
- [`BookingRequestContracts.cs`](../../src/StudioManager.WinForms/Contracts/BookingRequestContracts.cs)
- [`PaymentReceiptAlbumContracts.cs`](../../src/StudioManager.WinForms/Contracts/PaymentReceiptAlbumContracts.cs)

### Bảng kế hoạch mở rộng Schema & Application Services

| Nhóm CTR | Chức năng Frontend đang chạy Demo | Thành phần SQL Server mới cần thêm | Quy tắc nghiệp vụ bắt buộc ở Backend |
|---|---|---|---|
| **CTR-01, CTR-02** | Danh mục Gói, Phòng, Tài nguyên, Kiểm tra xung đột đọc-chỉ-xem (`SeedGroundedCatalogRepository`) | Có thể đọc trực tiếp `GoiChup`, `PhongChup`, `TaiNguyen`, `DichVu`, `LichChup` | Khung giờ trống phải tính theo bảng `LichChup` (`TrangThai <> 'DA_HUY'`) cho cả phòng và `NhiepAnhGiaId` đang `DANG_LAM`. |
| **CTR-03, CTR-04** | Đăng nhập Khách (`CustomerLoginForm`), Đăng ký (`CustomerRegistrationForm`), `CustomerIdentityLinkState` (5 trạng thái), `CustomerProfilePage` | Thêm bảng **`TaiKhoanKhachHang`** (tách biệt với `TaiKhoan` nội bộ hoặc thêm scope `KHACH_HANG` + khóa ngoài nullable tới `KhachHangId`) và bảng **`YeuCauLienKetKhachHang`** (`Unlinked`, `PendingVerification`, `VerifiedOwnership`, `RejectedVerification`, `AuthorizedManualLink`) | Dùng chung `Pbkdf2PasswordHasher` (`PBKDF2-SHA256$210000$...`). **Tuyệt đối không tự động gán `KhachHangId` cũ chỉ qua trùng SĐT/Email khi chưa qua xác minh OTP hoặc Nhân viên kiểm tra thủ công.** Ghi nhận mọi chuyển trạng thái liên kết vào `NhatKyHeThong`. |
| **CTR-05, CTR-06, CTR-07, CTR-08, CTR-09** | Wizard đặt lịch 5 bước (`CustomerBookingWizardPage`) + Tab `2.2 Yêu cầu đặt lịch` (`BookingRequestsPlaceholderPage`) | Thêm **`YeuCauDatLich`** (`MaYeuCau` dạng `YC...`, `TaiKhoanKhachId`, `GoiChupId`, `NgayMongMuon`, `KhungGio`, `PhongUuTienId`, `NhiepAnhGiaDeXuatId`, `TrangThai` ∈ `CHO_XU_LY | CAN_BO_SUNG | DA_XAC_NHAN | DA_TU_CHOI | KHACH_HUY`, `PhanHoiChoKhach`, `GhiChuNoiBo`, `LichChupChuyenDoiId`), **`YeuCauDatLichDichVu`**, **`LichSuYeuCauDatLich`** | Chỉ chấp nhận chuyển đổi `DA_XAC_NHAN` thành `LichChup` trong **1 transaction ACID** sau khi kiểm tra không xung đột phòng/thợ/tài nguyên và cập nhật `LichChupChuyenDoiId` (idempotent, chống click đúp tạo 2 lịch). |
| **CTR-10, CTR-11, CTR-12** | `VietQrCardControl`, khai báo chứng từ (`CustomerPaymentsPage`), Tab `2.3` (`PaymentWorkspacePlaceholderPage`), `ExtendedReceiptPreviewForm` | Thêm **`GiaoDichChoDoiSoat`** (`MaGiaoDich`, `LoaiThamChieu` `YEU_CAU | LICH_CHUP`, `MaThamChieu`, `PhuongThuc`, `SoTienKhaiBao`, `SoTienXacNhan`, `NoiDungChuyenKhoan` bắt buộc `PHOENIX [Mã]`, `DuongDanAnhChungTu`, `TrangThai` ∈ `CHO_DOI_SOAT | DA_KHOP_THU_CONG | SAI_LECH_THONG_TIN | THANH_TOAN_TIEN_MAT | TU_CHOI`, `NguoiDoiSoatId`, `GhiChuDoiSoat`) | **CẤM TÍCH HỢP SEPAY / WEBHOOK / TỰ ĐỘNG XÁC NHẬN.** Quét QR chỉ lưu khai báo chờ duyệt. Chỉ khi Nhân viên bấm khớp thủ công hoặc thu tiền mặt mới ghi vào bảng bất biến **`ThanhToan`** và sinh mã biên nhận chuẩn hóa cho A4/K80/K58/PDF. |
| **CTR-13** | `VisualAssetService`, `AdminMediaManagerDialog`, `CustomerAlbumsPage` | Thêm **`HinhAnhDanhMuc`** (`EntityType` ∈ `GoiChup | PhongChup | TaiNguyen`, `TargetCode`, `FilePath`, `IsPrimaryCover`, `SourceKind`, `SortOrder`, `Caption`, `AltText`) và **`AlbumKhachHang`**, **`AnhAlbumKhachHang`** | Chỉ `QUAN_TRI_VIEN` có quyền INSERT/UPDATE/DELETE trên `HinhAnhDanhMuc`; `NHAN_VIEN` chỉ đọc; khách hàng chỉ tải danh sách `AnhAlbumKhachHang` khi `CustomerIdentityLinkState` ∈ `{VerifiedOwnership, AuthorizedManualLink}` và khớp chủ sở hữu `KhachHangId`. |
| **CTR-14** | Kiểm toán & RBAC mở rộng | Mở rộng enum `LoaiDoiTuong` và `HanhDong` trong `NhatKyHeThong` | Bắt buộc ghi audit log cho: duyệt/từ chối/chuyển đổi `YeuCauDatLich`, khớp/từ chối `GiaoDichChoDoiSoat`, liên kết thủ công `KhachHangId`, thay đổi ảnh danh mục. |

---

## 3. ĐIỂM THAY THẾ TRONG WINFORMS KHI NỐI BACKEND

1. Thay thế lời gọi tĩnh trong [`DesktopDemoSessionStore.cs`](../../src/StudioManager.WinForms/Simulation/DesktopDemoSessionStore.cs) và [`SeedGroundedCatalogRepository.cs`](../../src/StudioManager.WinForms/Simulation/SeedGroundedCatalogRepository.cs) bằng các service interface tiêm qua `AppFacade`.
2. Giữ nguyên toàn bộ lớp UI WinForms (`CustomerLoginForm`, `CustomerRegistrationForm`, `CustomerMainForm`, 8 `CustomerPages\`, `BookingRequestsPlaceholderPage`, `PaymentWorkspacePlaceholderPage`, `AdminMediaManagerDialog`, `ExtendedReceiptPreviewForm`) — chỉ cần nối `async Task` API từ `AppFacade` và bỏ nhãn `"DEMO"` trên dữ liệu thật sau khi kích hoạt SQL persistence.
3. Tuyệt đối giữ nguyên ràng buộc `Application` & `Domain` tests hiện hữu tại `tests\StudioManager.Tests\` và mở rộng thêm Integration Tests khi có LocalDB / SQL Server test instance.

---

## 4. CẬP NHẬT KIẾN TRÚC QA & UX POLISH TRƯỚC BÀN GIAO (P0/P1)

1. **Đăng nhập hợp nhất & Điều hướng theo vai trò (`CustomerIdentityContracts.cs`, `LoginForm.cs`):**
   - Frontend đã định nghĩa hợp đồng `IUnifiedAuthenticationService`, `AuthenticatedPrincipal`, `AuthRoutingDecision`, `UserSecurityRole`, `TargetDesktopShell`, và `UnifiedAuthRouter.ResolveRoute`.
   - Khi nối SQL Auth cho Khách hàng, Codex triển khai `IUnifiedAuthenticationService` xác thực thống nhất `TaiKhoan` nội bộ (`MainForm`) và `TaiKhoanKhachHang` (`CustomerMainForm`) qua một biểu mẫu đăng nhập duy nhất `LoginForm`, đồng thời gỡ bỏ khung `"XEM TRƯỚC CỔNG KHÁCH HÀNG (FRONTEND DEMO)"` ở chân card đăng nhập.
2. **Cô lập dữ liệu theo chủ sở hữu & Quyền xem lịch sử:**
   - Mọi truy vấn yêu cầu đặt lịch, lịch chụp, giao dịch thanh toán, album ảnh và thông báo (`GetNotifications(demoCustomerId)`) phải lọc theo đúng `TaiKhoanKhachHangId` và khóa ngoài `KhachHangId` đã xác minh (`VerifiedOwnership` / `AuthorizedManualLink`).
   - Khách hàng chỉ được phép gửi yêu cầu đối chiếu (`SubmitCustomerIdentityVerificationRequest` → trạng thái `PendingVerification`), tuyệt đối không có API tự nâng trạng thái sang đã xác minh ở giao diện khách. Drawer mô phỏng 5 trạng thái (`_qaToggleBtn` / `_qaDrawerBody` trong `CustomerProfilePage.cs`) chỉ phục vụ QA/Dev và cần ẩn hoặc khóa theo build debug/admin khi lên production.
3. **Đồng bộ giao dịch khép kín (Yêu cầu → Lịch chụp → Đối soát → Biên nhận):**
   - Chuyển yêu cầu sang `DA_XAC_NHAN` phải tạo/cập nhật bản ghi `LichChup` (idempotent qua `LichChupChuyenDoiId`), mang theo phòng, nhiếp ảnh gia `DANG_LAM`, dịch vụ đính kèm và tổng tiền đã đối soát.
   - Khớp giao dịch thủ công ở Tab `2.3` (`DA_KHOP_THU_CONG` / `THANH_TOAN_TIEN_MAT`) phải cập nhật công nợ lịch/yêu cầu nguồn và sinh `ReceiptPreviewData` đồng nhất giữa Khách hàng & Nhân viên trên cả 4 khổ A4/K80/K58/PDF. Không tích hợp SePay SDK hay webhook tự động.
4. **Hệ thống thông báo & Tài sản ảnh (`IMG-02`):**
   - Duy trì sử dụng `AtelierNotificationService` (`AtelierDialog`, `AtelierToast`, `AtelierInlineAlert`) và `Ui.Error` / `Ui.Info`; không dùng lại `MessageBox.Show`.
   - Toàn bộ **18/18 thực thể Seed (`GC000001..05`, `P0001..03`, `TN000001..10`)**, **3/3 bản ghi thật ngoài Seed đang có trên SQL Server (`P5A94A4 — Phòng 4D`, `TNA1DC12 — Đèn trung thu`, `TN501515 — Micro`)** và **2 nền xác thực** đã có ảnh AI độ phân giải cao cục bộ (23 tệp AI + 2 tệp fallback); các mã kiểm thử hoặc mã mới chưa gắn ảnh (`GC9CD624 test`, `TNDA9E3F Đèn test`) rơi về `CreateFallbackEntry` kèm nhãn `"Hình thay thế"` minh bạch. Giai đoạn Backend cần bổ sung luồng tải ảnh thật cho `AdminMediaManagerDialog` và kho ảnh album bàn giao thật của khách hàng (`CustomerAlbumsPage`).
5. **Thống nhất & mở rộng kiến trúc shell cho Khách hàng:**
   - `CustomerMainForm` sử dụng header `80px` với breadcrumb + tiêu đề phân cấp và nút `"Đặt lịch ngay"` + nhãn `"CHẾ ĐỘ DEMO CỤC BỘ"`; khi nối Backend thật cần bỏ/ẩn nhãn `"DEMO"` trên các màn hình sử dụng dữ liệu SQL trực tiếp và cập nhật `SeedGroundedCatalogRepository` / `CrudPage` dùng dữ liệu DB làm nguồn sự thật duy nhất (chỉ giữ `Demo` fallback khi offline hoàn toàn).

