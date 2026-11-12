# P02 — Final Acceptance Preparation

Ngày: 12/11/2026, Asia/Saigon.

**P02 NOT READY — TEST ENVIRONMENT / RUNTIME BLOCKERS**

Đã hoàn tất phần source review, sửa thiếu sót trong safety harness và xác minh offline. Chưa có approval tạo/cấp quyền/schema/seed/mutation/cleanup SQL. Không mở kết nối SQL, không khởi chạy ứng dụng với SQL thật, không tạo Windows Service hoặc P03.

**Target revision theo Owner, 12/11/2026:** hủy đề xuất cài PHOENIXP02, dùng instance có sẵn `localhost\SQLEXPRESS`. Giữ nguyên DB/Run ID. Metadata SQL Server 2022 Express/no user DB, `@@SERVERNAME = MSI\SQLEXPRESS`, sysadmin=1 và CREATE ANY DATABASE=1 do Owner cung cấp; lượt này không kết nối lại để xác minh. Current identity chỉ provisioning. Thay đổi mới chỉ nằm trong safety harness/tests và tài liệu/artifacts, giữ mọi P02 uncommitted trước đó.

## 1. Current Git state

- Workspace: `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend`.
- Branch: `feature/v2.2-backend`; HEAD: `c2c3c233211ddfaf6b89ba0d5c443618c77b7324`.
- Giữ nguyên toàn bộ uncommitted P02 từ lượt recovery. Không reset/clean/overwrite baseline, commit/push/merge/đổi branch.
- Tracked diff vẫn 20 files, 491 additions/138 deletions; Git diffstat không bao gồm nội dung file mới untracked.
- Worktree Frontend riêng giữ HEAD `c2c3c23` và hai mục untracked có sẵn (prototype/tài liệu Blazor); không sửa/dọn. `database/` và `src/StudioManager.WinForms/Contracts` không có diff.

## 2. Full P02 change inventory

Danh sách file đầy đủ nằm ở phụ lục tự kiểm kê cuối báo cáo. Tác dụng before/after từng file P02 recovery được bảo toàn tại `P02_RECOVERY_REPORT.md`, mục D. Không diễn giải số test và NU1900 của báo cáo recovery thành kết quả hiện tại.

| Nhóm | Phần đã có và được review | Thay đổi ở final preparation |
|---|---|---|
| Domain/Application/Infrastructure | Read-only session, issuance/revalidation/revocation, password APIs/CAS, transaction guards, quote snapshot policy, safe diagnostics | Không sửa lại nghiệp vụ hoặc dựng kiến trúc mới |
| WinForms | Login lifecycle, ordinary/forced password forms, init-failure cleanup, explicit package quote và safe error callsites | Không sửa layout hay mở application với config production |
| Unit/source tests | 92 main-project cases + 22 SQL-project offline cases từ recovery | Thêm failed-relogin và lỗi diagnostic sink; SQL guard/parser/identity/permission/artifact tests |
| SQL harness | Exact run/database markers, empty fixture, actor/row cleanup, 9 SQL facts | Pin instance, observed instance/permissions, harness connection revalidation, DB identity continuity, metadata fingerprint; thêm 2 SQL facts có chủ đích |
| Review artifacts | ADR, ownership/transaction contracts, SQL test strategy | Approval plan, guarded SQL artifacts, target JSON, metadata queries, UAT runbook và báo cáo này |

## 3. Security review results

**SOURCE VERIFIED**: `AppFacade.Session` không có setter. `AuthService` là đường phát hành principal; record `UserSession` tự tạo không đủ registry authorization. Account/role/hash/forced status được đọc lại ở service; mutation còn revalidate dưới transaction. Guard repository không được trình bày là process boundary chống caller đã có SQL credentials.

`InternalAuthorization` có generation, token registry, expiry và post-await validation; các đường logout, forced-change cancel, initialization fail/cancel và return/exception/finally LoginForm đều thu hồi session. Password rotation không được làm sống lại phiên đã logout hoặc thay thế phiên của account đăng nhập sau đó.

Không tìm thấy raw `ex.Message`/stack-trace rendering hoặc gán `.Session =` còn thực thi trong source đã rà soát. Chưa thực hiện tấn công runtime hoặc kiểm chứng trust boundary của Windows Service.

## 4. Password/session verification

**UNIT TEST VERIFIED** tại Application với stateful repository double:

- ADMIN/STAFF login giữ ID/role/hash; sai mật khẩu và account khóa không phát hành session.
- Test mới `FailedRelogin_CannotKeepAnEarlierAuthenticatedSession`: login lại sai không giữ phiên cũ.
- Principal giả, sửa account/role/token, stale locked/role/hash/forced/missing account bị chặn trước write.
- Ordinary change xác minh current password và chỉ đổi account trong session; không có target account ID trên public self-service API.
- Forced endpoint chỉ dành cho restricted session; restricted session không chạy nghiệp vụ thường; admin reset là operation riêng.
- Failed change/CAS không ghi credential; rotate từ chối token cũ; account khác không bị đổi.
- Logout/expiry/races trong account read/login/password write không khôi phục phiên.
- Đường CRUD/booking/service/resource/payment/refund/report nội bộ vẫn được gọi qua guard khi có quyền (repository-double compatibility, không phải SQL evidence).

**UNIT TEST VERIFIED** trên cryptographic implementation thật: 6 cases PBKDF2-SHA256 legacy format, salt độc lập, wrong password và malformed encoding. Không đọc hash studio.

**SQL INTEGRATION VERIFIED: chưa có.** Binary expected-hash comparison, account-state SQL guard, concurrent CAS/audit phải chạy trên target được duyệt. Không dùng mocks để chứng minh locking.

## 5. Booking snapshot verification

**SOURCE VERIFIED + UNIT TEST VERIFIED**: 19 policy cases giữ agreed name/price khi chỉ sửa lịch, kể cả giá hiện hành 0/khác hoặc gói ngừng bán. Đổi gói/nhận quote cần explicit acceptance, expected original ID/price và reason hợp lệ; backend đọc catalog thật dưới transaction, từ chối stale quote, ghi audit riêng.

**BLOCKED**: SQL snapshot persistence, audit consistency, aggregate công nợ/receipt và tương tác xác nhận quote WinForms chưa được chạy. Existing 4 SQL snapshot/quotation facts vẫn giữ, không hạ assertion để pass. Runbook yêu cầu đổi giá catalog sau tạo lịch rồi chỉ sửa thời gian/phòng; tiếp đó thay gói có acceptance.

## 6. Diagnostics verification

**SOURCE VERIFIED + UNIT TEST VERIFIED**: safe code/message/correlation và JSON allowlist không serialize exception message/stack/Data/hash/private path. Test mới đưa TraceListener cố tình ném lỗi, xác nhận listener thực sự được gọi và lỗi logging không thay public result. Listener được tháo trong finally; collection không chạy song song khi thay global listener.

**BLOCKED**: UI runtime error dialogs và các trường hợp lỗi SQL thật. Correlation identity qua cùng exception được unit-test; user-friendly Result từ repository và UI chỉ được compile/source review ở các đường SQL chưa execute.

## 7. SQL safety audit

Thiếu sót xác nhận trong harness cũ: cho phép nhiều tên local instance, chỉ kiểm tra MachineName; chưa có effective-permission guard; source checksum marker không tự phát hiện schema drift; connection sau preflight chưa tự xác minh lại. Đã siết mà không kết nối SQL:

| Cấu hình/rủi ro | Enforcement hiện tại | Evidence |
|---|---|---|
| Database `StudioManager`/system hoặc tên bất ngờ | Pin đúng Run ID `7d70a08ac15349acbd207f82c8349f6e` và database tương ứng; actual DB_NAME phải khớp | UNIT TEST VERIFIED parser/observed identity, cả run khác có tên DB khớp GUID vẫn bị từ chối |
| Thiếu mode/run/connection | Parse từ chối; facts skip trước fixture khi mode không bật | UNIT TEST VERIFIED |
| Default/MSSQLSERVER, PHOENIXP02, hostname/IP/LocalDB aliases khác | Parser chỉ nhận `localhost\SQLEXPRESS`; actual InstanceName=SQLEXPRESS, MachineName=MSI và @@SERVERNAME=MSI\SQLEXPRESS phải khớp | UNIT TEST VERIFIED predicates; SQL observation BLOCKED |
| Alias của approved name trỏ instance khác | So actual server/instance/machine/DB, thiếu/khác từ chối; principal không có quyền production | SOURCE VERIFIED; actual alias resolution BLOCKED |
| Unknown schema | Source version/hash; actual metadata hash so marker; 14 dbo tables/cột/constraints, không synonym/module/trigger đang hoạt động, FK/CHECK trusted | SOURCE VERIFIED; fingerprint SQL chưa execute/compile trên engine |
| Missing markers | Run/version/source checksum/metadata checksum bắt buộc | UNIT TEST VERIFIED cho marker predicates; actual SQL BLOCKED |
| Quyền rộng hoặc thiếu quyền | Từ chối sysadmin/db_owner/NULL kể cả READ_ONLY; server/database permission allowlists, cấm access studio/user DB khác; cần VIEW DEFINITION và object DML theo mode, TRUSTWORTHY/DB_CHAINING OFF | UNIT TEST VERIFIED role và permission predicates fail-closed; effective SQL permissions BLOCKED |
| DB bị recreate | Harness lưu DB_ID/create_date và đối chiếu các connection do harness quản lý | SOURCE VERIFIED, chưa runtime-test |
| Partial seed | Seed transaction; `_ownsActor` chỉ sau commit. Uncertain commit không tự cleanup | SOURCE VERIFIED, rollback SQL BLOCKED |
| Run bị ngắt/nonempty | 14-table empty gate trước fixture; không reset; cleanup chỉ actor/IDs sở hữu | SOURCE VERIFIED; interrupted-run SQL scenario BLOCKED |

Mỗi harness-managed connection kiểm tra metadata/quyền trước khi trả cho fixture. Repository còn mở connection riêng từ cấu hình đã kiểm; least-privilege principal và instance tách biệt là rào bắt buộc. Không có bảo đảm chống SQL/local administrator đổi aliases/DB/markers trong lúc chạy; không gọi preflight là cơ chế cô lập tuyệt đối.

Cleanup là DML DELETE trong transaction, có actor/row ownership; không DROP/TRUNCATE hoặc dynamic SQL trong fixture. Setup artifacts có dynamic SQL cố định chỉ để CREATE/ALTER **đúng DB test mới**, có guard và execution flag=0. Không có SQLCMD include/variable hoặc `GO` để vượt guard. Payload fresh schema không chạy seed/backfill lịch sử.

Lượt đổi target bổ sung instance preflight read-only và hai templates login/user. Cả bốn mutation templates đều mặc định tắt, mọi entry point kiểm tra identity/context null-safe theo binary collation. Login/user tồn tại thì dừng; grants chỉ CONNECT/VIEW DEFINITION trong test DB và SELECT/INSERT/UPDATE/DELETE trên dbo, không role/server grants. Bảy PowerShell command blocks trong approval plan đã parse AST offline không lỗi, không được thực thi. Đây không phải SQL syntax/runtime verification.

## 8. Exact proposed test configuration

- Machine `MSI`, existing instance `localhost\SQLEXPRESS`, observed server `MSI\SQLEXPRESS`, InstanceName `SQLEXPRESS`.
- Database `StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e`.
- Run ID `7d70a08ac15349acbd207f82c8349f6e`.
- Windows SQL group đề xuất `MSI\PhoenixP02Tests`; test user non-admin đề xuất `MSI\PhoenixP02Runner`. Chưa tạo hoặc xác minh các identity này.
- Windows Integrated Security, không SQL username/password. Cấu hình proposal không được ghi vào production settings.
- Source manifest: `02_CreateSchema.sql` + `05_UpgradeToSpec2_1.sql` chuẩn hóa LF, SHA-256 `B8A556B123DDB93B742FDA93643AC70CB4D11E8F62E9AE30BAF37758900B0EF3`.
- Metadata fingerprint được setup lưu sau fresh schema; actual giá trị chưa tồn tại/chưa được đọc.

Owner đã chọn SQLEXPRESS theo metadata được cung cấp, không cần cài PHOENIXP02. Current sysadmin chỉ provisioning; SQL facts/UAT dùng Windows runner non-admin riêng, harness từ chối sysadmin. Kế hoạch và scripts buộc kiểm tra lại exact identity/empty user-DB inventory trước CREATE. Lệnh chính xác cho Windows account, execution copies/checksums, SQL provisioning, restricted READ_ONLY gate và mutation suite: `P02_ISOLATED_SQL_APPROVAL_PLAN.md`. Không có lệnh nào trong kế hoạch này đã được thực thi với SQL.

## 9. Read-only metadata status

