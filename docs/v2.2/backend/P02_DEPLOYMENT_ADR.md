# P02 — ADR: Ranh giới triển khai và bảo mật Desktop

Ngày: 12/11/2026 (Asia/Saigon). Trạng thái: **DECISION-01 ĐÃ ĐƯỢC OWNER DUYỆT — OPTION B**. Các quyết định nghiệp vụ còn lại và thiết kế transport chi tiết vẫn chờ duyệt.

Owner xác nhận trong phiên P02: **“Cùng máy studio, có Windows Service tách quyền.”** Đây là phê duyệt topology; không mở rộng phạm vi P02 sang triển khai service hoặc customer persistence.

Phạm vi được phép: củng cố backend nội bộ P02. Không triển khai Windows Service, transport, account khách hoặc truy cập album production trong quyết định này. Toàn bộ giao diện tiếp tục là native WinForms.

## Bối cảnh đã kiểm tra từ source

- `Program.Main` tạo `SqlStudioRepository` và `AppFacade` trong tiến trình WinForms.
- `TaiKhoan` hiện lưu ADMIN/STAFF, `NhanVien.ChucVu` là vị trí công việc. Nhiếp ảnh gia không phải vai trò bảo mật thứ ba.
- Contracts khách nằm ở WinForms; simulation store và trạng thái QA không phải authority cho production.
- Chưa xác minh quyền SQL/filesystem thực tế hoặc topology đang vận hành. Owner đã chọn topology đích B; SQL scripts không chứng minh schema live.
- Không được coi record `UserSession` do caller tự dựng là chứng cứ đăng nhập. P02 cần ngữ cảnh nội bộ do auth service cấp và revalidation quyền hiện hành.

## Các lựa chọn

| Tiêu chí | A — Máy studio tin cậy, SQL trong WinForms | B — WinForms + Windows Service local | C — WinForms trên máy riêng/LAN + application service |
|---|---|---|---|
| Giả định triển khai | Máy và tài khoản Windows do studio quản lý, người dùng không có quyền tự sửa client/SQL | UI và service trên cùng máy; service chạy bằng OS identity riêng | Client ở máy khác, phải coi client và network không tin cậy |
| Cô lập bảo mật | Guard bảo vệ đường ứng dụng thông thường; không cô lập khỏi process đã bị can thiệp | Service giữ session, authorization, SQL và private media; pipe chỉ nhận command có kiểu | Service giữ authority; client không có SQL credential/private share; xác thực và mã hóa mạng bắt buộc |
| Độ phức tạp | Thấp nhất, tương thích kiến trúc hiện có | Trung bình: service lifecycle, IPC, versioning, ACL và installer | Cao nhất: host tập trung, network identity/TLS, timeout/retry, vận hành nhiều client |
| Installer/vận hành | App + SQL configuration; khóa quyền cài đặt và DB tối thiểu | Cài service bằng quyền quản trị; tài khoản service, pipe ACL, media ACL, upgrade/recovery | Triển khai service host, certificate/secret management, firewall, monitoring, backup và client compatibility |
| Tích hợp WinForms | Facade nội bộ giữ được phần lớn API | Adapter gọi service; giữ controls/layout, async/cancellation và typed errors | Adapter service từ xa; giữ UI, xử lý mất kết nối và uncertain commit |
| SQL credential | WinForms process/OS identity có quyền SQL; giới hạn bảo vệ phải được công bố | Chỉ service identity được cấp SQL, client không đọc configuration bí mật | Chỉ server identity được cấp SQL; không phân phối connection string SQL cho khách |
| Private media | Cùng quyền OS có thể đọc trực tiếp nếu ACL cho phép; UI không ngăn được | Service stream theo ownership, client không được quyền đọc private root | Service stream theo ownership, không công khai UNC share/raw path |
| Testability | Unit tests + SQL integration tại process boundary | Thêm contract/IPC/ACL/lifecycle và test client bị sửa | Thêm network/session isolation, TLS, replay, retry, concurrent clients |
| Giới hạn | Không đạt yêu cầu chống khách kiểm soát máy/client | Không bảo vệ tuyệt đối khỏi local administrator; không phải giải pháp cho LAN tùy ý | Cần phê duyệt hạ tầng và transport trước triển khai; không có browser UI |

## Quyết định và lý do

**Owner chọn B: cùng máy studio, Windows Service tách quyền.** Service là authority đích, giữ SQL credential/session/authorization và private media dưới OS identity riêng. Lựa chọn phù hợp việc không phân phối SQL credential cho customer và giữ nguyên toàn bộ UI WinForms. P02 chưa cài hoặc triển khai service.

Named pipe là transport local được đề xuất cho thiết kế tiếp theo, chưa được triển khai hoặc coi là cấu hình đã nghiệm thu. Thiết kế phải có ACL tường minh, chặn remote connection, message bounds và authentication/session ứng dụng; pipe ACL không tự chứng minh vai trò CUSTOMER/STAFF.

Nếu phạm vi tương lai đổi sang máy khác/LAN, cần mở lại ADR và đánh giá C. Không trình bày named pipe local-only là đủ cho mô hình này. Không triển khai endpoint mạng trong P02.

Nếu chỉ có nhân sự tin cậy sử dụng máy studio, A có thể tiếp tục phục vụ nội bộ với giới hạn đã nêu. Không dùng kết quả unit test ngữ cảnh in-process làm chứng cứ chống malicious client hoặc bảo vệ SQL credential.

