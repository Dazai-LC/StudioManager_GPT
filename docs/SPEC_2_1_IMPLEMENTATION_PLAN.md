# Kế hoạch triển khai đặc tả StudioManager 2.1

## Quy ước làm việc

- Branch: `chore/spec-v2.1-stage-0`, tạo từ `fix/login-form-clarity`; không merge vào `main`.
- Mỗi giai đoạn là một branch/commit nhỏ tiếp theo, build/test riêng trước khi sang gate kế tiếp.
- Không có thay đổi nghiệp vụ nào ở Stage 0. Mọi trạng thái nghiệm thu chưa chạy là `Not verified`.

| Giai đoạn | Đầu ra chính | File/khu vực dự kiến | Gate |
|---|---|---|---|
| 0. Chốt đặc tả | Gap analysis, plan, traceability, câu hỏi đã duyệt | `docs/SPEC_2_1_*.md` | Phê duyệt các câu hỏi bên dưới |
| 1. Khung & DB | Migration/clean scripts, constraints/index/seed, repository contracts rõ ràng | `database/`, Domain, Application contracts, Infrastructure | Build + database sạch tạo/seed thành công |
| 2. FR01 | Session, AccountService, quyền, audit account, UI account hợp lệ | Auth/Account services, login/accounts, SQL | AT01-AT06 |
| 3. FR02-FR04 | Customer history/xóa, employee/category status và lookups | Customer/catalog services, CRUD pages, SQL | AT07-AT11 |
| 4. FR05-FR06 | Query ID/paging, full filter, booking edit/cancel/progress, resource reschedule và audit | Booking/Progress services, booking pages, SQL | AT12-AT23 |
| 5. FR09 | Assign/return/cancel resource, capacity, rental confirmation | Resource service/page, SQL | AT24-AT25 |
| 6. FR07 | Service snapshot, discount invariants, payment/refund/receipt, summary refresh | Finance service/pages, SQL | AT26-AT35 |
| 7. FR08/10/11 | Aggregate report, audit viewer, backup/restore two-step and history | Reports/Audit/Backup services/pages, SQL | AT36-AT40 |
| 8. Bàn giao | DPI smoke test, README/UserGuide, publish instructions, acceptance result | docs, tests, project config | AT41 + full build/test |

## Nguyên tắc code cho mọi giai đoạn

1. UI chỉ gọi Application Service; không truy cập `AppFacade.Repository` từ Form/UserControl.
2. Service kiểm tra session, role, validation nghiệp vụ và quyền trước khi gọi repository.
3. Repository chỉ truy vấn/persistence và thực hiện transaction theo contract service; mọi SQL parameterized.
4. Mutation nhiều bảng recheck bất biến trong transaction, audit trước/sau/lý do khi yêu cầu, rồi mới trả success sau commit.
5. Test unit cho quy tắc miền và integration/acceptance cho SQL phải gắn commit/hash đang test.

## Câu hỏi cần phê duyệt trước khi code Stage 1

1. **Nâng cấp dữ liệu:** Có phải giữ nguyên dữ liệu SQL Server hiện có? Đặc tả yêu cầu script tạo DB sạch, nhưng dự án hiện có database; nếu cần giữ dữ liệu, phải bổ sung migration không phá dữ liệu thay vì sửa/seed lại trực tiếp.
2. **Baseline:** Xác nhận branch mới nên tiếp tục từ `fix/login-form-clarity` như hiện tại, hay cần gộp cả experiment `feature/app-initialization-screen` trước khi bắt đầu các gate nghiệp vụ? Hai branch này chưa được merge và có lịch sử khác nhau.
3. **Phạm vi giao hàng:** Có đủ ba nhóm UML mà tài liệu nhắc tới để đối chiếu ở Stage 0 không? Nếu có, vui lòng cung cấp/chỉ vị trí; nếu không, đặc tả 2.1 cùng D01-D12 sẽ là nguồn duy nhất để triển khai.
4. **Xác minh Windows/SQL:** Môi trường hiện tại không chạy .NET/WinForms/SQL Server. Sau mỗi gate, bạn sẽ chạy checklist/AT trên Windows theo các lệnh mình cung cấp, hay muốn mình dừng ở `Not verified` cho đến khi bạn gửi evidence? Không được ghi Pass chỉ từ đọc source.
5. **Xác nhận workflow:** Đặc tả yêu cầu dừng khi có câu hỏi. Sau khi bạn xác nhận 4 điểm trên, mình bắt đầu Stage 1; chưa triển khai toàn bộ FR01-FR11 trong một commit lớn.
