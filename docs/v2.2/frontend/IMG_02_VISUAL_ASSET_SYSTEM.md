# STUDIOMANAGER v2.2
## IMG-02 — Visual Asset System & Authentication Background Pack

**Dự án:** StudioManager / Project Phoenix / Atelier Lumière  
**Phiên bản tài liệu:** Draft 1.0 — Đề xuất phạm vi nghiệp vụ và UX/UI  
**Workspace triển khai:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Frontend`  
**Branch:** `feature/v2.2-frontend`  
**Tài liệu nền:** Final Locked UX Blueprint Revision 1  
**Trạng thái:** Chưa triển khai, chưa phê duyệt sản xuất bộ background bổ sung.  

---

## 1. Mục tiêu chiến lược

Xây dựng hệ thống hình ảnh thống nhất trên hai bề mặt sản phẩm:

- **StudioManager Desktop WinForms:** Ứng dụng quản lý nội bộ dành cho nhân viên và Admin.
- **Customer Portal:** Website Next.js dành cho khách hàng.

Hình ảnh phải phục vụ ba mục tiêu:

1. **Nghiệp vụ:** Giúp nhận diện nhanh gói chụp, phòng chụp, thiết bị, trang phục và đạo cụ.
2. **Trải nghiệm:** Tạo giao diện chuyên nghiệp, giàu tính thẩm mỹ và dễ sử dụng.
3. **Nhận diện thương hiệu:** Duy trì phong cách Atelier Lumière — Warm Editorial, Premium Photography và Cinematic Minimalism.

Hệ thống phải hoạt động được khi trình diễn local. WinForms không phụ thuộc Internet để hiển thị ảnh mặc định.

## 2. Ranh giới phạm vi

### 2.1. Phạm vi đã duyệt

Giữ nguyên `DEC-05`, gồm chính xác:

| Nhóm | Số lượng | Dữ liệu |
|---|---:|---|
| Gói chụp | 5 | `GoiChup` |
| Phòng chụp | 3 | `PhongChup` |
| Thiết bị, trang phục, đạo cụ | 10 | `TaiNguyen` |
| Ảnh mặc định dự phòng | 2 | `FALLBACK` |
| **Tổng** | **20** | `AI-AST-01..20` |

Không tự tạo thêm ảnh cho các danh mục không tồn tại trong database seed.

### 2.2. Phạm vi mở rộng đề xuất — IMG-02

Bổ sung bốn ảnh background gốc:

| ID | Vị trí | Chủ đề |
|---|---|---|
| `BG-AUTH-01` | Desktop Login | Camera trên bàn làm việc nghệ thuật |
| `BG-AUTH-02` | Portal Login | Máy ảnh trong studio ánh sáng điện ảnh |
| `BG-AUTH-03` | Portal Register | Máy ảnh, hoa và không gian sáng ấm |
| `BG-AUTH-04` | Portal Verify Email / Recovery | Bàn làm việc ảnh in, ánh sáng tinh tế |

Bộ background là đề xuất bổ sung, **không được coi là đã phê duyệt sinh ảnh** cho đến khi chủ dự án xác nhận.

Các ảnh minh họa phụ cho empty states, dashboard hoặc gallery bổ sung sẽ được đánh giá sau, không tự mở rộng số lượng.

## 3. Kiểm kê danh mục cần ảnh hóa

### 3.1. Gói chụp

| Mã | Tên | Concept ảnh |
|---|---|---|
| `GC000001` | Chân dung cá nhân | Studio chân dung tối giản, phông kem, key light |
| `GC000002` | Gia đình | Phòng chụp gia đình ấm áp, sofa linen, ánh sáng tự nhiên |
| `GC000003` | Em bé | Không gian an toàn, giỏ mây, chăn len, tông pastel |
| `GC000004` | Cặp đôi | Editorial lãng mạn, rèm voan và ánh nắng chiều |
| `GC000005` | Kỷ yếu | Kiến trúc cổ điển, nắng vàng và concept thanh xuân |

**Quy tắc:** Ảnh mô tả phong cách và bối cảnh, không được khiến khách tưởng đó là ảnh chụp thật từ portfolio studio.

### 3.2. Phòng chụp

| Mã | Tên | Concept ảnh |
|---|---|---|
| `P0001` | Phòng Ánh Sáng | Daylight studio, cửa sổ vòm, sàn gỗ sáng |
| `P0002` | Phòng Cổ Điển | Nội thất vintage, tường phào chỉ, sắc nâu Espresso |
| `P0003` | Phòng Em Bé | Không gian trẻ em dịu nhẹ, đạo cụ gỗ, thảm kem |

**Quy tắc:** Tất cả ảnh phòng chụp do AI tạo phải có nhãn minh họa; không được trình bày như ảnh hiện trạng cơ sở vật chất.

### 3.3. Thiết bị, trang phục và đạo cụ

| Mã | Tài nguyên | Concept minh họa |
|---|---|---|
| `TN000001` | Canon R6 Mark II | Máy ảnh mirrorless chuyên nghiệp trên bệ đá kem |
| `TN000002` | Sony A7 IV | Máy ảnh full-frame đặt trên bàn gỗ sẫm |
| `TN000003` | Đèn Godox AD600 | Flash studio và softbox |
| `TN000004` | Đèn LED RGB | Đèn LED studio với chân đỡ |
| `TN000005` | Phông giấy trắng | Cuộn phông trắng và hệ thống treo |
| `TN000006` | Váy cưới trắng | Váy cưới satin trên mannequin |
| `TN000007` | Áo dài đỏ | Áo dài đỏ trưng bày trong studio |
| `TN000008` | Vest nam | Vest nam trên mannequin |
| `TN000009` | Váy công chúa bé | Váy voan trẻ em tông kem hồng |
| `TN000010` | Đạo cụ sinh nhật | Bánh giả, bóng bay, chữ số và trang trí |

**Quy tắc đặc biệt với thiết bị có tên thương mại:** Hình minh họa có thể thể hiện loại sản phẩm tương ứng, nhưng không tự khẳng định chính xác model, thông số, phụ kiện hoặc tình trạng vật lý nếu ảnh AI không xác thực được những chi tiết đó.

**Quy tắc kiểm kê:** Nếu xuất hiện bản ghi `TaiNguyen` mới trong tương lai, dùng ảnh fallback phù hợp trước. Không tự động tạo ảnh AI mỗi lần Admin tạo một tài nguyên mới.

### 3.4. Danh mục dịch vụ bổ sung

Blueprint có tám dịch vụ bổ sung, nhưng chưa duyệt tám ảnh riêng tương ứng.

Đề xuất:

- Trong danh sách dịch vụ: dùng icon và typography theo loại dịch vụ.
- Nếu cần thẻ dịch vụ có ảnh: sử dụng hình minh họa ngữ cảnh phù hợp hoặc fallback, không gán nhầm ảnh của một thiết bị cụ thể.
- Một bộ ảnh dịch vụ riêng chỉ được xem xét sau khi kiểm kê đủ tên và mã dịch vụ thực tế.

---

## 4. Art Direction — Atelier Lumière

### 4.1. Ngôn ngữ hình ảnh thống nhất

**Phong cách chính:** Photorealistic Editorial Studio Photography.

Đặc điểm:

- Ánh sáng tự nhiên hoặc studio mềm mại.
- Tương phản vừa phải.
- Vật liệu có kết cấu thật: gỗ, linen, da, kim loại, thủy tinh.
- Bố cục ít vật thể, tập trung chủ thể.
- Chiều sâu hình ảnh rõ.
- Hậu cảnh không lộn xộn.
- Màu sắc điện ảnh nhưng không quá tối.

### 4.2. Bảng màu

| Vai trò | Màu |
|---|---|
| Ivory / Alabaster | `#FAF8F5` |
| Warm canvas | `#F4F1EA` |
| Espresso | `#161412` |
| Dark brown | `#24211E` |
| Terracotta | `#C04E26` |
| Neutral stone | `#7C7469` |