**BLOCKED** ở execution approval gate: target đã chọn nhưng DB chưa tạo, token/schema thực chưa được harness xác minh. Đã chuẩn bị instance preflight chỉ đọc trên master/SQLEXPRESS và `sql-acceptance/02_METADATA_READ_ONLY.sql` trên đúng test DB: server/version/database/compatibility/RCSI/trust flags; markers; bảng/cột/types/defaults; PK/unique/FK/CHECK; index keys/includes/filter; tên procedures/views/triggers/functions/synonyms; effective permissions. Không query business rows hoặc dump module definitions có thể chứa bí mật.

So sánh schema actual với scripts phải thực hiện sau provisioning bằng full metadata; không suy từ marker hoặc file SQL rằng deployed schema giống. SQL scripts gốc có `USE StudioManager` (01 còn CREATE/ALTER DB, Restore có REPLACE). Đã giữ nguyên originals và không execute; artifact fresh loại bỏ database switching, không chạy script seed.

## 10. Actual tests and builds

| Kiểm tra cuối | Debug | Release | Evidence class |
|---|---|---|---|
| Entire solution build | 0 errors, 0 warnings | 0 errors, 0 warnings | SOURCE VERIFIED compilation |
| Main project | 94 pass, 0 fail, 0 skip | 94 pass, 0 fail, 0 skip | UNIT TEST VERIFIED (bao gồm source checks) |
| Offline SQL-project tests | 60 pass, 0 fail | 60 pass, 0 fail | UNIT TEST VERIFIED: guard/hash/artifact checks |
| SQL facts | 11 skip | 11 skip | BLOCKED |

Tổng mỗi configuration: **154 pass, 0 fail, 11 skip**; không cộng Debug và Release thành số cases khác nhau. Lượt đổi target thêm 18 offline cases cho exact identity/run, role rejection và artifacts. SQL count ban đầu 9 tăng thành 11 vì thêm concurrent-password-CAS và locked-actor-inside-repository tests, chưa chạy cả hai. Không có kết quả SQL INTEGRATION VERIFIED trong lượt này.

Commands lượt đổi target: `dotnet build StudioManager.sln -c Debug --no-restore --nologo` và tương tự Release; `dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Debug --no-build --no-restore --nologo` và tương tự Release; cùng hai commands test cho `tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj`. Đặt `PHOENIX_SQL_TEST_MODE` rỗng **chỉ trong process test**, SQL-project test còn xóa process connection/run variables. Không đọc/ghi application connection settings. VSTEST temporary-artifact fallback vẫn là thông báo runner, không phải vulnerability/SQL result; không thay package chỉ để giấu thông báo. Hai artifact assertions ban đầu false-positive trên chuỗi tên quyền CREATE ANY DATABASE trong SELECT; scanner được sửa để bỏ string literal, EXEC vẫn bị cấm. Kết quả bảng là lần kiểm tra cuối sau sửa.

## 11. WinForms UAT

**BLOCKED / NOT STARTED** cho toàn bộ tương tác SQL-backed. Đã chuẩn bị `P02_WINFORMS_UAT_RUNBOOK.md`, gồm ADMIN/STAFF/bad/locked/logout/init fail/forced, ordinary/current-password/admin reset, CRUD/account/catalog, booking/snapshot/quote/resource, payment/refund/dashboard/report, layout/DPI/error dialog và recovery rehearsal.

Không khởi chạy ứng dụng mặc định vì config có thể trỏ studio. UAT phải dùng output copy có test config sau target approval, data synthetic và principal giới hạn. Không sửa config production hoặc Frontend worktree. Không có ảnh DrawToBitmap hoặc mock nào được dùng để báo UAT PASS.

## 12. NU1900 và dependency audit

**Đã xác minh nguyên nhân môi trường và audit thành công ở lượt final-preparation trước khi đổi target**; lượt đổi target chỉ dùng cache --no-restore, không claim audit network mới:

1. Source được liệt kê an toàn: nuget.org HTTPS và local Visual Studio package source; không in credential/query string. Không có HTTP(S)/ALL proxy environment được phát hiện; không đọc proxy passwords hoặc thay Windows settings.
2. DNS `api.nuget.org` phân giải trong sandbox nhưng socket HTTPS bị từ chối với OSError 10013. Không phải bằng chứng lỗi certificate.
3. Ngoài sandbox, mặc định TLS verification thành công: service index HTTP 200, vulnerability index HTTP 200, hai data resources HTTP 200. Dữ liệu gzip được giải nén để kiểm tra hợp lệ; không tắt kiểm tra cert.
4. `dotnet restore StudioManager.sln --force-evaluate --no-http-cache --nologo` ngoài sandbox thành công không NU1900.
5. `dotnet list StudioManager.sln package --vulnerable --include-transitive --no-restore --format json` ngoài sandbox exit 0, không trả package vulnerability nào cho 6 projects.

