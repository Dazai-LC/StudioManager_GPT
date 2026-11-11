# STUDIOMANAGER v2.2 — PHASE 02: CUSTOMER PORTAL SHELL, PUBLIC PAGES & AUTHENTICATION UX
## TECHNICAL REPORT & ARCHITECTURAL SUMMARY

**Mã giai đoạn:** Phase 02 — Customer Portal Implementation  
**Workspace:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Frontend`  
**Git Branch:** `feature/v2.2-frontend`  
**Baseline Commit:** `ea7f7d8`  
**Thương hiệu áp dụng:** Atelier Lumière (Haute Photographie & Art Studio)  
**Trạng thái:** HOÀN THÀNH — READY FOR REVIEW  

---

## 1. TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ

Phase 02 đã hoàn tất việc khởi tạo và phát triển ứng dụng **Customer Portal** độc lập tại:
`src/StudioManager.CustomerPortal/`

### 1.1. Công nghệ sử dụng
- **Framework:** Next.js 14 (App Router)
- **Ngôn ngữ:** TypeScript 5 (Strict Mode, 100% typed)
- **Styling:** Tailwind CSS 3.4 tuân thủ bảng màu độc quyền Atelier Lumière:
  - `Alabaster / Ivory`: `#FAF8F5`, `#F4F1EA`, `#E9E4DA`
  - `Espresso / Dark`: `#161412`, `#24211E`
  - `Terracotta (Accent)`: `#C04E26`, `#D16038`, `#FDF4F0`
  - `Sage (Em bé/Pastel)`: `#2B7250`, `#EEF7F2`
  - `Ochre (Cảnh báo/Ấm)`: `#B86E0E`, `#FEF9EB`
- **Iconography:** `lucide-react`
- **Kiến trúc dữ liệu:** `Presentation Layer → IPortalDataProvider → MockPortalDataProvider`

### 1.2. Tính độc lập & An toàn hệ thống
- **Zero SQL Server Dependency:** Ứng dụng chạy hoàn toàn local, không kết nối cơ sở dữ liệu vật lý hay backend API, an toàn tuyệt đối cho Codex phát triển sau này.
- **Zero SMTP / OAuth Dependency:** Mô phỏng quy trình gửi email khôi phục mật khẩu và đăng nhập mà không kích hoạt dịch vụ bên thứ ba.
- **Không sửa đổi Core Solution:** Không sửa bất kỳ file nào trong `Domain`, `Application`, `Infrastructure`, `database/`, hay `StudioManager.WinForms`.

---

## 2. DANH MỤC CÁC MÀN HÌNH ĐÃ TRIỂN KHAI

### 2.1. Nhóm màn hình Khám phá Công khai (Public Pages)
| Mã màn hình | Route | Tên màn hình | Nội dung & Điểm nhấn UX |
|---|---|---|---|
| `CP-PUB-01` | `/` | Trang chủ (Home) | Hero không gian ánh sáng điện ảnh, khẩu hiệu "Ánh sáng kể câu chuyện của bạn", 3 gói chụp tiêu biểu (`GC000001, GC000002, GC000004`), 3 phòng chụp (`P0001, P0002, P0003`), cam kết thương hiệu Atelier Lumière. |
| `CP-PUB-02` | `/packages` | Khám phá gói chụp | Danh mục đầy đủ 5 gói seed (`GC000001..05`), bộ lọc danh mục (Chân dung, Gia đình, Em bé, Cặp đôi, Kỷ yếu), tìm kiếm theo từ khóa/mã gói, sắp xếp theo giá, thẻ tỷ lệ đặt cọc (30% / 50%). |
| `CP-PUB-03` | `/packages/[code]` | Chi tiết gói chụp | Hero ảnh gói chụp (`AI-AST-01..05`), bảng quyền lợi chi tiết (inclusions & highlights), gợi ý phòng chụp phù hợp, danh sách dịch vụ gia tăng (`DV000001..08`), nút "Đặt lịch gói chụp này" mở modal preview Phase 03. |
| `CP-PUB-04` | `/studio-showcase` | Không gian & Thiết bị | Tabbed interface trình diễn: (1) 03 phòng chụp tiêu chuẩn studio, (2) 08 dịch vụ gia tăng chính thức, (3) 10 trang thiết bị máy ảnh/đèn flash và phục trang cao cấp. |
| `CP-PUB-05` | `/about` | Về Atelier Lumière | Triết lý nghệ thuật, tiêu chuẩn âm thanh - ánh sáng, 3 giá trị cốt lõi (Ánh sáng điện ảnh, Cảm xúc chân thật, Lưu trữ vĩnh viễn trên StudioManager). |

### 2.2. Nhóm màn hình Xác thực & Bảo mật (Authentication UX)
| Mã màn hình | Route | Tên màn hình | Nguyên tắc kiến trúc & Điểm nhấn UX |
|---|---|---|---|
| `CP-AUTH-01` | `/register` | Đăng ký thành viên | Split layout với khung hình `BG-AUTH-03`, thanh đo độ mạnh mật khẩu (`PasswordStrengthMeter`), kiểm tra trùng tài khoản portal và cảnh báo xung đột hồ sơ tại quầy theo **DEC-03**. |
| `CP-AUTH-02` | `/login` | Đăng nhập Portal | Split layout với khung hình `BG-AUTH-02`, hỗ trợ Email/SĐT, ghi nhớ đăng nhập, thanh công cụ chọn nhanh 3 tài khoản mẫu (Đã liên kết, Khách mới, Chờ duyệt). |
| `CP-AUTH-03` | `/recovery` | Khôi phục mật khẩu | Khung hình `BG-AUTH-04`, mô phỏng tiếp nhận yêu cầu reset password, thông báo minh bạch chế độ Frontend Demo không phát tán email thật. |
| `CP-AUTH-04` | `/verify-identity` | Định danh khách hàng (DEC-03) | Giao diện liên kết hồ sơ khách hàng tại quầy với tài khoản portal. Cung cấp bộ nút chuyển đổi 4 trạng thái mô phỏng (`VERIFIED`, `PENDING_VERIFICATION`, `REJECTED`, `UNLINKED`) phục vụ nghiệm thu. |
| `CP-ERR-01` | `/access-denied` | Quyền truy cập giới hạn | Trang 403 mang phong cách thanh lịch với chỉ dẫn đăng nhập hoặc quay lại trang chủ. |