Không bắt mọi vật thể mang màu thương hiệu. Đây là bảng màu chi phối ánh sáng, hậu cảnh và cách phối cảnh tổng thể.

### 4.3. Tránh các lỗi ảnh AI thường gặp

- Máy ảnh có nhiều ống kính hoặc nút điều khiển dị dạng.
- Logo và chữ AI sinh bị méo.
- Thiết bị không đúng cấu trúc.
- Người mẫu có bàn tay hoặc khuôn mặt bất thường.
- Trang phục sai tỷ lệ.
- Studio quá xa hoa, không còn phù hợp với ngữ cảnh đồ án.
- Bối cảnh chứa watermark hoặc chữ giả.
- Ánh sáng quá mạnh khiến chữ giao diện khó đọc.

### 4.4. Tính minh bạch

Ảnh AI danh mục phải được ghi nhận nguồn là `AI_DEFAULT`.

Ảnh Admin tự tải lên có thể được ghi nhận là `ADMIN_UPLOAD`.

Ảnh hệ thống dự phòng ghi nhận là `SYSTEM_PLACEHOLDER`.

Việc phân loại ảnh chỉ mô tả nguồn hình ảnh, không thay thế kiểm tra nội dung và quyền sử dụng.

---

# 5. BG-AUTH — Background cho màn hình xác thực