Không nâng package, sửa NuGet.Config, tắt audit hoặc thay network/certificate. Build cuối cả hai cấu hình không warning. "Không có vulnerability được báo tại thời điểm/nguồn đã kiểm" không phải cam kết mọi package an toàn vô thời hạn. Build/test cần dùng cache trong sandbox; restore cần nguồn audit truy cập được.

Tham chiếu chính thức: [NuGet NU1900](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1900), [NuGet package auditing](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages).

## 13. Backup/recovery readiness

**SOURCE VERIFIED**, nhưng rehearsal **BLOCKED / NOT STARTED**. Backup/restore source vẫn có guard admin, log và destructive-operation UI confirmations; chưa execute. Runner test không được cấp backup/restore/sysadmin. Rehearsal cần provisioner riêng, approved backup path/SQL service ACL và destination `StudioManager_P02_Restore_7d70a08ac15349acbd207f82c8349f6e` trên instance riêng, không restore vào StudioManager. Không tạo backup, không kiểm CHECKSUM/VERIFYONLY hoặc phục hồi thật ở lượt chuẩn bị.

## 14. Remaining risks and architecture readiness

- Quyền/schema SQL và SQL syntax của review artifacts chưa runtime-verified. Fingerprint marker chỉ được tin sau baseline metadata review; không phải chứng cứ tự có.
- SQLEXPRESS cùng host với default instance dùng chung CPU/I/O/storage. Chưa kiểm tra token/certificate/instance resolution bằng runner thật; không stop/restart service, stress test hoặc đổi production/network/SQL configuration để vượt lỗi.
- Transaction guards/capacity/audit/CAS đúng hay không trên SQL chỉ được kết luận sau tests thật. Chưa có fault injection sau mọi bước mutation; cần review riêng nếu nghiệm thu rollback toàn bộ điểm lỗi.
- P02 chưa có request/payment business idempotency hoặc receipt archival của phase sau; không claim random unique codes ngăn duplicate nghiệp vụ.
- In-process authority không chống client có quyền can thiệp/SQL credential. Windows Service đích chưa implement; durable session security version, OS/IPC permissions và ownership production vẫn là future design.
- `P02_DEPLOYMENT_ADR.md` đã bổ sung service-readiness: di chuyển auth/policy/orchestration/repository vào service; giữ SQL credentials/private media ngoài client; session service-owned; adapter giữa Application và frozen WinForms contracts; không chọn/implement transport chi tiết P02.
- Các thay đổi hành vi recovery (current-password bắt buộc, init fail quay login, quote explicit) cần UAT thật. Không biết regression runtime nào chỉ từ build xanh.

## 15. Required approvals

Target SQLEXPRESS đã được Owner chọn. Còn cần Owner duyệt **execution workflow tại `P02_ISOLATED_SQL_APPROVAL_PLAN.md`** trước khi: tạo Windows user/group, test DB mới và options, fresh schema/markers, Windows SQL login/user/grants, certificate policy loopback, fixture writes và cleanup. UAT synthetic seed/launch có gate riêng. Không yêu cầu chia sẻ mật khẩu trong chat; current sysadmin chỉ provisioning, restricted runner phải qua metadata/permissions gate trước DML.

Approval tests không bao gồm cài/config SQL instance, thay firewall/certificate, backup/restore rehearsal hoặc DROP DB. Các việc đó cần phạm vi riêng. Nếu target đề xuất không phù hợp, sửa proposal/guards/artifacts và review lại trước execution; không nới guard để dùng studio DB.

## 16. Recommendation on closure

Build errors gốc vẫn được sửa, offline tests đạt, NU1900 đã phân lập và audit lại thành công, ADR được giữ. Chưa đủ điều kiện đóng P02 vì **SQL integration, test-environment identity/permissions/schema và security-sensitive runtime UAT chưa verified**. Owner còn cần nghiệm thu evidence.

