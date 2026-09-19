# Studio Manager

Ứng dụng desktop quản lý đặt lịch và dịch vụ cho studio chụp ảnh, xây dựng bằng C# .NET 8 WinForms và Microsoft SQL Server. Project bám theo đặc tả phiên bản 2.0: kiến trúc phân lớp, phân quyền, chống trùng lịch, tiến độ tuần tự, thanh toán nhiều lần, hoàn tiền, báo cáo, nhật ký và sao lưu.

## Điểm nổi bật

- Dashboard hiện đại với bốn thẻ KPI, biểu đồ dòng tiền 6 tháng, biểu đồ trạng thái và danh sách lịch sắp tới.
- Skeleton loading có hiệu ứng shimmer trong lúc nạp dữ liệu dashboard, tránh cảm giác màn hình bị treo hoặc trống.
- Sidebar có thể thu gọn/mở rộng bằng nút `☰`; trạng thái menu đang chọn được làm nổi bật.
- Các màn hình quản lý có đủ thao tác tìm kiếm, làm mới, xem chi tiết, xuất CSV, thêm, sửa và ngừng dùng theo quyền.
- Giao diện tiếng Việt, hỗ trợ DPI 100–150%, tự co giãn trên màn hình 1366×768 trở lên.
- Hai vai trò: Quản trị viên và Nhân viên; quyền được kiểm tra tại giao diện và Service.
- Kiểm tra trùng nhiếp ảnh gia/phòng theo khoảng `[Bắt đầu, Kết thúc)` trong transaction.
- Giá gói và dịch vụ được chụp lại để bảo toàn lịch sử.
- Dòng tiền bất biến: thu nhiều lần, hoàn nhiều lần, không vượt giới hạn.
- PBKDF2-HMAC-SHA256 với salt riêng; SQL luôn dùng parameter.

## Yêu cầu

- Windows 10/11 x64.
- Visual Studio 2022 17.8+ với workload **.NET desktop development**.
- .NET 8 SDK/Desktop Runtime.
- SQL Server 2019+ hoặc SQL Server Express và SQL Server Management Studio.

## Cài đặt

1. Mở SQL Server Management Studio, chạy lần lượt:
   - `database/01_CreateDatabase.sql`
   - `database/02_CreateSchema.sql`
   - `database/03_SeedData.sql`
2. Mở `src/StudioManager.WinForms/appsettings.json`, sửa `ConnectionString` theo máy. Cấu hình mặc định dùng `localhost\SQLEXPRESS` và Windows Authentication.
3. Mở `StudioManager.sln` bằng Visual Studio, chọn **Restore NuGet Packages** rồi **Build Solution**.
4. Đặt `StudioManager.WinForms` làm Startup Project và nhấn `F5`.

Nếu bạn đã chạy ba file SQL trước đó thì **không cần chạy lại**. Chỉ cần kiểm tra `ConnectionString` trỏ đúng instance và database `StudioManager`; chạy lại file seed có thể tạo dữ liệu trùng.

Tài khoản demo:

| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin` | `Admin@123` |
| Nhân viên | `nhanvien1` | `Admin@123` |
| Nhân viên | `nhanvien2` | `Admin@123` |

> Mật khẩu trên chỉ dùng trình diễn. Hãy đổi mật khẩu và cập nhật seed khi triển khai thực tế.

## Build và kiểm thử

```powershell
dotnet restore StudioManager.sln
dotnet build StudioManager.sln -c Release
dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Release
```

Publish self-contained Windows x64:

```powershell
dotnet publish src/StudioManager.WinForms/StudioManager.WinForms.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

## Cấu trúc

- `StudioManager.Domain`: entity, enum và quy tắc miền thuần.
- `StudioManager.Application`: DTO, interface repository và service nghiệp vụ.
- `StudioManager.Infrastructure`: ADO.NET, SQL Server, PBKDF2, backup.
- `StudioManager.WinForms`: giao diện và điều hướng.
- `StudioManager.Tests`: unit test quy tắc quan trọng.
- `database`: script tạo DB, schema, seed, test và phục hồi.
- `docs`: đặc tả rút gọn, dữ liệu và hướng dẫn người dùng.

## Ghi chú sao lưu

Đường dẫn tệp `.bak` được hiểu trên máy chạy SQL Server; tài khoản dịch vụ SQL Server phải có quyền ghi. Phục hồi là thao tác thay thế dữ liệu: đóng ứng dụng, tạo bản sao lưu hiện tại, rồi chạy `database/RestoreDatabase.sql` sau khi thay đường dẫn tệp.