## 5.1. BG-AUTH-01 — Desktop Login

**Mục tiêu:** Nâng cấp màn hình đăng nhập WinForms hiện có thành một trải nghiệm hình ảnh cao cấp nhưng vẫn giữ nguyên toàn bộ logic đăng nhập.

<textual concept>
Một chiếc máy ảnh mirrorless màu đen đặt trên bàn gỗ sẫm. Bên cạnh có ống kính, vài tấm ảnh in nghệ thuật và cuốn sổ ghi chú. Ánh sáng hoàng hôn chiếu xiên qua cửa sổ, tạo dải sáng vàng nhẹ trên thân máy. Không gian phía sau có chiều sâu studio, mềm và tối giản.
</textual concept>

**Bố cục đề xuất:**

- Màn hình rộng: chia thành hai vùng.
- Vùng hình ảnh chiếm khoảng 48–55% chiều ngang.
- Vùng form đăng nhập chiếm 45–52%.
- Form nằm trên nền Alabaster sạch, không đặt trực tiếp trên ảnh.
- Logo Atelier Lumière và khẩu hiệu ngắn có thể đặt trên ảnh nếu đủ tương phản.
- Ảnh cần có vùng trống an toàn dành cho nội dung chữ.

**Ưu tiên:** Giữ trải nghiệm nhập tài khoản/mật khẩu rõ ràng hơn việc phô diễn ảnh.

**Không thay đổi:** Xác thực, trạng thái tài khoản, đổi mật khẩu bắt buộc, thông báo lỗi và luồng mở MainForm hiện hành.

## 5.2. BG-AUTH-02 — Customer Portal Login

**Mục tiêu:** Tạo cảm giác khách đang bước vào một thương hiệu nhiếp ảnh, không phải biểu mẫu quản trị.

**Concept:**

Máy ảnh chuyên nghiệp đặt trên chân gỗ hoặc bàn studio, phía sau là set chụp editorial với phông vải kem, cửa sổ lớn và ánh sáng chiều tinh tế.

**Bố cục:**

- Desktop: split-screen, ảnh lớn ở một bên, form đăng nhập ở bên còn lại.
- Tablet: thu hẹp khối ảnh, ưu tiên không gian nhập liệu.
- Mobile: ảnh có thể chuyển thành banner ngang phía trên hoặc ẩn để tránh đẩy form xuống quá xa.
- Nút đăng nhập và liên kết đăng ký/khôi phục mật khẩu luôn dễ tìm.

Không được làm giảm tốc độ tải form chỉ vì hình nền quá lớn.

## 5.3. BG-AUTH-03 — Customer Portal Register

**Mục tiêu:** Tạo cảm giác chào đón, ấm áp, tinh tế.

**Concept:**

Một máy ảnh nhỏ phong cách chuyên nghiệp đặt cạnh bó hoa trắng, một số ảnh in và vải linen trong studio sáng. Ánh nắng ban mai tạo hiệu ứng mềm, cảm giác thân thiện và có chiều sâu.

**Khác với Login:**

- Sáng và nhẹ hơn.
- Ít bóng tối hơn.
- Không gian thoáng để truyền tải cảm giác khởi đầu.
- Ưu tiên tương phản đọc biểu mẫu.

**Form đăng ký dự kiến:**

- Họ tên.
- Email.
- Số điện thoại.
- Mật khẩu.
- Xác nhận mật khẩu.
- Đồng ý với điều khoản áp dụng.

