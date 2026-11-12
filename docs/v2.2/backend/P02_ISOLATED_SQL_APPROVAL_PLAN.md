# P02 — SQL approval plan: existing SQLEXPRESS

Ngày 12/11/2026. **PROPOSED / NOT EXECUTED — STOP AT OWNER EXECUTION APPROVAL.**
Kế hoạch này thay đề xuất cài PHOENIXP02. Không cần cài thêm instance.

## Target cố định và bằng chứng

| Thuộc tính | Giá trị bắt buộc |
|---|---|
| Máy / observed MachineName | MSI |
| Client DataSource duy nhất | localhost\SQLEXPRESS |
| Observed @@SERVERNAME | MSI\SQLEXPRESS |
| Observed InstanceName | SQLEXPRESS |
| Database | StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e |
| Run ID | 7d70a08ac15349acbd207f82c8349f6e |
| Schema version | baseline-c2c3c23-spec2.1 |
| Source SHA-256 | B8A556B123DDB93B742FDA93643AC70CB4D11E8F62E9AE30BAF37758900B0EF3 |
| Proposed Windows runner / SQL group | MSI\PhoenixP02Runner / MSI\PhoenixP02Tests |

**Owner cung cấp metadata đã xác minh**: SQLEXPRESS là SQL Server 2022 Express, chưa có user DB được báo cáo; identity hiện tại có sysadmin=1 và CREATE ANY DATABASE=1. Lượt sửa này không kết nối lại SQL; đây là bằng chứng do Owner cung cấp, không phải runtime verification của harness. Chỉ chạy local help để xác nhận sqlcmd 16.0.1000.6 có các option bên dưới.

Default instance MSSQLSERVER chứa StudioManager và database khác: PROTECTED. Không kết nối, kiểm kê hay thay cấu hình instance đó. Parser từ chối localhost/default instance, localhost\MSSQLSERVER, PHOENIXP02, hostname/IP/LocalDB aliases và mọi database/run khác. Harness đối chiếu observed server/machine/instance/database cùng markers, DB_ID/create_date trên các connection do harness quản lý. Giá trị thiếu/khác bị từ chối.

## Hai Windows identities, quyền tách biệt

