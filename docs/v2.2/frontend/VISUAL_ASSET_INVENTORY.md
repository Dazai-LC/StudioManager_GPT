# STUDIOMANAGER v2.2 — KIỂM KÊ HỆ THỐNG HÌNH ẢNH OFFLINE (IMG-02)
## STUDIO MANAGER DESKTOP WINFORMS (ATELIER LUMIÈRE)

**Tài liệu kiểm kê thực tế:** Đóng băng sau quá trình sản xuất & đóng gói vào `src\StudioManager.WinForms\Assets\`  
**Tài liệu tham chiếu:** `IMG_02_VISUAL_ASSET_SYSTEM.md` (`DEC-05` + mở rộng nền xác thực `IMG-02`)  
**Cấu hình build:** `StudioManager.WinForms.csproj` sao chép toàn bộ `Assets\**\*` với `CopyToOutputDirectory="PreserveNewest"` (`Offline-First`, 0% phụ thuộc Internet khi trình diễn).

---

## 1. TỔNG HỢP MINH BẠCH NGUỒN ẢNH THỰC TẾ (25 TỆP JPEG TRÊN ĐĨA)

Tất cả **23 tệp ảnh nền & danh mục AI** (gồm **2 nền xác thực**, **18/18 thực thể Seed chuẩn**, và **3 thực thể thật phát hiện thêm trong SQL Server đang chạy** gồm `P5A94A4`, `TNA1DC12`, `TN501515`) đều là ảnh biên tập độ phân giải cao lưu cục bộ (`Offline-First`, 0% phụ thuộc Internet); **2 tệp hệ thống (`MediaDefaults/fallbacks/`)** đóng vai trò ảnh thay thế minh bạch cho các mã test/chưa gắn ảnh (`GC9CD624`, `TNDA9E3F`, mã mới).

| Nhóm | Số lượng tổng | Ảnh AI chân thực (`LocalAiDefault`) | Ảnh đồ họa dự phòng (`SystemFallback`) |
|---|---:|---:|---:|
| Nền Xác thực (`Backgrounds/`) | 2 | 2 | 0 |
| Gói chụp (`MediaDefaults/packages/`) | 5 | 5 | 0 |
| Phòng chụp (`MediaDefaults/rooms/`) — gồm Seed `P0001..03` + SQL `P5A94A4` | 4 | 4 | 0 |
| Thiết bị & Trang phục (`MediaDefaults/resources/`) — gồm Seed `TN000001..10` + SQL `TNA1DC12`, `TN501515` | 12 | 12 | 0 |
| Ảnh mặc định dự phòng (`MediaDefaults/fallbacks/`) | 2 | 0 | 2 |
| **Tổng cộng** | **25** | **23** | **2** |

> **Phân biệt Seed vs Live SQL:**
> - **Seed chuẩn (`database/03_SeedData.sql`, 18 thực thể danh mục có ảnh):** **18/18 đạt ảnh AI riêng (100%)**.
> - **Bản ghi bổ sung ngoài Seed trên SQL Server thực tế (`localhost\StudioManager`):** **3/5 thực thể có tên nghĩa rõ ràng** (`P5A94A4 — Phòng 4D`, `TNA1DC12 — Đèn trung thu`, `TN501515 — Micro`) đã được tạo ảnh AI riêng; **2 bản ghi kiểm thử mơ hồ** (`GC9CD624 — "test"`, `TNDA9E3F — "Đèn test"`) chủ ý giữ nguyên `SystemFallback` (`"Hình thay thế"`, không gắn ảnh giả mạo). Tổng cộng **21/23 thực thể danh mục Seed + Live SQL dùng ảnh AI (91,3%)** và **2/23 là Fallback trung thực**.

---

## 2. BẢNG KIỂM KÊ CHI TIẾT 25 TỆP HÌNH ẢNH TRÊN ĐĨA

| STT | Mã định danh / Thực thể | Tệp tin tương đối từ `src\StudioManager.WinForms\Assets\` | Kích thước (bytes) | Độ phân giải | Phân loại nguồn thực tế | Ghi chú trình diễn UI |
|---:|---|---|---:|---|---|---|
| 1 | `BG-AUTH-01` (Desktop Staff/Admin Login) | `Backgrounds\bg_auth_desktop_login.jpg` | 612,465 | 1376×768 | Ảnh AI chân thực | Nền toàn màn hình `LoginForm` qua `FullCanvasAuthSurface` |
| 2 | `BG-AUTH-03` (Customer Auth Surface) | `Backgrounds\bg_auth_portal_register.jpg` | 779,227 | 1376×768 | Ảnh AI chân thực | Nền toàn màn hình `CustomerLoginForm` & `CustomerRegistrationForm` |
| 3 | `GC000001` — Chân dung cá nhân | `MediaDefaults\packages\pkg_GC000001_cover_ai.jpg` | 709,283 | 1200×896 | Ảnh AI chân thực | Gói chụp, Bookings Inspector, Dashboard |
| 4 | `GC000002` — Gia đình | `MediaDefaults\packages\pkg_GC000002_cover_ai.jpg` | 821,568 | 1200×896 | Ảnh AI chân thực | Gói chụp, Wizard đặt lịch |
| 5 | `GC000003` — Em bé | `MediaDefaults\packages\pkg_GC000003_cover_ai.jpg` | 762,547 | 1200×896 | Ảnh AI chân thực | Gói chụp & Album mẫu Demo |
| 6 | `GC000004` — Cặp đôi | `MediaDefaults\packages\pkg_GC000004_cover_ai.jpg` | 854,639 | 1264×848 | Ảnh AI chân thực | Gói chụp & Wizard đặt lịch |
| 7 | `GC000005` — Kỷ yếu | `MediaDefaults\packages\pkg_GC000005_cover_ai.jpg` | 977,327 | 1264×848 | Ảnh AI chân thực | Gói chụp & Bảng so sánh gói |
| 8 | `P0001` — Phòng Ánh Sáng | `MediaDefaults\rooms\room_P0001_cover_ai.jpg` | 753,119 | 1264×848 | Ảnh AI chân thực | Phản ánh trong `CustomerSpacesResourcesPage` & `CrudPage(PhongChup)` |
| 9 | `P0002` — Phòng Cổ Điển | `MediaDefaults\rooms\room_P0002_cover_ai.jpg` | 866,911 | 1264×848 | Ảnh AI chân thực | Phản ánh trong `CustomerSpacesResourcesPage` & `CrudPage(PhongChup)` |
| 10 | `P0003` — Phòng Em Bé | `MediaDefaults\rooms\room_P0003_cover_ai.jpg` | 759,354 | 1264×848 | Ảnh AI chân thực | Phản ánh trong `CustomerSpacesResourcesPage` & `CrudPage(PhongChup)` |
| 11 | `P5A94A4` — Phòng 4D *(Live SQL)* | `MediaDefaults\rooms\room_P5A94A4_cover_ai.jpg` | 671,387 | 1264×848 | Ảnh AI chân thực | Tự động khớp qua `TryLoadLiveRoomsReadOnlyAsync` & `CrudPage(PhongChup)` |
| 12 | `TN000001` — Canon R6 Mark II | `MediaDefaults\resources\res_TN000001_cover_ai.jpg` | 652,399 | 1200×896 | Ảnh AI chân thực | Danh mục Thiết bị (`TaiNguyen`) |
| 13 | `TN000002` — Sony A7 IV | `MediaDefaults\resources\res_TN000002_cover_ai.jpg` | 689,403 | 1200×896 | Ảnh AI chân thực | Danh mục Thiết bị (`TaiNguyen`) |
| 14 | `TN000003` — Đèn Godox AD600 | `MediaDefaults\resources\res_TN000003_cover_ai.jpg` | 699,375 | 1200×896 | Ảnh AI chân thực | Danh mục Thiết bị (`TaiNguyen`) |
| 15 | `TN000004` — Đèn LED RGB | `MediaDefaults\resources\res_TN000004_cover_ai.jpg` | 677,818 | 1264×848 | Ảnh AI chân thực | Danh mục Thiết bị (`TaiNguyen`) |
| 16 | `TN000005` — Phông giấy trắng | `MediaDefaults\resources\res_TN000005_cover_ai.jpg` | 694,730 | 1264×848 | Ảnh AI chân thực | Danh mục Thiết bị (`TaiNguyen`) |
| 17 | `TN000006` — Váy cưới trắng | `MediaDefaults\resources\res_TN000006_cover_ai.jpg` | 717,705 | 1200×896 | Ảnh AI chân thực | Trang phục kèm `DV000002` |
| 18 | `TN000007` — Áo dài đỏ | `MediaDefaults\resources\res_TN000007_cover_ai.jpg` | 685,028 | 1200×896 | Ảnh AI chân thực | Trang phục kèm `DV000002` |
| 19 | `TN000008` — Vest nam | `MediaDefaults\resources\res_TN000008_cover_ai.jpg` | 690,360 | 1200×896 | Ảnh AI chân thực | Trang phục kèm `DV000002` |
| 20 | `TN000009` — Váy công chúa bé | `MediaDefaults\resources\res_TN000009_cover_ai.jpg` | 761,618 | 1200×896 | Ảnh AI chân thực | Trang phục kèm `DV000002` & Album Demo |
| 21 | `TN000010` — Đạo cụ sinh nhật | `MediaDefaults\resources\res_TN000010_cover_ai.jpg` | 813,189 | 1264×848 | Ảnh AI chân thực | Đạo cụ (`TaiNguyen`) |
| 22 | `TNA1DC12` — Đèn trung thu *(Live SQL)* | `MediaDefaults\resources\res_TNA1DC12_cover_ai.jpg` | 770,479 | 1264×848 | Ảnh AI chân thực | Đạo cụ (`TaiNguyen` Live SQL) |
| 23 | `TN501515` — Micro *(Live SQL)* | `MediaDefaults\resources\res_TN501515_cover_ai.jpg` | 661,856 | 1264×848 | Ảnh AI chân thực | Thiết bị thu âm (`TaiNguyen` Live SQL) |
| 24 | `DEF-CATALOG` | `MediaDefaults\fallbacks\fallback_catalog_cover.jpg` | 68,766 | 1200×800 | Đồ họa GDI+ dự phòng | Ảnh dự phòng chữ nhật ngang cho `GC9CD624`, `TNDA9E3F` hoặc mã mới |
| 25 | `DEF-SQUARE` | `MediaDefaults\fallbacks\fallback_atelier_square.jpg` | 50,987 | 800×800 | Đồ họa GDI+ dự phòng | Ảnh dự phòng tỉ lệ 1:1 cho thẻ vuông |

---

## 3. KIẾN TRÚC HIỂN THỊ & QUẢN LÝ ẢNH TRÊN WINFORMS

- [`VisualAssetService`](../../src/StudioManager.WinForms/Simulation/VisualAssetService.cs): Nạp bất đồng bộ/bộ nhớ đệm thu nhỏ theo chiều rộng tối đa (`GetOrLoadImage`), nạp gốc không khóa tệp qua `CreateDetachedFullResolutionBitmap` cho trình xem lớn, lưu danh sách ảnh (Demo Sandbox) chỉ trong phiên làm việc và hỗ trợ `ResetToBaseline()`.
- [`VisualMediaPictureBox`](../../src/StudioManager.WinForms/Controls/PhotoMediaControls.cs): Bo tròn góc ảnh chuẩn `Cover` framing không bóp méo tỉ lệ, phủ lớp mờ tương tác + nút `"Xem ảnh lớn"` khi di chuột, nhãn nguồn ảnh minh bạch và nhấp chuột mở [`ImageViewerForm`](../../src/StudioManager.WinForms/Controls/ImageViewerForm.cs).
- [`ImageViewerForm`](../../src/StudioManager.WinForms/Controls/ImageViewerForm.cs): Hỗ trợ `Fit` / `100%` / `+` `-` / cuộn chuột thu phóng mượt / kéo thả dịch ảnh / dải thu nhỏ bên dưới / phím tắt `Esc`, `Left`, `Right` và bảng metadata kỹ thuật.
- [`AdminMediaManagerDialog`](../../src/StudioManager.WinForms/Controls/PhotoMediaControls.cs): Quản trị viên (`VaiTro.QuanTriVien`) có toàn quyền tải ảnh từ máy tính lên Demo, đặt ảnh bìa (`IsPrimaryCover`), đảo thứ tự, sửa tiêu đề/chú thích/alt text, khôi phục mặc định; Nhân viên (`VaiTro.NhanVien`) mở ở chế độ **Chỉ đọc**.