Không yêu cầu ảnh đại diện ngay ở bước đăng ký.

## 5.4. BG-AUTH-04 — Verify Email / Password Recovery

**Mục tiêu:** Trạng thái xác minh nhẹ nhàng, không gây căng thẳng.

**Concept:**

Bàn làm việc sáng màu với máy ảnh, một tấm ảnh in, phong bì và ánh sáng studio dịu. Tối giản hơn ba ảnh trên.

**Các trạng thái dùng chung:**

- Đang chờ xác minh email.
- Email đã được xác minh.
- Liên kết hết hạn.
- Yêu cầu gửi lại email.
- Quên mật khẩu.
- Đang chờ nhân viên xác minh hồ sơ khách hàng cũ.

Ảnh là nền trang trí; trạng thái nghiệp vụ phải được diễn đạt rõ bằng chữ và biểu tượng UI.

Không sử dụng hình ảnh để ngụ ý email đã được gửi thật trong chế độ demo.

---

# 6. Thiết kế responsive và bố cục nền

## 6.1. Desktop WinForms

- Không dùng ảnh full-screen che phía sau các TextBox.
- Tách rõ vùng artwork và vùng form.
- Hỗ trợ màn hình 1366×768.
- Hỗ trợ DPI 100%, 125%, 150%.
- Có phương án co giãn hoặc ẩn ảnh ở cửa sổ nhỏ.
- Không làm ảnh hưởng sự kiện bấm nút, Enter, Escape, Tab order.
- Không làm mất phản hồi lỗi đăng nhập.
- Không phá luồng `ApplicationInitializationForm` sau đăng nhập.

Ảnh trang trí phải được nạp an toàn, có fallback khi mất tệp.

## 6.2. Customer Portal

**Mobile dưới 640px:**
- Form là nội dung chính.
- Background chỉ đóng vai trò hỗ trợ.
- Không dùng ảnh chiếm toàn bộ màn hình khiến người dùng phải cuộn mới thấy nút đăng ký.
- Form có chiều rộng phù hợp, input dễ bấm.

**Tablet 640–1023px:**
- Ảnh có thể hiển thị như một panel gọn hoặc banner.
- Form không bị bó chiều ngang.

**Desktop từ 1024px:**
- Split layout có hình ảnh lớn.
- Nội dung rõ và không chồng lên vùng quan trọng của ảnh.

**Yêu cầu WCAG:**
- Form có nhãn rõ.
- Focus state dễ thấy.
- Tương phản chữ và nền phù hợp.
- Không dùng hình nền để truyền tải thông tin bắt buộc.
- Hỗ trợ giảm chuyển động.
- Không tự phát video nền hoặc animation nặng.

---

# 7. UX quản lý ảnh cho Admin

## 7.1. Vị trí chức năng

Giữ nguyên 5 hub Desktop hiện có.

Trong **Danh mục**, tích hợp Media Manager vào:

- Gói chụp.
- Phòng chụp.
- Tài nguyên.

Không thêm một hub điều hướng chính chỉ để quản lý hình ảnh.

## 7.2. Màn hình danh sách

Mỗi bản ghi có:

- Thumbnail.
- Mã.
- Tên.
- Trạng thái nghiệp vụ.
- Loại nguồn ảnh.
- Số ảnh bổ sung nếu có gallery.

Nhấn vào thumbnail có thể mở ảnh lớn.

## 7.3. Màn hình Media Manager

**Khối thông tin:**
- Tên bản ghi.
- Mã bản ghi.
- Ảnh đang được chọn làm bìa.
- Nguồn ảnh.
- Chú thích và mô tả thay thế.

**Thao tác dành cho Admin:**
- Xem ảnh.
- Thêm ảnh.
- Chọn ảnh bìa.
- Thay ảnh bìa.
- Xóa ảnh tùy chỉnh.
- Khôi phục ảnh mặc định.
- Chỉnh sửa caption và alt text.
- Sắp xếp gallery nếu có nhiều ảnh.
- Lọc các bản ghi chưa được cập nhật ảnh thật.

**Thao tác dành cho nhân viên:**
- Xem thumbnail.
- Xem chi tiết.
- Mở lightbox khi được phân quyền.

Nhân viên không được ghi thay đổi ảnh danh mục.

## 7.4. Quy tắc upload

