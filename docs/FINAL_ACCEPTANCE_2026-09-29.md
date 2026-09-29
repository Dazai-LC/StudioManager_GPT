# Báo cáo chốt nghiệm thu — StudioManager v2.1

## Phạm vi

- Branch: `feature/spec-v2.1-master-data`
- Commit nghiệm thu: `a56045152a1298c593a08676d8f331b6173a9034`
- Môi trường: Windows, .NET 8, SQL Server local, database `StudioManager`
- Không merge vào `main` trong đợt nghiệm thu này.

## Kết quả kỹ thuật

| Gate | Kết quả |
|---|---|
| `dotnet clean StudioManager.sln` | Pass |
| `dotnet restore StudioManager.sln` | Pass |
| `dotnet build StudioManager.sln` | Pass — Domain, Application, Infrastructure, WinForms, Tests |
| `dotnet test StudioManager.sln` | Pass — 29/29, 0 failed, 0 skipped |
| `database/05_UpgradeToSpec2_1.sql` | Pass — migration idempotent, bổ sung audit columns cho dữ liệu cũ |

## Kết quả nghiệm thu chức năng

| Nhóm | Kết quả |
|---|---|
| Đăng nhập, đổi mật khẩu bắt buộc, tài khoản, phân quyền | Pass |
| Khách hàng/danh mục/tài nguyên và guard xóa-ngừng dùng | Pass |
| Tạo lịch, trùng lịch, sửa/trạng thái/hủy lịch, chi tiết lịch | Pass |
| Phân công/trả/hủy tài nguyên, đồng bộ khi dời lịch | Pass |
| Tài nguyên có dịch vụ thuê: chọn Có/Không, thêm dòng dịch vụ và audit | Pass |
| Dịch vụ, giảm giá, chặn tổng phải thu nhỏ hơn đã thu | Pass |
| Thu tiền, hoàn tiền, lịch sử tài chính và biên nhận | Pass |
| Báo cáo, dashboard, nhật ký/audit detail | Pass |
| Xuất Excel `.xlsx` Unicode, Times New Roman | Pass |
| Sao lưu trên SQL Server Express | Pass |

## Kết luận

Mục tiêu **trên 90% đặc tả v2.1** đã đạt theo evidence build/test và nghiệm thu thủ công. Branch sẵn sàng review/merge khi chủ dự án phê duyệt.

## Hạng mục còn để kiểm thử phát hành

1. Phục hồi database từ `.bak` chỉ nên chạy trên database kiểm thử riêng vì thao tác thay thế toàn bộ dữ liệu.
2. Ghi nhận ảnh/log DPI riêng tại 125% và 150%; smoke resize hiện đã pass.