SQL access/writes/DDL/DML/cleanup/seed/backup/restore thực thi trong lượt này: **0**. Prepared SQL chỉ là file review. Không commit/push/merge, không sửa production config, không sửa Frontend worktree, không Windows Service, không P03.

**P02 NOT READY — TEST ENVIRONMENT / RUNTIME BLOCKERS**. Dừng tại approval gate.

## Phụ lục — inventory đầy đủ từ Git

Danh sách dưới đây gồm mọi tracked modification và untracked artifact của P02; `bin/obj` bị ignore. `M` là sửa file tracked; `??` là file mới chưa stage/commit.

```text
 M StudioManager.sln
 M src/StudioManager.Application/Contracts.cs
 M src/StudioManager.Application/Services/AppFacade.cs
 M src/StudioManager.Application/Services/StudioServices.cs
 M src/StudioManager.Domain/Models.cs
 M src/StudioManager.Infrastructure/SqlStudioRepository.cs
 M src/StudioManager.WinForms/AuthVisualAssets.cs
 M src/StudioManager.WinForms/Controls/AtelierNotifications.cs
 M src/StudioManager.WinForms/Controls/Ui.cs
 M src/StudioManager.WinForms/Forms/ApplicationInitializationForm.cs
 M src/StudioManager.WinForms/Forms/ChangePasswordForm.cs
 M src/StudioManager.WinForms/Forms/LoginForm.cs
 M src/StudioManager.WinForms/Pages/AccountsPage.cs
 M src/StudioManager.WinForms/Pages/BackupPage.cs
 M src/StudioManager.WinForms/Pages/BookingsPage.cs
 M src/StudioManager.WinForms/Pages/CrudPage.cs
 M src/StudioManager.WinForms/Pages/DashboardPage.cs
 M src/StudioManager.WinForms/Pages/ReportsPage.cs
 M tests/StudioManager.Tests/AccountSecurityServiceTests.cs
 M tests/StudioManager.Tests/FrontendV22Tests.cs
?? docs/v2.2/backend/P02_CUSTOMER_AND_TRANSACTION_CONTRACTS.md
?? docs/v2.2/backend/P02_DEPLOYMENT_ADR.md
?? docs/v2.2/backend/P02_FINAL_ACCEPTANCE_REPORT.md
?? docs/v2.2/backend/P02_ISOLATED_SQL_APPROVAL_PLAN.md
?? docs/v2.2/backend/P02_RECOVERY_REPORT.md
?? docs/v2.2/backend/P02_SQL_TEST_STRATEGY.md
?? docs/v2.2/backend/P02_WINFORMS_UAT_RUNBOOK.md
?? docs/v2.2/backend/sql-acceptance/00_CREATE_DATABASE_REVIEW_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/00_INSTANCE_PREFLIGHT_READ_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/01_SCHEMA_REVIEW_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/02_METADATA_READ_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/03_SCHEMA_FINGERPRINT_SELECT.sql
?? docs/v2.2/backend/sql-acceptance/04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/05_CREATE_TEST_USER_REVIEW_ONLY.sql
?? docs/v2.2/backend/sql-acceptance/P02_SQL_TARGET.json
?? src/StudioManager.Application/BookingPackageSnapshotPolicy.cs
?? src/StudioManager.Application/InternalAuthorization.cs
?? src/StudioManager.Application/SafeDiagnostics.cs
?? tests/StudioManager.SqlIntegrationTests/AcceptanceArtifactSafetyTests.cs
?? tests/StudioManager.SqlIntegrationTests/PasswordHashCompatibilityTests.cs
?? tests/StudioManager.SqlIntegrationTests/RepositoryTransactionIntegrationTests.cs
?? tests/StudioManager.SqlIntegrationTests/SqlMetadataIntegrationTests.cs
?? tests/StudioManager.SqlIntegrationTests/SqlMutationFixture.cs
?? tests/StudioManager.SqlIntegrationTests/SqlTestSafety.cs
?? tests/StudioManager.SqlIntegrationTests/SqlTestSafetyTests.cs
?? tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj
?? tests/StudioManager.SqlIntegrationTests/VerifiedSqlTestDatabase.cs
?? tests/StudioManager.Tests/BookingPackageSnapshotTests.cs
?? tests/StudioManager.Tests/DiagnosticListenerFailureTests.cs
?? tests/StudioManager.Tests/InternalSecurityRegressionTests.cs
?? tests/StudioManager.Tests/SafeDiagnosticsTests.cs
```