Đề xuất:

- Chấp nhận JPEG, PNG, WebP.
- Giới hạn file ban đầu: 5 MB.
- Kiểm tra kích thước pixel tối đa.
- Kiểm tra loại dữ liệu thực tế, không chỉ phần mở rộng.
- Tạo tên file nội bộ an toàn.
- Không ghi đè trực tiếp ảnh gốc đã đóng gói.
- Tạo thumbnail riêng khi cần.
- Có hình xem trước trước khi lưu.
- Thông báo rõ lỗi file không hợp lệ.

Trong Frontend-only, các thao tác này chỉ làm việc với vùng sandbox demo nếu được triển khai.

Việc lưu metadata chính thức, kiểm soát quyền tải lên và phục vụ ảnh an toàn thuộc giai đoạn Backend của Codex.

## 7.5. Đặt ảnh bìa

Mỗi thực thể có tối đa một ảnh bìa đang hoạt động tại một thời điểm.

Khi xóa ảnh bìa tùy chỉnh:

1. Nếu có ảnh tùy chỉnh khác, hệ thống có thể yêu cầu Admin chọn ảnh bìa thay thế.
2. Nếu không, quay về ảnh AI mặc định tương ứng.
3. Nếu không có ảnh AI riêng, dùng fallback phù hợp.

Không tự động xóa file ảnh AI mặc định khỏi bộ asset đóng gói.

## 7.6. Làm mới bằng AI

Đề xuất giao diện tương lai:

- Xem prompt gợi ý.
- Chọn phong cách.
- Tạo ảnh mới.
- Xem trước.
- Chấp nhận hoặc bỏ kết quả.
- Chọn làm ảnh bìa.

**Không triển khai tính năng gọi AI trực tiếp trong v2.2 hiện tại.**

Ở phiên bản demo trước mắt, quy trình tạo ảnh AI vẫn là một bước sản xuất asset được thực hiện riêng, sau đó tích hợp tệp đầu ra vào ứng dụng.

---

# 8. Hợp đồng dữ liệu hình ảnh

Đề xuất giao diện dữ liệu chung giữa WinForms và Portal:

| Thuộc tính | Ý nghĩa |
|---|---|
| `MediaId` | Định danh bản ghi media |
| `EntityType` | `GoiChup`, `PhongChup`, `TaiNguyen` |
| `EntityId` | Định danh bản ghi danh mục |
| `AssetKey` | Khóa ảnh ổn định |
| `SourceType` | `AI_DEFAULT`, `ADMIN_UPLOAD`, `SYSTEM_PLACEHOLDER` |
| `ImagePath` | Đường dẫn ảnh |
| `ThumbnailPath` | Đường dẫn thumbnail, nếu có |
| `AltText` | Văn bản mô tả |
| `Caption` | Chú thích hiển thị |
| `IsCover` | Có phải ảnh bìa đang chọn |
| `DisplayOrder` | Thứ tự trong gallery |
| `IsIllustration` | Cờ cảnh báo ảnh minh họa |
| `UploadedAt` | Thời gian tải lên |
| `UploadedBy` | Tài khoản thực hiện |

Đây là **đề xuất contract Frontend**, không phải yêu cầu tạo ngay những cột tương ứng trong SQL Server.

Codex sẽ quyết định thiết kế bảng, khóa, index và nơi lưu file khi đến giai đoạn Backend.

**Bổ sung contract cho background tĩnh:**

- `BackgroundAssetKey`
- `TargetScreen`
- `DesktopVariantPath`
- `WebVariantPath`
- `FocalPoint`
- `OverlayPreset`
- `FallbackBackgroundColor`

Background giao diện không nhất thiết phải được quản lý cùng một bảng dữ liệu với hình ảnh danh mục.

---

## 9. Cấu trúc lưu trữ tài nguyên

### 9.1. Thư mục đề xuất cho Frontend

```text
StudioManager-v2.2-Frontend/
│
├── assets-source/
│   └── visual-system/
│       ├── packages/
│       ├── rooms/
│       ├── resources/
│       ├── backgrounds/
│       └── fallbacks/
│
├── src/
│   ├── StudioManager.WinForms/
│   │   └── Assets/
│   │       ├── MediaDefaults/
│   │       └── Backgrounds/
│   │
│   └── StudioManager.CustomerPortal/
│       └── public/
│           └── media/
│               ├── packages/
│               ├── rooms/
│               ├── resources/
│               ├── backgrounds/
│               └── fallbacks/
│
└── docs/
    └── v2.2/
        └── frontend/
            └── visual-assets/
```