P02 tiếp tục với phần không phụ thuộc transport: session issuance/revalidation nội bộ, đổi mật khẩu gắn phiên, guard nghiệp vụ, snapshot giá, safe errors, test SQL tách biệt. Account khách thật và kho ảnh riêng chỉ được nối sau khi service boundary được thiết kế/triển khai/kiểm thử trong phase được Owner cho phép.

## Hợp đồng session và SQL access

- Actor/role/current account phải được auth boundary xác lập; bỏ quyền setter tùy ý không tự tạo ra process isolation.
- Lệnh nhạy cảm phải xác minh account còn hoạt động, quyền hiện hành và tình trạng bắt buộc đổi mật khẩu. Lệnh ghi còn phải revalidate ở transaction boundary.
- Phiên forced-change chỉ cho đổi mật khẩu/đăng xuất; self-service thông thường yêu cầu mật khẩu hiện tại; admin reset là lệnh riêng.
- Đổi mật khẩu/reset/khóa phải làm phiên liên quan mất hiệu lực. Cache/revocation nội bộ một process không tự bảo đảm thu hồi trên nhiều instance.
- Khả năng revocation durable/multi-instance cần cơ chế phía authority, ví dụ security version trong DB được kiểm tra khi gọi; thêm schema này thuộc migration tương lai, phải được duyệt. Không tự chạy migration P02.
- Application database principal có quyền nghiệp vụ tối thiểu; migration/backup/restore dùng quyền và workflow riêng. Application ADMIN không có nghĩa là SQL `sysadmin`.
- Database identity/schema/permissions live chưa xác minh. Không mở SQL để thực hiện nghiệp vụ hoặc test ghi trên database studio trong P02 khi chưa được phép.
- Không log password/hash/connection string/private path. Diagnostics dùng stable code, correlation ID và metadata allowlist.

## Trạng thái bảy quyết định

| Mã | Câu hỏi/options | Tác động | Đề xuất chưa duyệt |
|---|---|---|---|
| DECISION-01 — APPROVED | Owner chọn B: cùng máy studio, Windows Service tách quyền | Trust boundary, installer, session và media isolation | Thiết kế service local; named pipe là đề xuất transport, chưa triển khai P02 |
| DECISION-02 | Có ghi nhận tiền trước `LichChup`? | Ledger, báo cáo, receipt và rollback | Cho khai báo request; chỉ posting khi đã có lịch. Thu trước cần advance ledger riêng |
| DECISION-03 | Một hay nhiều account được sở hữu hồ sơ cũ? | Unique constraints và mọi owner predicate | Một owner verified còn hiệu lực; chia sẻ là quyền cấp rõ ràng |
| DECISION-04 | Manual recovery tại studio hay email/SMS thực? | Chứng minh account, token/expiry/rate limit | Manual recovery có kiểm chứng; không OTP giả |
| DECISION-05 | STAFF có sửa ảnh catalog? | Media policy/UI và audit | ADMIN quản lý catalog; STAFF upload album theo quyền riêng |
| DECISION-06 | Username/email/SĐT nào dùng đăng nhập? | Normalization, uniqueness, legacy collision | Registry định danh chung; xung đột từ chối, không try-staff-then-customer |
| DECISION-07 | Ngân hàng, beneficiary và nguồn đối chiếu nào? | QR payload và độ tin cậy thu tiền | Owner xác nhận cấu hình thực qua kênh quản trị; không bịa thông tin ngân hàng |

## Điều kiện mở P03

DECISION-01 đã được chấp thuận. Vẫn cần Owner nghiệm thu P02 và cho phép phase tiếp theo, chốt model identifier/recovery liên quan, thiết kế service boundary và kiểm thử boundary thực phù hợp topology. Di chuyển một interface vào Application không đủ để đạt điều kiện này. Không tự bắt đầu P03 sau khi P02 build thành công.

## Readiness của adapter và service tương lai — DESIGN ONLY

- Có thể chuyển `AuthService`, authorization policies, booking/finance orchestration và `SqlStudioRepository` sang host tin cậy sau khi tách composition. `InternalAuthorization` phải do service sở hữu; registry/token không được để client tự dựng hoặc chia sẻ object reference qua IPC.
- `Program.Main` hiện vẫn tạo repository trong WinForms. Composition tương lai cần thay bằng facade adapter gọi command/query có kiểu; chỉ service identity đọc SQL configuration và kho media. Không phân phối connection credentials cho client. Chưa thay Program hoặc tạo host P02.
- `UserSession` hiện là internal DTO/bearer trong process, không phải wire authentication proof. Service xác thực token riêng, current status/role/permission, expiry và revocation; durable security version/multi-process revocation cần thiết kế migration được duyệt.
- Contracts trong `StudioManager.WinForms.Contracts` được giữ nguyên tại checkpoint. Adapter tại presentation map shared Application responses sang DTO hiển thị, không để Application reference WinForms; không đưa private paths/hash/role tự khai báo thành authority.
- Layout và navigation giữ tại WinForms. Bản adapter sau này cần async/cancellation, request size limits, correlation, timeout và idempotency cho uncertain commit. Quyền OS/pipe, chống remote, service identity và installer/lifecycle phải threat-review trước khi chọn chi tiết IPC; không mở endpoint/customer auth P02.
- Kiểm thử local tương lai gồm unit policy, dedicated SQL integration, rồi service/adapter contract, OS permissions và client giả mạo. Smoke trên chính process UI hiện nay không chứng minh trust boundary đó. SQL/UAT P02 có kế hoạch riêng và chưa được execute.

Tham chiếu: `../frontend/DESKTOP_ONLY_ARCHITECTURE_AMENDMENT.md`, `../frontend/CODEX_BACKEND_HANDOFF.md`, [Microsoft Named Pipe Security](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights).