---

## 3. CÁC THÀNH PHẦN SHELL & REUSABLE COMPONENTS

1. **`DemoModeBanner.tsx`**: Thanh thông báo cố định trên cùng, hiển thị rõ chế độ chạy thử nghiệm frontend, trạng thái đăng nhập hiện tại và nút thu nhỏ/đóng thông báo.
2. **`PortalHeader.tsx`**: Header thương hiệu với biểu tượng ống kính khẩu độ (Aperture), menu điều hướng mượt mà, huy hiệu trạng thái định danh DEC-03, nút Đặt lịch và mobile navigation drawer.
3. **`MobileBottomNav.tsx`**: Thanh điều hướng di động cố định dưới cùng chuẩn 5 điểm đến: *Trang chủ*, *Gói chụp*, *Không gian*, *Đặt lịch*, *Tài khoản*.
4. **`Footer.tsx`**: Chân trang đậm chất tạp chí với thông tin studio (128 Phố Huế), giờ mở cửa, liên kết nhanh và tuyên ngôn bản quyền StudioManager v2.2.
5. **`PortalAuthLayout.tsx`**: Khung layout hai cột cho nhóm màn hình đăng nhập/đăng ký, tích hợp vùng hiển thị hình ảnh thương hiệu `BG-AUTH-01..04`.
6. **`BookingPreviewModal.tsx`**: Hộp thoại thông báo minh bạch giải thích luồng đặt lịch trực tuyến đầy đủ sẽ chính thức hoạt động trong **Phase 03**.
7. **`VisualPlaceholder.tsx`**: Thành phần hiển thị hình ảnh nghệ thuật thông minh với gradient Atelier, hiệu ứng ánh sáng ambient và thẻ định danh tài nguyên `AI-AST-01..18` / `BG-AUTH-01..04`.
8. **`PasswordStrengthMeter.tsx`**: Đo lường trực quan 4 cấp độ an toàn mật khẩu (độ dài, ký tự hoa, số, ký tự đặc biệt).

---

## 4. DỮ LIỆU ĐỒNG BỘ CHUẨN TỪ `03_SeedData.sql`

Toàn bộ dữ liệu trong `src/lib/mock-provider.ts` được ánh xạ chính xác 100% từ database seed:
- **5 Gói chụp (`GoiChup`):**
  - `GC000001`: Chân dung cá nhân (1.500.000 đ, 120 phút, 10 ảnh chỉnh sửa)
  - `GC000002`: Gia đình gắn kết (2.800.000 đ, 180 phút, 20 ảnh chỉnh sửa)
  - `GC000003`: Thiên thần nhỏ - Em bé (2.200.000 đ, 150 phút, 15 ảnh chỉnh sửa)
  - `GC000004`: Cặp đôi & Kỷ niệm (2.500.000 đ, 180 phút, 15 ảnh chỉnh sửa)
  - `GC000005`: Kỷ yếu & Nhóm bạn (6.500.000 đ, 300 phút, 30 ảnh chỉnh sửa)
- **3 Phòng chụp (`PhongChup`):** `P0001` (Phòng Ánh Sáng), `P0002` (Phòng Cổ Điển), `P0003` (Phòng Em Bé).
- **8 Dịch vụ (`DichVu`):** `DV000001` (Trang điểm) đến `DV000008` (Giao ảnh hỏa tốc 24h).
- **10 Tài nguyên (`TaiNguyen`):** `TN000001` (Canon R6 Mark II) đến `TN000010` (Đạo cụ sinh nhật).
- **5 Khách hàng tại quầy tham chiếu (`KhachHang`):** `KH000001` (Nguyễn Văn An) đến `KH000005` (Hoàng Minh Đức).

---

## 5. KẾT QUẢ KIỂM THỬ THỰC TẾ

| Hạng mục kiểm thử | Lệnh thực thi | Kết quả | Ghi chú |
|---|---|---|---|
| **Next.js Production Build** | `npm run build` | **PASS (Code 0)** | 12/12 static/dynamic routes prerendered sạch sẽ |
| **Next.js ESLint** | `npm run lint` | **PASS (Code 0)** | 0 warnings, 0 errors |
| **C# Solution Debug Build** | `dotnet build StudioManager.sln -c Debug -m:1 -nr:false` | **PASS (Code 0)** | 0 warnings, 0 errors (00:00:33) |
| **C# Solution Release Build** | `dotnet build StudioManager.sln -c Release -m:1 -nr:false` | **PASS (Code 0)** | 0 warnings, 0 errors (00:01:21) |
| **C# Solution Unit Tests** | `dotnet test tests/StudioManager.Tests/...` | **PASS (Code 0)** | **29/29 tests PASSED** (29 ms) |
| **Git Working Tree Verification** | `git status` | **PASS** | Không có thay đổi ngoài `src/StudioManager.CustomerPortal/` |