Đây là cấu trúc dự kiến. Không tự tạo ngay trong lúc đặc tả.

Ảnh nguồn chất lượng cao nên tách khỏi bản đã tối ưu cho runtime để tránh tăng dung lượng bản demo không cần thiết.

### 9.2. Tên file

Ví dụ:

- `pkg_GC000001_cover_ai.webp`
- `room_P0001_cover_ai.webp`
- `res_TN000001_cover_ai.webp`
- `fallback_atelier_square_ai.webp`
- `bg_auth_desktop_login.webp`
- `bg_auth_portal_login.webp`
- `bg_auth_portal_register.webp`
- `bg_auth_portal_verify.webp`

Tên file dựa trên ID thực thể, không dựa vào thứ tự hiển thị.

### 9.3. Định dạng

| Mục đích | Định dạng chính | Khuyến nghị |
|---|---|---|
| Web catalog | WebP | 800–1200px tùy khung |
| Web background | WebP | 1600–1920px chiều ngang |
| WinForms catalog | JPEG/PNG | Ảnh offline giải mã trực tiếp |
| WinForms login background | JPEG | 1600px trở lên |
| File làm nguồn | PNG/JPEG chất lượng cao | Lưu riêng khi cần tái xuất |

Không ép cùng một định dạng cho mọi ứng dụng nếu thư viện hiển thị của chúng khác nhau.

---

## 10. Quy trình sản xuất ảnh

Mỗi ảnh đi qua các trạng thái:

`PLANNED → GENERATED → REVIEWED → APPROVED → OPTIMIZED → INTEGRATED → VERIFIED`

### PLANNED
- Có ID.
- Có mã thực thể hoặc màn hình sử dụng.
- Có mô tả hình ảnh.
- Có tỷ lệ khung hình.
- Có tiêu chí chất lượng.

### GENERATED
- Ảnh AI đã được tạo.
- Chưa mặc định được duyệt.

### REVIEWED
- Kiểm tra vật thể, ánh sáng, bố cục.
- Kiểm tra lỗi AI.
- Kiểm tra khoảng trống cho giao diện.
- Kiểm tra tính nhất quán với danh mục.

### APPROVED
- Người dùng duyệt hình ảnh.
- Không tự thay bằng ảnh khác khi đã khóa.

### OPTIMIZED
- Xuất phiên bản WebP cho Portal.
- Xuất phiên bản tương thích WinForms.
- Tối ưu dung lượng.
- Gắn metadata và mapping.

### INTEGRATED
- Đặt vào đúng vị trí.
- Hiển thị ảnh đúng bản ghi.
- Có fallback.
- Có nhãn ảnh minh họa nếu cần.

### VERIFIED
- Hiển thị đúng trên Desktop.
- Hiển thị đúng trên Portal.
- Không bị crop sai chủ thể.
- Không có ảnh vỡ hoặc biến dạng.
- Không có file bị thiếu khi build hoặc đóng gói.

---

## 11. Phân quyền và an toàn

### 11.1. Public / Customer
- Được xem ảnh danh mục đã công bố.
- Không được tải lên ảnh quản lý danh mục.
- Không được thay ảnh bìa hệ thống.
- Chỉ xem dữ liệu hình ảnh được cho phép.

### 11.2. Staff
- Được xem ảnh phục vụ công việc.
- Không được thay ảnh danh mục nếu chưa được cấp quyền chính thức.
- Không được tự tạo đường dẫn media tùy ý vào hệ thống.

### 11.3. Admin
- Được quản lý ảnh danh mục.
- Được thay cover.
- Được xóa ảnh tùy chỉnh.
- Được xem nguồn ảnh và lịch sử thao tác khi Backend hỗ trợ.

**Lưu ý:** Kiểm tra quyền trên UI không thay thế Backend authorization.

### 11.4. Kiểm soát an toàn file

Backend tương lai phải bảo vệ:

