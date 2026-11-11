# STUDIOMANAGER v2.2 — PHASE 01: FRONTEND FOUNDATIONS & DESKTOP NAVIGATION
**Document Version:** 1.1.0 (Phase 01 Final Correction)  
**Author:** Lead Frontend Engineer & WinForms UX/UI Specialist (Antigravity)  
**Approved Design Reference:** STUDIOMANAGER v2.2 — FINAL LOCKED UX BLUEPRINT (REVISION 1)  
**Target Solution:** StudioManager.sln (WinForms .NET 8 / Future Next.js Customer Portal)  

---

## 1. TỔNG QUAN KIẾN TRÚC (ARCHITECTURE SUMMARY)

StudioManager v2.2 (Project Phoenix / Atelier Lumière) mở rộng hệ thống vận hành Studio ảnh nghệ thuật chuyên nghiệp với mô hình đa giao diện (Multi-surface Architecture):
1. **Desktop Management Hub (WinForms .NET 8 C# 12):** Ứng dụng điều hành trung tâm dành cho Quản trị viên (Admin) và Nhân viên Studio (Staff). Hoạt động theo mô hình Code-First WinForms, GDI+ vector rendering, PerMonitorV2 DPI awareness.
2. **Customer Portal (Web Next.js App Router / TypeScript / Tailwind CSS):** Cổng dịch vụ trực tuyến dành cho khách hàng tìm hiểu gói chụp, gửi yêu cầu đặt lịch, đối soát thanh toán chuyển khoản VietQR và nhận bàn giao sản phẩm ảnh nghệ thuật (bắt đầu triển khai từ Phase 02).

### Chiến lược Phân kỳ & Ranh giới (Phase Order & Boundaries)
- **Quy tắc bất biến:** Antigravity hoàn thiện và người dùng nghiệm thu toàn bộ tầng Frontend trước khi Codex phát triển Backend, Database và API mới.
- **Ranh giới dữ liệu:** Toàn bộ tầng Backend (`StudioManager.Domain`, `StudioManager.Application`, `StudioManager.Infrastructure`), cơ sở dữ liệu SQL Server và các đặc tả v2.1 được giữ nguyên trạng (Strictly Read-Only).
- **Phạm vi thanh toán được phê duyệt:** Final Locked UX Blueprint Revision 1 chỉ phê duyệt **VietQR, chuyển khoản thủ công, đối soát nhân viên và thanh toán tiền mặt tại quầy**. Tuyệt đối không tích hợp cổng tự động bên thứ ba (không SePay SDK, không Webhook, không API thanh toán tự động).
- **Mục tiêu Phase 01:** Thiết lập hạ tầng điều hướng 3 tab trong Hub `Lịch chụp` (Lịch chụp chính thức, Yêu cầu đặt lịch, Đối soát & Thanh toán), tích hợp an toàn `MainForm`, cô lập dữ liệu mô phỏng, và chuẩn hóa hợp đồng dữ liệu giao diện (Contracts CTR-01..14 & Design System Tokens).

---

## 2. DANH SÁCH FILE THAY ĐỔI & BỔ SUNG (FILES CHANGED)

| STT | Tệp tin | Trạng thái | Phân loại | Mục đích & Trách nhiệm |
|---|---|---|---|---|
| 1 | `src/StudioManager.WinForms/Forms/MainForm.cs` | Modified | WinForms Presentation | Cập nhật điều hướng Hub `bookings`: cấu hình 3 sub-tab qua `SegmentedTabBar`, định tuyến `OpenSubView`, cập nhật palette lệnh `Ctrl+K`. |
| 2 | `src/StudioManager.WinForms/Pages/BookingRequestsPlaceholderPage.cs` | Added (Untracked) | WinForms Presentation | Giao diện mô phỏng Tab 2.2 (`Yêu cầu đặt lịch`): KPI card, bộ lọc, lưới danh sách yêu cầu, bảng dossier chi tiết quy trình tiếp nhận. |
| 3 | `src/StudioManager.WinForms/Pages/PaymentWorkspacePlaceholderPage.cs` | Added (Untracked) | WinForms Presentation | Giao diện mô phỏng Tab 2.3 (`Đối soát & Thanh toán`): KPI VietQR/Thanh toán Studio, bộ lọc giao dịch, lưới thanh toán, bảng dossier chi tiết quy tắc đối soát an toàn CTR-10..CTR-12. |
| 4 | `docs/v2.2/frontend/PHASE01_FOUNDATIONS.md` | Added (Untracked) | Tài liệu kỹ thuật | Tài liệu kiến trúc nền tảng Frontend Phase 01, hợp đồng Provider, ranh giới an toàn Demo, bảng ánh xạ Design Tokens. |

*Lưu ý xác nhận an toàn:* Không có bất kỳ tệp tin nào thuộc Domain, Application, Infrastructure, SQL Migrations hay Database bị chỉnh sửa hoặc phát sinh.

---

## 3. CẤU TRÚC ĐIỀU HƯỚNG DESKTOP (NAVIGATION STRUCTURE)

Hệ thống điều hướng Desktop bảo toàn 5 primary hubs trên thanh điều hướng dọc `ObsidianRailPanel`:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ObsidianRail (84px) │ TopCommandBarPanel (72px)                              │
├─────────────────────┼───────────────────────────────────────────────────────┤
│ [Logo] Atelier      │ BREADCRUMB  //  WORKSPACE                             │
│                     │ Hub Title Header                  [Ctrl+K] [UserPill] │
│ [1] Bàn làm việc    ├───────────────────────────────────────────────────────┤
│ [2] Lịch chụp  ───► │ SubNavHost / SegmentedTabBar (50px)                   │
│                     │ ├── [Tab 2.1] Lịch chụp chính thức (BookingsPage v2.1)│
│ [3] Danh mục        │ ├── [Tab 2.2] Yêu cầu đặt lịch (Simulation v2.2)      │
│ [4] Báo cáo (Admin) │ └── [Tab 2.3] Đối soát & Thanh toán (Simulation v2.2) │
│ [5] Hệ thống (Admin)├───────────────────────────────────────────────────────┤
│                     │ Workspace Content Area (Dock: Fill)                   │
│ [Key] Mật khẩu      │                                                       │
│ [Exit] Thoát        │                                                       │
└─────────────────────┴───────────────────────────────────────────────────────┘
```

### Chi tiết 3 Sub-tabs trong Hub Lịch chụp (`bookings`):

### 3.1. Tab 2.1 — Lịch chụp chính thức (`LichChup`)
- **Hiển thị nhãn:** `Lịch chụp chính thức` (Icon: `IconKind.Calendar`).
- **Nội dung:** Nhúng nguyên bản `BookingsPage` từ phiên bản Phoenix v2.1 đã ổn định.
- **Tính năng bảo toàn 100%:**
  - Tra cứu đa tiêu chí: text search, khoảng thời gian chụp (`from`/`to`), lọc theo nhiếp ảnh gia, phòng chụp, gói chụp, trạng thái.
  - Pipeline ribbon trạng thái: Tất cả, Chờ xử lý, Đã xác nhận, Đang chụp, Hậu kỳ, Chờ bàn giao, Hoàn tất, Đã hủy.
  - Tạo mới lịch chụp (`CreateAsync`), Sửa lịch (`EditAsync`), Hủy lịch (`CancelAsync`).
  - Tạo nhanh hồ sơ khách hàng (`QuickCreateCustomerAsync`).
  - Xem chi tiết hồ sơ đặt lịch (`DetailAsync`).
  - Điều phối tài nguyên: Gán phòng chụp, gán thiết bị/nhiếp ảnh gia (`AssignResourceAsync`).
  - Quản lý phụ phí dịch vụ (`AddServiceAsync`), chiết khấu ưu đãi (`AddDiscountAsync`).
  - Giao dịch tài chính v2.1: Thu tiền cọc/thanh toán (`AddPaymentAsync`), hoàn trả (`AddRefundAsync`), in lại phiếu thu lịch sử (`PrintReceiptAsync`).
  - Kiểm soát phân quyền: Staff và Admin truy cập đúng quyền hạn đã cấu hình.

### 3.2. Tab 2.2 — Yêu cầu đặt lịch (`YeuCauDatLich`)
- **Hiển thị nhãn:** `Yêu cầu đặt lịch` (Icon: `IconKind.Clock`).
- **Nội dung Phase 01:** Giao diện mô phỏng tiếp nhận yêu cầu (Presentation-only placeholder).
- **Thành phần giao diện:**
  - Banner cảnh báo trực quan: Nhãn `MÔ PHỎNG PHÁT TRIỂN / CHƯA KẾT NỐI SQL SERVER`, mô tả luồng tiếp nhận từ Web Customer Portal (Phase 02/03).
  - 4 thẻ đo lường (StatCard): *Yêu cầu mới 24h*, *Chờ studio tư vấn*, *Đã xác nhận & cọc*, *Tổng cọc dự kiến*.
  - Thanh công cụ tra cứu & bộ lọc nhanh: *Tất cả*, *Chờ duyệt*, *Đã liên hệ*, nút *Làm mới*.
  - Lưới danh sách yêu cầu mô phỏng (DataGrid 6 cột chuẩn Atelier Lumière).
  - Khu vực Dossier chi tiết bên phải: Thông tin khách hàng, số điện thoại, gói chụp, concept ghi chú, phòng đề xuất, cọc dự kiến, và sơ đồ quy trình 4 bước tiếp nhận.
- **Ranh giới an toàn:** Không tạo record trong bảng `LichChup`, không can thiệp SQL Server.

### 3.3. Tab 2.3 — Đối soát & Thanh toán (`DoiSoatThanhToan`)
- **Hiển thị nhãn:** `Đối soát & Thanh toán` (Icon: `IconKind.Wallet`).
- **Nội dung Phase 01:** Giao diện mô phỏng trung tâm đối soát giao dịch và thanh toán (Presentation-only placeholder).
- **Thành phần giao diện:**
  - Banner cảnh báo: Nhãn `MÔ PHỎNG ĐỐI SOÁT / VIETQR & THANH TOÁN STUDIO`, mô tả luồng đối soát chuyển khoản VietQR, đối soát nhân viên và thanh toán tiền mặt.
  - 4 thẻ đo lường tài chính studio: *Thu chuyển khoản VietQR*, *Đã đối soát khớp*, *Chờ nhân viên đối soát*, *Hoàn tiền & Điều chỉnh*.
  - Thanh tra cứu mã giao dịch, mã lịch và bộ lọc: *Tất cả*, *Đã khớp*, *Chờ duyệt*, nút *Làm mới*.
  - Lưới giao dịch đối soát (DataGrid 7 cột: Mã GD, Mã tham chiếu, Khách hàng, Số tiền, Hình thức, Cú pháp, Trạng thái).
  - Khu vực Dossier quy tắc đối soát an toàn (CTR-10..CTR-12): Kiểm tra cú pháp chuẩn `PHOENIX [Mã]`, tính bất biến của sổ cái phiếu thu, xử lý cách ly giao dịch sai lệch cho nhân viên tra soát.
- **Ranh giới an toàn:** Không phát sinh bản ghi `ThanhToan`, không sửa đổi số dư tài chính.

---

## 4. GHI CHÚ TÍCH HỢP MAINFORM (MAINFORM INTEGRATION NOTES)

1. **Kiểm soát hình học `WorkspaceHostPanel`:**
   - Chiều cao Top Header: cố định `72px`.
   - Chiều cao SubNav: `50px` khi hiển thị (`SubNavVisible = true`), `0px` khi ẩn.
   - Chiều cao Content Area: `ClientSize.Height - HeaderHeight - subH`, luôn tính toán lại chính xác tại `OnLayout` mà không phụ thuộc vào DockStyle lồng nhau phức tạp.
   - Ngăn chặn hoàn toàn lỗi che khuất tiêu đề hoặc thanh phụ thuộc xuất hiện ở các phiên bản trước.
2. **Cơ chế chuyển đổi Sub-navigation:**
   - Trong `NavigateTo("bookings", subKey)`:
     Khởi tạo danh sách 3 `SegmentItem`, xác định `targetBookingKey` mặc định là `LichChup` (hoặc giữ tab con đang mở nếu người dùng quay lại từ cùng phân hệ).
     Kích hoạt `_subNav.SetItems(...)`, bật `_subNav.Visible = true; _workspaceHost.SubNavVisible = true;`, sau đó gọi `OpenSubView("bookings", targetBookingKey)`.
   - Chuyển sang `dashboard` hoặc `reports`: `_subNav.Visible = false; _workspaceHost.SubNavVisible = false;`, không gian làm việc tự động giãn nở thu hồi `50px` của thanh phụ.
   - Chuyển sang `catalog` hoặc `system`: Thanh sub-nav được tái sử dụng để nạp các mục tương ứng (`KhachHang`, `NhanVien`, ... hoặc `TaiKhoan`, `NhatKy`, `SaoLuu`).
3. **Phím tắt và Command Palette:**
   - `Alt+1..5`: Duy trì điều hướng tức thì tới 5 hub chính. `Alt+2` điều hướng trực tiếp vào Hub `Lịch chụp`.
   - `Ctrl+K` (Command Palette): Bổ sung đầy đủ 3 lệnh con trực tiếp:
     - `Lịch chụp chính thức & Tiến độ` (`bookings` -> `LichChup`)
     - `Hộp thư Yêu cầu đặt lịch trực tuyến (v2.2 Demo)` (`bookings` -> `YeuCauDatLich`)
     - `Trung tâm Đối soát & Thanh toán (v2.2 Demo)` (`bookings` -> `DoiSoatThanhToan`)

---

## 5. HỢP ĐỒNG DỮ LIỆU PORTAL & ÁNH XẠ CTR (PLANNED PORTAL CONTRACTS)

Trong Phase 02, ứng dụng Web Customer Portal sẽ được xây dựng trên nền Next.js App Router với kiến trúc Provider phân tầng trừu tượng:

```
┌─────────────────────────────────────────────────────────────┐
│                    Customer Portal UI                       │
└──────────────────────────────┬──────────────────────────────┘
                               │
                ┌──────────────▼──────────────┐
                │    IPortalDataProvider      │ (TypeScript Contract)
                └──────┬───────────────┬──────┘
                       │               │
        ┌──────────────▼──────┐ ┌──────▼─────────────┐
        │MockPortalDataProvider│ │ApiPortalDataProvider│ (Future Phase / Codex)
        │ (In-Memory Fixtures)│ │  (REST / Web API)  │
        └─────────────────────┘ └────────────────────┘
```

### Ánh xạ Hợp đồng Chuẩn CTR-01..CTR-14:
- **CTR-01:** Danh mục Gói chụp Công khai (Public Catalog & Package Showcase)
- **CTR-02:** Tra cứu Lịch trống & Khung giờ (Public Availability & Timeslot Query)
- **CTR-03:** Định danh & Xác thực Khách hàng OTP/Passkey (Customer Authentication & Identity)
- **CTR-04:** Hồ sơ Khách hàng & Lịch sử Đặt lịch Liên kết (Customer Profile & Linked Bookings)
- **CTR-05:** Nộp Yêu cầu Đặt lịch Trực tuyến (Customer Booking Request Submission)
- **CTR-06:** Hủy / Điều chỉnh Yêu cầu Đặt lịch (Booking Request Revision & Cancellation)
- **CTR-07:** Tiếp nhận & Thẩm tra Yêu cầu trên Desktop (Desktop Request Ingestion & Review)
- **CTR-08:** Chuyển đổi 1-Click sang Lịch chụp Chính thức (1-Click Request Conversion to LichChup)
- **CTR-09:** Điều phối Tài nguyên & Chống Xung đột Phòng/Thợ (Studio Resource Allocation & Anti-conflict)
- **CTR-10:** Sinh mã thanh toán VietQR & Cú pháp chuyển khoản (VietQR Dynamic Payload Generation)
- **CTR-11:** Xác minh Đặt cọc & Đối soát Nhân viên (Deposit Verification & Staff Reconciliation Queue)
- **CTR-12:** Ghi nhận Phiếu thu Bất biến & Sổ cái Kế toán (Immutable Ledger Entry & Receipt Archival)
- **CTR-13:** Quản lý Hình ảnh Nghệ thuật & Bàn giao Sản phẩm (Media Gallery & Deliverable Management)
- **CTR-14:** Quản lý Tài khoản Nâng cao & Phân quyền Kiểm toán (Advanced Account & Audit RBAC)

### Đặc tả Interface TypeScript dự kiến (`IPortalDataProvider`):

```typescript
export interface IPortalDataProvider {
  // 1. Catalog & Packages (CTR-01, CTR-02)
  getPackages(): Promise<PortalPackageDto[]>;
  getPackageBySlug(slug: string): Promise<PortalPackageDetailDto | null>;
  getAvailableTimeSlots(date: string, packageId: string): Promise<TimeSlotDto[]>;

  // 2. Customer Identity & Auth (CTR-03, CTR-04)
  requestOtp(phone: string): Promise<{ sessionId: string }>;
  verifyOtp(sessionId: string, code: string): Promise<CustomerSessionDto>;
  getCustomerProfile(): Promise<CustomerProfileDto>;
  updateCustomerProfile(profile: UpdateCustomerProfileDto): Promise<CustomerProfileDto>;

  // 3. Booking Requests (CTR-05, CTR-06)
  createBookingRequest(request: CreateBookingRequestDto): Promise<BookingRequestResultDto>;
  getCustomerBookingRequests(): Promise<BookingRequestSummaryDto[]>;
  getBookingRequestDetail(requestId: string): Promise<BookingRequestDetailDto>;
  cancelBookingRequest(requestId: string, reason: string): Promise<boolean>;

  // 4. Confirmed Bookings & Deliverables (CTR-07, CTR-08, CTR-13)
  getCustomerBookings(): Promise<CustomerBookingDto[]>;
  getBookingDetail(bookingId: string): Promise<CustomerBookingDetailDto>;
  getBookingMediaGallery(bookingId: string): Promise<MediaGalleryDto>;

  // 5. Payment & Invoicing (CTR-10, CTR-11, CTR-12)
  generateVietQr(bookingOrRequestId: string, paymentType: 'DEPOSIT' | 'FINAL'): Promise<VietQrPayloadDto>;
  getPaymentHistory(bookingId: string): Promise<PaymentReceiptDto[]>;
}
```

---

## 6. HỢP ĐỒNG DỮ LIỆU MÔ PHỎNG DESKTOP (PLANNED DESKTOP DEMO PROVIDERS)

Để phục vụ trình diễn tính năng v2.2 độc lập tại Desktop mà không phụ thuộc Backend chưa khởi tạo:

### Các Provider C# dự kiến trong `StudioManager.WinForms.Simulation`:
1. `IBookingRequestDemoProvider`:
   - `Task<IReadOnlyList<DemoBookingRequest>> GetPendingRequestsAsync();`
   - `Task<DemoBookingRequestDetail> GetRequestDetailAsync(string requestId);`
   - `Task<DemoConversionResult> SimulateConvertToOfficialBookingAsync(string requestId);`
2. `IPaymentReconciliationDemoProvider`:
   - `Task<IReadOnlyList<DemoPaymentTransaction>> GetRecentTransactionsAsync();`
   - `Task<DemoReconciliationSummary> GetTelemetrySummaryAsync();`
   - `Task<DemoManualMatchResult> SimulateManualMatchAsync(string txnId, string bookingCode);`
3. `IIdentityLinkingDemoProvider`:
   - `Task<IReadOnlyList<DemoIdentityLinkRequest>> GetPendingLinkRequestsAsync();`
   - `Task<DemoLinkApprovalResult> SimulateApproveLinkAsync(string linkRequestId);`

Tất cả các Demo Provider đều triển khai theo nguyên tắc In-Memory Singleton hoặc Static Fixture Store, độc lập hoàn toàn với `AppFacade` và `SqlStudioRepository`.

---

## 7. QUY TẮC AN TOÀN DEMO (DEMO SAFETY BOUNDARIES)

### Các nguyên tắc cốt lõi (Hard Requirements)
1. **Không ghi dữ liệu:** Các thao tác duyệt yêu cầu, gán phòng thử nghiệm trên giao diện v2.2 tuyệt đối không phát sinh `INSERT`, `UPDATE`, `DELETE` trên bảng `LichChup`, `ThanhToan`, `TaiKhoan`, `KhachHang`.
2. **Cô lập định danh (ID Isolation):**
   - Mã yêu cầu mô phỏng dùng định dạng: `REQ-202611-xxx`.
   - Mã giao dịch số mô phỏng dùng: `TXN-202611-xxx`.
   - Không dùng trùng không gian ID số nguyên tự tăng của cơ sở dữ liệu hiện hữu.
3. **Phạm vi thanh toán chuẩn:** Hỗ trợ VietQR, chuyển khoản thủ công, đối soát nhân viên và tiền mặt. Tuyệt đối không tích hợp cổng tự động bên thứ ba (không SePay SDK, không Webhook).
4. **Chính sách hiển thị thuật ngữ rõ ràng (Terminology Policy):**
   - Mọi khu vực thử nghiệm phải mang nhãn rõ ràng: `MÔ PHỎNG UI`, `CHƯA KẾT NỐI BACKEND`, `DỮ LIỆU MINH HỌA`.
   - Cổng thanh toán hiển thị: `MÃ QR MÔ PHỎNG — KHÔNG CHUYỂN TIỀN THẬT`.
   - Trạng thái chưa hoàn tất ghi chú: `CHỜ XÁC MINH (DEMO)`.
   - Tuyệt đối không phát sinh thông báo thành công giả tạo gây hiểu lầm là đã thu tiền thực tế hoặc đã lập hợp đồng pháp lý.
5. **Giới hạn đồng bộ xuyên nền tảng (Cross-Surface Limitation):**
   - Store dữ liệu của Desktop WinForms và Web Portal trong các giai đoạn Frontend là 2 kho in-memory độc lập.
   - Không giả lập việc đồng bộ socket thời gian thực khi Backend chưa được phát triển.
   - Để trình diễn luồng đi xuyên suốt (Customer Journey), hai bề mặt sẽ sử dụng chung bộ kịch bản mẫu thống nhất (Scenario Fixture Dataset), ví dụ: Hồ sơ khách hàng *Nguyễn Mai Anh* đặt gói *Heritage Bridal Editorial*.

---

## 8. HỆ THỐNG THIẾT KẾ ĐỒNG BỘ (DESIGN SYSTEM FOUNDATIONS)

Nhận diện hình ảnh "Atelier Lumière" (Phòng ảnh Ánh sáng Di sản) được ánh xạ đồng nhất giữa WinForms GDI+ Tokens và Tailwind CSS Tokens cho Web Portal tương lai:

### Bảng Ánh Xạ Tokens (Desktop WinForms ↔ Tailwind Web Portal)

| Nhóm Token | Token WinForms (`Theme.cs`) | Giá trị Hex | Token Web Tailwind (`tailwind.config.ts`) | Ý nghĩa sử dụng |
|---|---|---|---|---|
| **Nền tảng (Canvas)** | `Theme.Primitives.Alabaster50` | `#FAF8F5` | `bg-alabaster-50` / `surface-warm` | Nền thẻ phụ, thẻ KPI |
| | `Theme.Primitives.Alabaster100` | `#F4F1EA` | `bg-alabaster-100` / `background` | Màu nền chính toàn ứng dụng |
| | `Theme.Primitives.Alabaster200` | `#E9E4DA` | `bg-alabaster-200` / `border-subtle` | Đường kẻ phân tách, rãnh tab |
| **Thanh điều hướng** | `Theme.Primitives.Espresso900` | `#161412` | `bg-espresso-900` | Thanh Rail dọc, Header bóng tối |
| | `Theme.Primitives.Espresso800` | `#24211E` | `bg-espresso-800` | Thẻ tối trên Rail, hover menu |
| **Màu thương hiệu** | `Theme.Primitives.Terracotta600` | `#C04E26` | `brand-terracotta` / `primary` | Màu nhấn chủ đạo (Leica Bronze) |
| | `Theme.Primitives.Terracotta700` | `#A13D1B` | `brand-terracotta-dark` | Trạng thái hover nút chính |
| | `Theme.Primitives.Terracotta50` | `#FDF4F0` | `brand-terracotta-light` | Nền badge được chọn |
| **Màu trạng thái** | `Theme.Primitives.Sage600` | `#2B7250` | `status-sage` / `success` | Thành công, đã cọc, đã khớp |
| | `Theme.Primitives.Ochre600` | `#B86E0E` | `status-ochre` / `warning` | Chờ duyệt, chờ nhân viên đối soát |
| | `Theme.Primitives.Crimson600` | `#C42B2B` | `status-crimson` / `danger` | Đã hủy, giao dịch lỗi |
| | `Theme.Primitives.Teal600` | `#167280` | `accent-teal` | Điểm nhấn phân hệ thanh toán VietQR, đối soát studio |
| **Mực chữ (Ink)** | `Theme.Primitives.Espresso900` | `#161412` | `text-espresso-900` / `text-main` | Chữ tiêu đề đậm nét |
| | `Theme.Primitives.Umber700` | `#4A443D` | `text-umber-700` / `text-secondary`| Nhãn trường, nội dung phụ |
| | `Theme.Primitives.Stone500` | `#7C7469` | `text-stone-500` / `text-muted` | Ghi chú, placeholder |

### Quy tắc Typography & Components:
- **Font gia đình:** WinForms sử dụng `Segoe UI` (Sans-serif hiện đại) kết hợp `Consolas` cho mã số kỹ thuật. Web Portal sẽ sử dụng font serif cổ điển sang trọng (Cormorant Garamond / Playfair Display) cho tiêu đề biên tập và Inter cho giao diện số liệu.
- **Bo góc (Radius):** Thống nhất chuẩn 8px - 10px cho các thẻ bảng điều khiển (`CardPanel`) và nút nhấn.
- **Độ tương phản (Contrast):** Đạt chuẩn WCAG 2.1 AA tối thiểu 4.5:1 cho toàn bộ nhãn chữ tiếng Việt.

---

## 9. SẴN SÀNG CHO GIAI ĐOẠN 02 (PHASE 02 READINESS)

Hạ tầng Phase 01 đã thiết lập đầy đủ điều kiện để chuyển giao sang Phase 02:
1. Cấu trúc Desktop WinForms đã mở rộng ổn định, 3 tab hoạt động mượt mà, không phá vỡ logic v2.1.
2. Ranh giới an toàn dữ liệu được thiết lập chặt chẽ, không ảnh hưởng đến cơ sở dữ liệu hiện có.
3. Hợp đồng dữ liệu `IPortalDataProvider` và hệ thống token thiết kế đã sẵn sàng làm chuẩn mực để khởi tạo Customer Portal Shell trong Phase 02 khi được phê duyệt.
4. Phạm vi thanh toán chuẩn hóa theo VietQR và đối soát nhân viên/thủ công/tiền mặt, không còn ngụ ý SePay hay cơ chế tự động hóa ngoài luồng.