- Identity Windows hiện tại có sysadmin: chỉ provisioning trên SQLEXPRESS, không chạy SQL suite hoặc WinForms UAT.
- Đề xuất user Windows mới MSI\PhoenixP02Runner thuộc group mới MSI\PhoenixP02Tests, không thuộc Administrators hoặc group SQL admin. Chưa tạo/xác minh user/group.
- SQL login Windows group chỉ trên SQLEXPRESS; user chỉ trong đúng test DB. Cấp CONNECT, VIEW DEFINITION trong database và SELECT/INSERT/UPDATE/DELETE trên schema dbo. Không tạo database role hoặc server role membership; không cấp sysadmin, db_owner, CONTROL, ALTER, CREATE ANY DATABASE, CONNECT ANY DATABASE, impersonation, backup/restore hoặc quyền server write.
- Harness kiểm tra token thực: sysadmin/db_owner phải bằng 0, kể cả READ_ONLY; NULL cũng từ chối. Server permissions phải có và chỉ được CONNECT SQL và VIEW ANY DATABASE (để inventory user DB không bị che); không có quyền ngoài allowlist tại database. Cần đủ VIEW DEFINITION và quyền từng bảng theo mode. Phải không có access StudioManager/user DB khác trên SQLEXPRESS. Không dò default instance để kiểm chứng điều này.
- Kiểm tra quyền hiệu lực bắt buộc vì quyền có thể kế thừa qua Windows groups/public. fn_my_permissions phản ánh quyền hiệu lực; IS_SRVROLEMEMBER cũng có giới hạn khi phân giải group, nên không chỉ dựa vào một role check. Xem [Microsoft: effective permissions](https://learn.microsoft.com/en-us/sql/relational-databases/system-functions/sys-fn-my-permissions-transact-sql?view=sql-server-ver16) và [role membership](https://learn.microsoft.com/en-us/sql/t-sql/functions/is-srvrolemember-transact-sql?view=sql-server-ver16).
- Database allowlist gồm CONNECT, VIEW DEFINITION, SELECT/INSERT/UPDATE/DELETE và bốn quyền metadata đọc: VIEW SECURITY DEFINITION, VIEW PERFORMANCE DEFINITION (được VIEW DEFINITION bao hàm trên SQL 2022), VIEW ANY COLUMN ENCRYPTION KEY DEFINITION và VIEW ANY COLUMN MASTER KEY DEFINITION (public defaults). Không cấp thêm quyền để khớp allowlist. Tham chiếu [Microsoft: database permission hierarchy](https://learn.microsoft.com/en-us/sql/t-sql/statements/grant-database-permissions-transact-sql?view=sql-server-ver16) và [Always Encrypted permissions](https://learn.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-ver16).
- Runner cần read/execute code, bin, schema/fingerprint và .sln; cấp quyền ghi trong profile/temp riêng cho kết quả test. Owner review ACL G: trước khi chạy; không thêm quyền admin để xử lý lỗi access.

## Artifacts được review

| File trong sql-acceptance | Context / hành vi |
|---|---|
| 00_INSTANCE_PREFLIGHT_READ_ONLY.sql | master trên SQLEXPRESS; chỉ metadata, yêu cầu provisioner nhìn đầy đủ và chưa có user DB |
| 00_CREATE_DATABASE_REVIEW_ONLY.sql | master; tạo duy nhất DB mới đã nêu, từ chối nếu target tồn tại hoặc có bất kỳ user DB; RCSI ON, TRUSTWORTHY OFF, DB_CHAINING OFF chỉ cho DB mới |
| 01_SCHEMA_REVIEW_ONLY.sql | đúng test DB; schema rỗng, dbo provisioner; fresh 14 bảng/index, markers trong transaction |
| 02_METADATA_READ_ONLY.sql | đúng test DB; version/trust flags, columns/types/null/default/keys/constraints/indexes/modules names và effective permissions, không business rows/hash |
| 03_SCHEMA_FINGERPRINT_SELECT.sql | SELECT fragment dùng trong connection đã xác minh; không phải standalone entry point |
| 04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql | master; tạo Windows group login mới, default DB đúng target; từ chối login đã tồn tại/user DB ngoài dự kiến |
| 05_CREATE_TEST_USER_REVIEW_ONLY.sql | đúng test DB và run/version/source markers; tạo user mới + đúng hai GRANT statements trong transaction |
| P02_SQL_TARGET.json | manifest target/principal, không credential |

Bốn mutation templates đều có @OwnerApprovedExecution bit=0. Mọi entry point có guard null-safe, so identity và DB theo binary collation; không USE, GO, SQLCMD includes/variables hoặc 3/4-part write targets. Không cấp server-wide permission cho runner. Chỉ CREATE/ALTER DATABASE dùng dynamic SQL hằng với đúng tên DB mới.

**Review scripts gốc database/**: 01 dùng USE master, CREATE/ALTER StudioManager rồi USE StudioManager; 02/03/04/05 đều USE StudioManager; 03/04 seed business rows, 05 backfill UPDATE và ALTER; RestoreDatabase dùng USE master và RESTORE StudioManager WITH REPLACE. Không chạy những scripts này. Giữ nguyên originals. Fresh artifact sao bảng/index từ 02 sau loại USE/GO, cộng ba index cần thiết từ 05, không seed/backfill/restore. Artifact tests đối chiếu payload với source, không phải SQL syntax/runtime verification. Chưa compile/chạy các artifacts trên engine.

Sau schema, provisioner review metadata actual đầy đủ trước khi tin fingerprint marker: 14 bảng, columns/type/null/default/PK/FK/CHECK/index; system-generated names có thể khác nên đối chiếu semantic. Fingerprint sau đó bắt drift; source hash một mình không chứng minh deployed schema. RCSI phải ON, trust/chaining OFF; không triggers/modules/synonyms tùy ý, không FK/CHECK bị disable/untrusted.

## Prerequisites và Owner approvals còn thiếu

1. Target SQLEXPRESS và DB/Run ID đã được Owner chọn trong yêu cầu hiện tại; đề xuất cài PHOENIXP02 đã hủy. Quyết định target chưa cho phép execution. Xác minh lại metadata ngay trước provisioning; nếu instance xuất hiện user DB: STOP, review lại.
2. Owner duyệt tạo Windows user/group non-admin, credential quản lý ngoài chat, quyền đọc bundle/profile/temp và group memberships. User/group/login trùng tên: STOP; không tự sửa identity có sẵn.
3. Owner duyệt bốn mutation templates và execution copies/checksums: CREATE DATABASE + DB options, schema/markers, SQL Windows login và user/grants. Current sysadmin chỉ thực hiện phần này.
4. Owner duyệt riêng 10 mutation facts: synthetic INSERT/UPDATE/DELETE, password CAS, booking/payment transactions, concurrent operations và DELETE cleanup; chạy dưới restricted user sau metadata/permission gate.
5. UAT seed và launch cần approval riêng sau suite/cleanup. Backup/restore là rehearsal/approval riêng; runner không được cấp quyền để thực hiện.
6. Owner chọn certificate policy: lệnh đề xuất dùng encrypted loopback với TrustServerCertificate/-C cho test local. Không thay trust store/firewall/SQLBrowser/production SQL config. Nếu cần trusted cert hoặc không resolve instance: STOP để review, không fallback.

## Exact commands để review — CHƯA THỰC THI

Mỗi phase chỉ chạy sau approval tương ứng. Không paste toàn bộ các block rồi chạy liên tục qua gates.

Windows account phase, trong Windows PowerShell 64-bit có quyền quản trị OS, chỉ sau approval mục 2. Tên tồn tại thì dừng; nhập mật khẩu qua prompt, không đưa vào file/chat:

~~~powershell
$ErrorActionPreference = 'Stop'
if (Get-LocalUser -Name 'PhoenixP02Runner' -ErrorAction SilentlyContinue) { throw 'User exists: review required' }
if (Get-LocalGroup -Name 'PhoenixP02Tests' -ErrorAction SilentlyContinue) { throw 'Group exists: review required' }
$runnerPassword = Read-Host 'New test runner password' -AsSecureString
New-LocalUser -Name 'PhoenixP02Runner' -Password $runnerPassword -Description 'P02 restricted test runner'
New-LocalGroup -Name 'PhoenixP02Tests' -Description 'P02 test SQL access only'
Add-LocalGroupMember -Group 'PhoenixP02Tests' -Member 'MSI\PhoenixP02Runner'
Remove-Variable runnerPassword
# Review local group memberships and ACL; do not add this user to Administrators.
~~~

SQL phase, dưới Windows provisioning identity hiện tại, từ workspace Backend. Luôn chỉ định -S và -d; -b/exit code dừng sau lỗi. -N -C là encrypted local test policy cần Owner duyệt, không dành cho production.

~~~powershell
Set-Location -LiteralPath 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
$artifactDir = Join-Path (Get-Location).Path 'docs\v2.2\backend\sql-acceptance'
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d master -b -r 1 -l 5 -t 60 -i (Join-Path $artifactDir '00_INSTANCE_PREFLIGHT_READ_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'Preflight refused; STOP' }
# STOP: review exact identity/version/empty user-DB inventory.
~~~

Sau Owner approval cho mutation templates, tạo execution copies chỉ đổi đúng flag; giữ repository templates bằng 0. Hash execution copies là evidence để review trước thực thi:

~~~powershell
$executionDir = Join-Path (Get-Location).Path '.p02-approved\7d70a08ac15349acbd207f82c8349f6e'
if (Test-Path -LiteralPath $executionDir) { throw 'Execution directory already exists: review required' }
New-Item -ItemType Directory -Path $executionDir -ErrorAction Stop | Out-Null
$templates = @('00_CREATE_DATABASE_REVIEW_ONLY.sql','01_SCHEMA_REVIEW_ONLY.sql','04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql','05_CREATE_TEST_USER_REVIEW_ONLY.sql')
foreach ($name in $templates) {
    $template = [IO.File]::ReadAllText((Join-Path $artifactDir $name))
    $flag = 'DECLARE @OwnerApprovedExecution bit=0;'
    if ([regex]::Matches($template,[regex]::Escape($flag)).Count -ne 1) { throw 'Unexpected flag: STOP' }
    [IO.File]::WriteAllText((Join-Path $executionDir $name),$template.Replace($flag,'DECLARE @OwnerApprovedExecution bit=1;'),[Text.UTF8Encoding]::new($false))
}
Get-ChildItem -LiteralPath $executionDir -File | Get-FileHash -Algorithm SHA256
# STOP: review execution copies/hash and authorization before running SQL.
~~~

Thực thi tuần tự, chỉ sau approval mục 3; lỗi ở bất kỳ bước nào thì dừng. Không retry setup để ghi đè:

~~~powershell
$testDb = 'StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e'
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d master -b -r 1 -l 5 -t 60 -i (Join-Path $executionDir '00_CREATE_DATABASE_REVIEW_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'Database provisioning failed: STOP' }
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d $testDb -b -r 1 -l 5 -t 60 -i (Join-Path $executionDir '01_SCHEMA_REVIEW_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'Schema provisioning failed: STOP' }
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d $testDb -b -r 1 -l 5 -t 60 -i (Join-Path $artifactDir '02_METADATA_READ_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'Metadata read failed: STOP' }
# STOP: review actual schema, all four markers and database options.
~~~

Sau metadata review, vẫn dưới provisioner:

~~~powershell
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d master -b -r 1 -l 5 -t 60 -i (Join-Path $executionDir '04_CREATE_TEST_LOGIN_REVIEW_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'Login creation failed: STOP' }
sqlcmd -S 'localhost\SQLEXPRESS' -E -N -C -d $testDb -b -r 1 -l 5 -t 60 -i (Join-Path $executionDir '05_CREATE_TEST_USER_REVIEW_ONLY.sql')
if ($LASTEXITCODE -ne 0) { throw 'User/grants failed: STOP' }
runas /user:MSI\PhoenixP02Runner "powershell.exe -NoProfile"
~~~

Mở shell test bằng runas (nhập mật khẩu ở Windows prompt). Trong shell MỚI, dùng build đã có; không chạy bằng sysadmin hoặc EXECUTE AS. Chỉ đọc metadata trước:

~~~powershell
Set-Location -LiteralPath 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
whoami
$env:PHOENIX_SQL_TEST_RUN_ID = '7d70a08ac15349acbd207f82c8349f6e'
$env:PHOENIX_SQL_TEST_CONNECTION_STRING = 'Server=localhost\SQLEXPRESS;Database=StudioManager_P02_Test_7d70a08ac15349acbd207f82c8349f6e;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
$env:PHOENIX_SQL_TEST_MODE = 'READ_ONLY'
dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Debug --no-build --no-restore --filter FullyQualifiedName~SqlMetadataIntegrationTests --results-directory "$env:TEMP\PhoenixP02Sql"
if ($LASTEXITCODE -ne 0) { throw 'Restricted-principal gate failed: STOP' }
# STOP: review metadata/privilege success; do not enable writes automatically.
~~~

Sau approval mục 4, trong đúng restricted shell đã qua gate:

~~~powershell
try {
    $env:PHOENIX_SQL_TEST_MODE = 'APPROVED_ISOLATED_WRITES'
    dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Debug --no-build --no-restore --logger trx --results-directory "$env:TEMP\PhoenixP02Sql"
    if ($LASTEXITCODE -ne 0) { throw 'SQL suite failed: preserve evidence and STOP' }
}
finally {
    Remove-Item Env:PHOENIX_SQL_TEST_MODE,Env:PHOENIX_SQL_TEST_CONNECTION_STRING,Env:PHOENIX_SQL_TEST_RUN_ID -ErrorAction SilentlyContinue
}
~~~

Expected (chưa phải actual): cả **11 SQL facts** = 1 metadata + 10 mutation pass, không skip; offline cases cũng pass. Không diễn giải exit 0 với SQL facts skip thành acceptance.

## Cleanup, failure và rủi ro còn lại

Fixture chỉ seed khi cả 14 business tables rỗng, trong Serializable transaction. Facts không chạy song song với nhau; concurrency nằm trong từng scenario. Cleanup xóa đúng IDs/codes/actor thuộc fixture sau revalidation, trong transaction; không TRUNCATE/reset/drop. Nếu uncertain commit/crash/cleanup fail: giữ DB, dừng; lần sau nonempty gate từ chối. Không tự relabel markers, retry SQL vào studio hoặc tái sử dụng DB tồn tại.

Harness revalidates server/instance/database/markers/permissions/DB identity trên connection do nó quản lý. Repository mở connection riêng từ string đã kiểm; restricted principal và instance tách default là điều kiện bắt buộc. Không bảo đảm chống local/SQL administrator đổi target/aliases/permissions giữa các bước. SQLEXPRESS cùng host dùng chung CPU/I/O/storage; không stress/load testing, không restart SQL services, không đổi SQLBrowser/firewall/cert/config.

Schema SQL syntax, real effective token permissions, certificate/instance resolution, concurrency/locking/rollback, all 11 SQL facts và UAT còn chưa được xác minh runtime. Chưa fault-inject mọi vị trí sau INSERT; business idempotency thuộc phase sau. Storage/SQL service ACL cần kiểm tra trước provisioning; không dùng đường dẫn backup production.

UAT chỉ sau SQL suite/cleanup, dưới runner trong output copy có appsettings test đã review, synthetic data riêng. Không chạy UAT cùng fixture suite. Backup/restore và destination rehearsal cần kế hoạch riêng. Xem P02_WINFORMS_UAT_RUNBOOK.md. Không bắt đầu P03 hoặc service implementation.

**Gate hiện tại: chờ Owner duyệt Windows identity, provisioning/permissions/certificate policy và SQL DML/cleanup. Chưa tạo DB/account/login; chưa chạy SQL, mutation suite hoặc UAT.**