- Quyền upload.
- Kích thước và loại file.
- Đường dẫn ngoài thư mục cho phép.
- Truy cập file riêng tư.
- Giới hạn dung lượng lưu trữ.
- Audit các thay đổi quan trọng.

Các ảnh chứng từ chuyển khoản nhạy cảm không được lưu chung với thư viện ảnh danh mục công khai.

---

## 12. Tổ chức triển khai theo roadmap v2.2

Không tạo một Phase mới làm thay đổi thứ tự tám Phase đã khóa.

Thay vào đó, IMG-02 được phân bổ như sau:

| Giai đoạn | Công việc IMG-02 |
|---|---|
| **Phase 01** | Hoàn tất và nghiệm thu hotfix bố cục hiện tại. Không đưa ảnh vào trước khi layout ổn định |
| **Phase 02** | Thiết kế vùng hình ảnh cho Login/Register/Verify Email và các thẻ danh mục trên Portal. Cho phép placeholder tạm |
| **Phase 03** | Tích hợp khung thumbnail gói chụp, phòng chụp vào Booking Request Wizard |
| **Phase 04** | Bảo đảm Desktop Booking Workspace có vị trí hiển thị ảnh gói/phòng mà không phá layout |
| **Phase 05** | Không phát triển ảnh nghiệp vụ mới nếu không cần. Ưu tiên giao diện tài chính sạch, dễ đọc |
| **Phase 06** | Sản xuất và tích hợp 20 ảnh đã duyệt; nếu BG-AUTH được duyệt, sản xuất thêm 4 ảnh background |
| **Phase 07** | Kiểm thử ảnh trên các màn hình quản trị phù hợp, bảo đảm quyền quản lý ảnh không bị mở rộng sai |
| **Phase 08** | Regression, offline test, responsive, accessibility, asset packaging và tài liệu bàn giao |

**Không để việc thiếu hình nền ngăn Phase 02 hoàn thành UX nền tảng.** Có thể dùng background màu thương hiệu và ảnh tạm trong quá trình thiết kế.

---

## 13. Tiêu chí nghiệm thu

### AC-IMG-01 — Đầy đủ danh mục
- 5 gói chụp có ảnh bìa riêng.
- 3 phòng chụp có ảnh bìa riêng.
- 10 tài nguyên có ảnh minh họa riêng.
- 2 fallback hoạt động đúng.
- Không gắn nhầm mã ảnh với bản ghi.

### AC-IMG-02 — Desktop
- Ảnh mặc định hiển thị offline.
- Không làm chậm đáng kể quá trình mở trang.
- Đổi tab không tạo lỗi hiển thị.
- Ảnh không bị kéo méo.
- Không gây hồi quy layout ở DPI cao.

### AC-IMG-03 — Portal
- Ảnh hiển thị responsive.
- Có loading hoặc fallback.
- Ảnh quan trọng ở đầu trang không bị tải chậm bất hợp lý.
- Không che CTA.
- Có alt text phù hợp.
- Các ảnh AI được đánh dấu là minh họa.

### AC-IMG-04 — Auth backgrounds
- Desktop Login vẫn đăng nhập như trước.
- Portal Login/Register dễ sử dụng ở 375px và 1280px.
- Không có input hoặc nút bị che.
- Background crop giữ chủ thể ở vùng quan trọng.
- Có phương án hiển thị khi thiếu ảnh.
- Form dễ đọc trong light mode và khi tăng tỷ lệ hiển thị.

### AC-IMG-05 — Admin Media Management
- Admin có thể chọn ảnh, xem trước, thay và xóa ảnh demo.
- Không sửa nhầm ảnh mặc định đã đóng gói.
- Reset về fallback hoạt động.
- Staff không có nút chỉnh sửa được phép hoạt động.
- Không tạo thay đổi SQL Server khi thao tác trong mock mode.

### AC-IMG-06 — Nguồn dữ liệu
- Ảnh mặc định khớp chính xác mã seed.
- Ảnh Admin tải lên không bị nhận nhầm là ảnh AI mặc định.
- Không dùng đường dẫn tuyệt đối phụ thuộc một máy.
- Không dùng dữ liệu hình ảnh nhạy cảm làm asset public.

