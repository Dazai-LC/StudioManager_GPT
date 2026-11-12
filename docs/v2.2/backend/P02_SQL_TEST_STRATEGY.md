# P02 — Chiến lược kiểm thử SQL Server cô lập

Trạng thái SQL runtime: **BLOCKED — target đã được Owner chọn, chưa duyệt execution và chưa tạo database thử nghiệm**. Trong lượt chuẩn bị này không mở kết nối SQL và không thực thi setup, fixture hoặc cleanup SQL. Việc các test bị skip không phải bằng chứng integration đã đạt.

Final acceptance preparation, revised 12/11/2026: xem `P02_ISOLATED_SQL_APPROVAL_PLAN.md` và `P02_FINAL_ACCEPTANCE_REPORT.md`. Theo quyết định Owner, dùng instance có sẵn `localhost\SQLEXPRESS`, hủy đề xuất cài PHOENIXP02. Guard khóa Run ID `7d70a08ac15349acbd207f82c8349f6e` và đúng database tương ứng; đối chiếu `@@SERVERNAME = MSI\SQLEXPRESS`, MachineName/InstanceName và effective permissions, từ chối sysadmin kể cả READ_ONLY; revalidate các connection do harness quản lý, theo dõi DB ID/create_date. Đã bổ sung hai SQL facts (concurrent password CAS, locked actor trực tiếp ở repository), nên hiện có **11 SQL facts chưa chạy**, không còn chỉ 9. Các số liệu "lượt recovery" bên dưới là lịch sử, không phải số liệu final acceptance.

## Phạm vi đã hiện thực

Project `tests/StudioManager.SqlIntegrationTests` dùng SQL Server/ADO.NET thật và các phiên bản test package đã có trong solution. Nó không dùng SQLite, không đọc `appsettings.json`, không tự tìm SQL instance và không dùng dữ liệu seed của studio.

Các test bảo vệ cấu hình và tương thích hash PBKDF2 chạy offline. Các SQL facts mặc định skip trước khi tạo fixture. Chỉ khai báo connection string không bật được kết nối. Giá trị cấu hình và credential không được đưa vào thông báo lỗi hay `ToString()` của settings.

Lượt recovery P02 ngày 12/11/2026: project có 22 test offline pass (16 safety/manifest, 6 cryptographic compatibility) và 9 SQL facts skip. Mode SQL được để trống trong process chạy test để không vô tình kế thừa quyền chạy SQL từ môi trường. Không thay đổi cấu hình môi trường lâu dài của người dùng.

Test metadata kiểm tra danh tính server/database, schema markers, các bảng/cột và một số index/constraint cần thiết. Các mutation facts đã được viết để chạy sau khi có môi trường được duyệt:

- Sửa giờ giữ snapshot giá/tên gói lịch sử dù catalog đã đổi giá.
- Đổi gói thiếu chấp thuận báo giá bị từ chối, snapshot giữ nguyên.
- Báo giá đã cũ bị từ chối; báo giá hiện hành được chấp thuận có audit riêng.
- Hai lần thu đồng thời không vượt tổng lịch; số phiếu thu và audit khớp.
- Hai thao tác tạo lịch cùng slot chỉ commit một lịch và audit tương ứng.
- Khóa actor giữa phiên chặn thu tiền ở Application trước persistence.
- Đổi mật khẩu bằng compare-and-swap với expected hash sai không thay credential.

Các test này **chưa được chạy với SQL**. Không suy từ unit test, build hoặc SQL facts bị skip ra kết luận đúng về khóa SQL.

## Các lớp bảo vệ trước kết nối và trước ghi

1. Mode phải được đặt rõ: `READ_ONLY` hoặc `APPROVED_ISOLATED_WRITES` qua `PHOENIX_SQL_TEST_MODE`.
2. Cấu hình chỉ lấy từ `PHOENIX_SQL_TEST_CONNECTION_STRING`; yêu cầu Windows Integrated Security và duy nhất `localhost\SQLEXPRESS`, không attach file, user-instance, failover partner hoặc SQL username/password. Default instance/MSSQLSERVER, PHOENIXP02 và aliases khác bị từ chối; actual ServerName/InstanceName/MachineName phải khớp đúng target đã duyệt. Current sysadmin chỉ provisioning, runner là Windows user riêng không admin với effective permissions giới hạn.
3. `PHOENIX_SQL_TEST_RUN_ID` phải là GUID dạng N đúng `7d70a08ac15349acbd207f82c8349f6e`. Tên database phải khớp chính xác `StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e`. Run khác, dù tên DB khớp GUID mới, vẫn cần review lại.
4. Sau kết nối chỉ có truy vấn metadata giới hạn. `DB_NAME()` phải đúng tên trên; MachineName phải là MSI và đúng máy chạy test, InstanceName phải SQLEXPRESS, `@@SERVERNAME` phải `MSI\SQLEXPRESS`. Quyền hiệu lực phải qua allowlist, sysadmin/db_owner bằng 0; TRUSTWORTHY và DB_CHAINING phải OFF.
5. Database phải có bốn extended properties cấp database đã được người provision xác nhận:
   - `Phoenix.DisposableTestRunId`: đúng run ID.
   - `Phoenix.SchemaVersion`: `baseline-c2c3c23-spec2.1`.
   - `Phoenix.SchemaScriptsSha256`: checksum từ `SqlSchemaManifest.ExpectedChecksum()`.
   - `Phoenix.SchemaMetadataSha256`: fingerprint schema actual đã review, được harness so lại để phát hiện drift.
6. Mutation fixture yêu cầu mode ghi riêng và database `READ_WRITE`. Trước insert đầu tiên, toàn bộ 14 bảng nghiệp vụ phải rỗng. Có bất kỳ dữ liệu nào thì từ chối; không reset hoặc xóa dữ liệu có sẵn.
7. Fixture dùng identity do chính nó insert, dữ liệu thử có mã cố định và transaction setup. Cleanup chỉ xóa các identity/rows do actor thử nghiệm mới tạo sở hữu, kiểm tra lại markers trên **cùng target đã xác minh**. Không có `DROP DATABASE`, `TRUNCATE`, tạo bảng hoặc reset schema trong harness.
8. Các mutation tests chạy tuần tự; mỗi test tự tạo/dọn fixture. Chỉ test concurrency chủ động mở các thao tác đồng thời bên trong một test. Nếu test process bị ngắt và còn dữ liệu, lần chạy sau từ chối thay vì tự dọn.

Các gate bảo vệ nhầm target; chúng không thay thế quy trình phê duyệt của Project Owner hoặc một SQL principal giới hạn quyền. Không đặt biến mode ghi trước khi phê duyệt target, setup và phương án phục hồi.

## Provisioning có phiên bản — chưa thực thi

Theo quyết định mới, dùng SQLEXPRESS hiện có, tách biệt với default instance được bảo vệ; không cài thêm instance/container. Provisioner có sysadmin chỉ chuẩn bị DB mới theo Run ID cố định và SQL principal giới hạn theo approval plan. Không dùng bản sao database chứa PII production làm fixture.

Phiên bản schema test được nhận diện bằng nội dung hai script hiện hữu `02_CreateSchema.sql` và `05_UpgradeToSpec2_1.sql`, thứ tự tên/nội dung cố định, chuẩn hóa CRLF thành LF và SHA-256. Không dùng `03_SeedData.sql` vì chứa account/demo data không cần thiết cho fixture.

**Không chạy nguyên trạng scripts vào instance thử:** các script hiện có chứa `USE StudioManager`. Trước khi provision, phải chuẩn bị artifact setup được review, ràng buộc lại database đích và kiểm tra từng batch `GO`; không cho script chuyển sang database studio. Việc chuẩn bị/execution artifact này là bước provisioning riêng cần duyệt, không phải thao tác tự động khi chạy `dotnet test`.

Sau setup, đối chiếu metadata thực và lưu version/checksum markers. Marker là tuyên bố của người provision; test còn phải kiểm tra schema cần thiết, không xem marker đơn lẻ là bằng chứng schema hoàn toàn giống. Trước mutation fixture, harness kiểm tra trực tiếp tập 14 bảng, các cột cần thiết, không có trigger đang hoạt động hoặc FK/CHECK bị vô hiệu hóa/không trusted. Việc này độc lập với thứ tự chạy metadata Fact; không phải schema diff toàn diện.

Tương lai khi có migration mới, manifest cần liệt kê migration version/checksum tương ứng và preflight phải kiểm tra chúng. Không đánh dấu phiên bản mới trên schema cũ để làm test pass.

## Cách chạy sau khi được phê duyệt

Project Owner xác nhận chính xác local server, run ID/database, SQL principal, schema setup và database không có dữ liệu giá trị. Cung cấp connection string qua cơ chế bí mật của môi trường; không ghi giá trị vào repo, báo cáo hoặc log.

Sau đó đặt ba biến môi trường nêu trên và chạy `dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj`. Dùng `READ_ONLY` trước; chỉ chuyển sang `APPROVED_ISOLATED_WRITES` sau phê duyệt riêng cho fixture writes. Không có mode hợp lệ thì chỉ các guard unit tests chạy, SQL facts hiện rõ là skipped/BLOCKED.

Nếu có dữ liệu sót do test bị ngắt: giữ môi trường để điều tra hoặc provision run/database mới theo quy trình phê duyệt. Harness không tự dọn target không rỗng.

## Các kiểm thử thuộc phase sau

- Idempotency nghiệp vụ request/conversion/payment chưa tồn tại trong P02; không viết assertion giả rằng nó đã được hỗ trợ.
- P05/P06 bổ sung unique request conversion, rollback giữa các bước, cạnh tranh với lịch nội bộ và kiểm tra resource capacity đồng thời.
- P07 bổ sung duplicate bank reference, receipt posting nguyên tử và retry sau commit không chắc chắn.
- P08/P09 bổ sung DB–media recovery và restore trên target riêng.

Không dùng mocks để thay kết quả của các kiểm thử SQL này.

## Sửa prerequisite frontend

Test cũ đòi `src/StudioManager.CustomerPortal/StudioManager.CustomerPortal.csproj` tồn tại dù artifact đó là untracked và được bảo tồn ở worktree frontend. Backend checkout sạch không có file này, nên baseline 44 test có một lỗi môi trường giả.

Test mới giữ mục đích bảo vệ quyết định Desktop và dấu vết archive: kiểm tra amendment có `DEC-01-DESKTOP-ONLY`, tham chiếu archive Next.js/prototype, solution có WinForms và không đưa CustomerPortal vào build, project Desktop bật `UseWindowsForms`. Không khôi phục prototype Blazor, không chỉnh worktree frontend và không xóa bảo vệ kiến trúc chỉ để có kết quả xanh.