### AC-IMG-07 — Chất lượng kỹ thuật
- Debug/Release build đạt yêu cầu.
- Unit tests hiện có không hồi quy.
- Không có file bị thiếu trong output.
- Không có ảnh bị biến dạng.
- Không có memory leak do nạp lại ảnh liên tục trong WinForms.
- Đóng gói ứng dụng Desktop sử dụng được khi không có Internet.

---

## 14. Các hạng mục không nằm trong IMG-02

Không triển khai trong hạng mục này:

- Backend upload ảnh thật.
- Migration SQL Server.
- AI tự sinh ảnh theo yêu cầu trực tiếp trong phần mềm.
- Tự tạo ảnh mới khi Admin thêm tài nguyên.
- Nhận diện vật thể bằng AI.
- Giao ảnh thành phẩm cho khách hàng.
- Thư viện ảnh chụp riêng tư của từng khách hàng.
- Hệ thống quản lý bản quyền hình ảnh phức tạp.
- Đăng nhập Google OAuth.
- Tích hợp email SMTP thật ngay trong Phase 02.

Những tính năng đó cần một scope và quyết định triển khai riêng.

---

## 15. Backlog riêng — Thử nghiệm đăng ký và xác minh email thật

**Mã backlog:** `AUTH-EMAIL-EXP-01`  
**Trạng thái:** Đã ghi nhận, triển khai độc lập sau theo yêu cầu chủ dự án.  

**Mục tiêu:**  
Một khách hàng nhập email của mình trên form đăng ký thử nghiệm. Hệ thống gửi email xác minh thật đến hộp thư đó. Khách mở email, nhấn liên kết có token và xác nhận thành công.

**Phạm vi thử nghiệm:**  
1. Form đăng ký độc lập.
2. Gửi email thật bằng cấu hình SMTP hoặc dịch vụ email đã chọn.
3. Token xác minh dùng một lần.
4. Token có hạn sử dụng.
5. Xử lý liên kết hết hạn.
6. Chức năng gửi lại email.
7. Thông báo xác minh thành công hoặc thất bại.
8. Không tự động liên kết với hồ sơ `KhachHang` cũ chỉ vì trùng email.

**Ranh giới:**  
- Không dùng tài khoản Gmail chính làm mật khẩu SMTP.
- Không commit thông tin đăng nhập SMTP.
- Không đụng SQL Server StudioManager chính khi thử độc lập.
- Không gửi email hàng loạt.
- Không khẳng định xác minh email đồng nghĩa đã xác minh quyền sở hữu hồ sơ khách hàng cũ.

Việc ghép nối thử nghiệm vào sản phẩm chính cần được duyệt và do Backend phù hợp xử lý sau.

---

## 16. Danh mục đầu ra cần bàn giao

Khi triển khai IMG-02 ở các Phase tương ứng, Antigravity phải bàn giao:

1. **Asset Inventory:** Danh sách ảnh và mapping ID.
2. **Visual Direction Guide:** Quy chuẩn phong cách, màu sắc, góc chụp.
3. **Asset Manifest:** Tên file, kích thước, nguồn, nơi sử dụng.
4. **Approved Image Set:** Tài nguyên được duyệt.
5. **Optimized Runtime Images:** Bản đóng gói cho Desktop và Web.
6. **Admin Media UX:** Giao diện quản lý ảnh và trạng thái.
7. **Authentication Visual Pack:** Background và phương án responsive.
8. **Accessibility Checklist:** Alt text, độ tương phản và điều hướng.
9. **Demo Safety Report:** Chứng minh không trộn dữ liệu demo và DB thật.
10. **Codex Handoff:** Contract, yêu cầu lưu trữ, upload và phân quyền ảnh.

---

## 17. Quy tắc khóa scope

**Đã khóa từ Blueprint:** 20 ảnh AI danh mục.  
**Đề xuất mở rộng đang chờ duyệt:** 4 background Authentication (`BG-AUTH-01..04`).  
**Chưa duyệt:** 8 ảnh gallery bổ sung, ảnh riêng cho từng dịch vụ, tự động sinh ảnh AI trong ứng dụng.  
**Đã ghi nhận để làm sau:** Thử nghiệm gửi email xác minh thật `AUTH-EMAIL-EXP-01`.  

Không tự ý mở rộng số lượng asset, tính năng Backend hoặc phạm vi Phase khi chưa có quyết định mới.

**Kết thúc đặc tả IMG-02 Draft 1.0.**

