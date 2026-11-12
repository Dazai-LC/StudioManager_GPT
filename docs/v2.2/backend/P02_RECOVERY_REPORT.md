# StudioManager v2.2 — P02 Recovery & Security Foundation

Ngày báo cáo: 12/11/2026, Asia/Saigon.

**P02 NOT READY — BLOCKERS REMAIN**

Build và kiểm thử offline đã phục hồi. Các thay đổi chỉ nằm trong Backend worktree, chưa commit. P02 chưa đạt nghiệm thu tích hợp vì chưa có SQL test database cô lập được phê duyệt, chưa kiểm chứng schema live và chưa thực hiện WinForms/SQL UAT. Không dùng số test xanh làm chứng cứ cho locking hoặc tính tương thích production.

## A. Baseline và phạm vi khôi phục

- Workspace: `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend`.
- Branch: `feature/v2.2-backend`; HEAD giữ nguyên `c2c3c233211ddfaf6b89ba0d5c443618c77b7324`.
- Ban đầu có 10 tracked files modified và các source/test/docs P02 untracked. Đã đọc status/diff và tiếp tục phần đang làm; không reset/clean/ghi đè từ baseline.
- Baseline trước các sửa P02 (lượt làm việc trước): Debug/Release build được, 43 test pass, 1 test fail do yêu cầu prototype Blazor untracked không nằm trong Backend checkout.
- Bản bị ngắt được Owner báo cáo có 9 lỗi: 7 CS0200, 1 CS1503, 1 NETSDK1004. Sau recovery toàn bộ 6 project đều compile Debug và Release.
- Source schema xác nhận scripts khai báo 14 bảng và nâng cấp spec 2.1; chưa kiểm kê database đang triển khai. Không đọc application connection string hoặc mở SQL studio để làm baseline.
- Không sửa frozen Frontend contracts. Prototype và tài liệu Blazor untracked vẫn nằm tại worktree frontend riêng; không sao chép vào backend.

## B. Nguyên nhân chính xác của lỗi build

| Nhóm | Nguyên nhân | Sửa chữa | Trạng thái |
|---|---|---|---|
| 7 × CS0200 | `AppFacade.Session` đã trở thành getter của phiên do Auth cấp nhưng `LoginForm` vẫn gán kết quả login/null/bản `with` vào property | Bỏ gán login; dùng `Auth.SignOut()` ở logout, hủy forced-change, init fail, catch và finally; dùng phiên đã rotate thật sau đổi bắt buộc | FIXED AND VERIFIED: Debug/Release compile |
| 1 × CS1503 | Form còn gửi `TaiKhoanId int`; tham số mới là mật khẩu hiện tại `string`, không phải token hay ID | Form thường thêm ô mật khẩu hiện tại và gọi `ChangePasswordAsync(current,new,confirm)`; forced gọi `CompleteRequiredPasswordChangeAsync(new,confirm)` | FIXED AND VERIFIED: compile và service regression tests; UI tương tác chưa UAT |
| 1 × NETSDK1004 | Project SQL integration vừa thêm vào solution nhưng chưa có assets restore | Chạy `dotnet restore StudioManager.sln`; giữ nguyên project và package versions hiện hữu | FIXED AND VERIFIED: restore tạo assets và cả project compile |
| NU1900 | NuGet không tải được service index/vulnerability data tại nuget.org | Restore thường và thử lại ngoài sandbox; dependencies dùng cache thành công; không tắt audit | BLOCKED: vulnerability feed vẫn không truy cập được; không kết luận package an toàn hoặc có lỗ hổng từ warning này |

Trong lượt recovery có hai CS0103 tạm thời do còn tham chiếu biến diagnostic `stage` sau khi bỏ các assignment; đã thay bằng fixed-stage `Ui.ReportError` và build lại đạt. Ba analyzer warnings xUnit2031 trong SQL test đã sửa bằng overload `Assert.Single` có predicate. Kết quả build cuối chỉ còn 6 NU1900, không còn compiler/analyzer warnings khác được báo.

## C. Security findings và hành vi sau sửa

### SECURITY-01 — Password authorization

Trước: `AuthService.ChangePasswordAsync(int accountId,...)` cho caller chọn account; không xác minh mật khẩu hiện tại. Sau:

- Không còn target account ID ở public self-service API. Account lấy từ phiên nội bộ được auth service cấp.
- Self-service thường yêu cầu mật khẩu hiện tại; forced-first-login là endpoint riêng chỉ chấp nhận restricted session.
- Phiên restricted không gọi được nghiệp vụ thường; không thể dùng endpoint forced để né mật khẩu hiện tại trong ordinary session.
- Admin reset vẫn là operation riêng, kiểm tra current admin, cấm self-reset và buộc đổi khi login tiếp theo.
- SQL đổi mật khẩu so sánh expected hash nhị phân trước update; không ghi khi account khóa/nghỉ hoặc hash đã đổi; update và audit cùng transaction.
- Đổi thành công thu hồi phiên cũ và rotate phiên mới. Logout hoặc login tài khoản khác trong khi đổi mật khẩu không bị thao tác cũ phục hồi/ghi đè session.
- Không đổi format PBKDF2, không sửa ID hay hash đang lưu hàng loạt. Test độc lập xác minh format PBKDF2-SHA256 210000 hiện hữu.

Mức rủi ro ban đầu cao tại Application API. Logic Application đã test bằng stateful double; SQL compare-and-swap chưa được integration-test trên SQL Server.

### SECURITY-02 — Session và authorization

- `AppFacade.Session` chỉ đọc; `AuthService` là đường cấp phiên.
- `InternalAuthorization` lưu token được phát hành, principal chuẩn, thời hạn và credential stamp. Record tự dựng hoặc sửa account/role/token không đạt registry check.
- Kiểm tra account còn hoạt động, role hiện hành, forced state và credential stamp ở mỗi đường service nhạy cảm. Đọc lại sau await để chặn logout/revoke trong lúc chờ account read.
- Generation ngăn kết quả login cũ phát hành session sau logout hoặc login mới.
- Booking, payment/refund, admin CRUD, account operations, reports và backup/restore đều đi qua authorization chung.
- Repository mutations revalidate account/role/hash/forced state dưới transaction. Query account tính cả tình trạng nhân viên nghỉ việc. Backup/restore xác thực admin trước khi chạy lệnh quản trị; các lệnh SQL quản trị không được bao trong business transaction vì cơ chế SQL backup/restore.
- Hủy và sửa lịch kiểm tra lại trạng thái dưới khóa; phân công phòng/thợ kiểm tra trạng thái hoạt động/chức vụ khi ghi.

Giới hạn: đây là authority **trong process nội bộ**, chưa phải Windows Service. Repository vẫn là tầng hạ tầng tin cậy và không phải API cho khách hoặc một security boundary độc lập. Không trình bày token/stamp trong process là bảo vệ chống client bị sửa hoặc người có SQL credentials. Chưa có durable security version để ghi nhận mọi lần khóa/mở hoặc đổi quyền rồi đổi lại giữa hai lượt kiểm tra; phần đó cần thiết kế/migration được duyệt cho authority tương lai.

### SECURITY-03 — Giá lịch sử

`UpdateBookingAsync` trước đây luôn lấy tên/giá gói hiện hành kể cả chỉ sửa giờ. Nay mặc định đọc snapshot đã chốt dưới khóa và giữ nguyên, kể cả gói đã ngừng bán. Đổi gói cần explicit quote acceptance gồm gói/giá gốc mong đợi, giá catalog đã chấp thuận và lý do. Backend so lại giá catalog thật, từ chối báo giá cũ và ghi audit riêng cho đổi gói/chấp nhận báo giá. UI giữ khả năng đổi gói bằng confirmation và reason, không silently reprice.

Policy có 19 regression cases; SQL persistence, financial aggregates, audit và concurrency vẫn chờ integration. Không migration.

### SECURITY-04 — Error boundaries

`SafeDiagnostics` tạo stable code, thông báo Việt an toàn, correlation ID và JSON allowlist. Không serialize exception message, stack, Data, connection/hash/path. Repository trả lỗi an toàn; UI catch paths, initialization và exception dialog không hiển thị raw provider details. Diagnostic sink dùng Trace và log UI; lỗi ghi log không được làm hỏng luồng xử lý lỗi.

Initialization thất bại/hủy thu hồi phiên ngay. Nút retry sau lỗi quay về login để xác thực lại. Đây là thay đổi UX an toàn có chủ đích: không tái sử dụng phiên khởi tạo đã thất bại.

## D. Danh sách file và compatibility

Các đường dẫn trong bảng đều tương đối từ root Backend. Mỗi thay đổi là P02; không có implementation P03.

| File | Trước → sau; mục đích và ảnh hưởng tương thích |
|---|---|
| `StudioManager.sln` | 5 → 6 projects; thêm test project SQL, không thêm web/service runtime |
| `src/StudioManager.Application/Contracts.cs` | Password repository CAS signature; `BookingInput` giữ positional constructor và bổ sung optional quote intent; caller cũ sửa lịch cùng gói vẫn hợp lệ |
| `src/StudioManager.Application/Services/AppFacade.cs` | Setter session tùy ý → getter; chia sẻ một authority giữa các service; callsites đã cập nhật |
| `src/StudioManager.Application/Services/StudioServices.cs` | Đổi mật khẩu có target ID → session-bound ordinary/forced APIs; guards tập trung và revocation; nghiệp vụ nội bộ giữ được đường gọi có quyền |
| `src/StudioManager.Application/InternalAuthorization.cs` (mới) | Issuance registry, expiry/generation/revalidation/revocation; không chọn transport |
| `src/StudioManager.Application/BookingPackageSnapshotPolicy.cs` (mới) | Policy snapshot được dùng cả service và repository; giá do backend quyết định |
| `src/StudioManager.Application/SafeDiagnostics.cs` (mới) | Stable public error + sanitized diagnostic JSON, không thêm dependency |
| `src/StudioManager.Domain/Models.cs` | `UserSession` thêm token/stamp; constructor cũ vẫn compile nhưng record chưa phát hành không được authorize; ToString không in bearer/stamp |
| `src/StudioManager.Infrastructure/SqlStudioRepository.cs` | Transaction account guards, password CAS, eligibility/state checks, giữ snapshot, quote audit và safe errors; không thay schema |
| `src/StudioManager.WinForms/Forms/LoginForm.cs` | Gỡ 7 session writes; lifecycle Auth, phiên rotate thật, cleanup tất cả đường thoát; giữ form đăng nhập |
| `src/StudioManager.WinForms/Forms/ChangePasswordForm.cs` | Tách ordinary/forced; thêm current-password input cho ordinary; không gửi account ID |
| `src/StudioManager.WinForms/Forms/ApplicationInitializationForm.cs` | Safe errors; thu hồi phiên khi fail/cancel; lỗi quay về login thay vì giữ phiên để retry |
| `src/StudioManager.WinForms/Controls/Ui.cs` | Raw exception/stack logs → shared safe message/log, thêm `ReportError` |
| `src/StudioManager.WinForms/Controls/AtelierNotifications.cs` | Exception details pane chỉ nhận sanitized JSON |
| `src/StudioManager.WinForms/AuthVisualAssets.cs` | Không đưa đường dẫn/exception message vào diagnostic lỗi decode |
| `src/StudioManager.WinForms/Pages/BookingsPage.cs` | Explicit quote confirmation/reason và typed quote data; safe catches |
| `src/StudioManager.WinForms/Pages/AccountsPage.cs` | Catch UI dùng safe diagnostic helper; không đổi account layout/chức năng |
| `src/StudioManager.WinForms/Pages/BackupPage.cs` | Catch UI an toàn; không chạy backup/restore trong recovery |
| `src/StudioManager.WinForms/Pages/CrudPage.cs` | Hai catch UI an toàn; giữ CRUD và media layout |
| `src/StudioManager.WinForms/Pages/DashboardPage.cs` | Catch UI an toàn; không đổi công thức KPI |
| `src/StudioManager.WinForms/Pages/ReportsPage.cs` | Catch UI an toàn; không đổi công thức báo cáo |
| `tests/StudioManager.Tests/AccountSecurityServiceTests.cs` | Sửa caller theo current-password signature; giữ validation assertions |
| `tests/StudioManager.Tests/FrontendV22Tests.cs` | Không đòi prototype untracked ở Backend; kiểm tra quyết định Desktop, archive references và không build Web |
| `tests/StudioManager.Tests/BookingPackageSnapshotTests.cs` (mới) | 19 policy cases, gồm giá 0, inactive, stale quote, reason và explicit acceptance |
| `tests/StudioManager.Tests/InternalSecurityRegressionTests.cs` (mới) | 23 Application cases; stateful double, auth/forced/rotation/races và internal business compatibility |
| `tests/StudioManager.Tests/SafeDiagnosticsTests.cs` (mới) | 6 cases bảo vệ payload/path/secret/correlation/stage/cancellation |
| `tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj` (mới) | net8, cùng test-package versions đang dùng, reference Infrastructure |
| `tests/StudioManager.SqlIntegrationTests/SqlTestSafety.cs` (mới) | Exact target/run ID, local integrated auth, explicit modes và schema checksum; default skip |
| `tests/StudioManager.SqlIntegrationTests/SqlTestSafetyTests.cs` (mới) | 16 offline guards/manifest cases; không mở SQL |
| `tests/StudioManager.SqlIntegrationTests/PasswordHashCompatibilityTests.cs` (mới) | 6 cryptographic unit cases trên hasher thật; không dùng account studio |
| `tests/StudioManager.SqlIntegrationTests/VerifiedSqlTestDatabase.cs` (mới) | Bounded metadata identity/version và pre-write schema-shape guards; không provision |
| `tests/StudioManager.SqlIntegrationTests/SqlMetadataIntegrationTests.cs` (mới) | 1 SQL metadata fact, blocked/skip |
| `tests/StudioManager.SqlIntegrationTests/SqlMutationFixture.cs` (mới) | Fixture chỉ trên exact approved disposable DB có markers và schema rỗng; cleanup theo ID sở hữu, không DROP/TRUNCATE |
| `tests/StudioManager.SqlIntegrationTests/RepositoryTransactionIntegrationTests.cs` (mới) | 8 SQL facts cho snapshot, quote, payment/booking concurrency, revoke và CAS; blocked/skip |
| `docs/v2.2/backend/P02_DEPLOYMENT_ADR.md` (mới) | So sánh A/B/C; ghi nhận hướng B, service/transport chưa triển khai |
| `docs/v2.2/backend/P02_CUSTOMER_AND_TRANSACTION_CONTRACTS.md` (mới) | Ownership và transaction design cho phase sau; không claim customer implementation |
| `docs/v2.2/backend/P02_SQL_TEST_STRATEGY.md` (mới) | Approval/provisioning/gates/blocked cases; cảnh báo scripts gốc có `USE StudioManager` |
| `docs/v2.2/backend/P02_RECOVERY_REPORT.md` (mới) | Báo cáo này; kết quả và giới hạn được tách riêng |

Không thay `database/`, WinForms customer contracts, asset JPEG, package versions hoặc file trong worktree Frontend. SQL admin operation và nghiệp vụ thực có thay guard; cần kiểm chứng lại trên schema/permissions deployed trước acceptance.

## E. Verification thực tế

Các command thực thi:

```powershell
dotnet restore StudioManager.sln --nologo
dotnet build StudioManager.sln -c Debug -m:1 -nr:false --no-restore --nologo
dotnet build StudioManager.sln -c Release -m:1 -nr:false --no-restore --nologo
# Chỉ trong process shell kiểm thử; không ghi cấu hình môi trường lâu dài:
$env:PHOENIX_SQL_TEST_MODE = ''
dotnet test StudioManager.sln -c Debug --no-build --no-restore --nologo
dotnet test StudioManager.sln -c Release --no-build --no-restore --nologo
git diff --check
```

| Kiểm tra | Kết quả | Phân loại |
|---|---|---|
| Restore | Success, assets SQL project có; NU1900 vẫn còn sau thử ngoài sandbox | FIXED AND VERIFIED cho restore; audit feed BLOCKED |
| Debug build | 6 project thành công, 0 errors, 6 NU1900 | FIXED AND VERIFIED |
| Release build | 6 project thành công, 0 errors, 6 NU1900 | FIXED AND VERIFIED |
| `StudioManager.Tests` | 92 pass, 0 fail, 0 skip ở cả Debug/Release | FIXED AND VERIFIED — unit/source/policy tests |
| Offline tests trong SQL test project | 22 pass, 0 fail ở cả Debug/Release | FIXED AND VERIFIED — 16 safety + 6 real cryptographic tests, không phải SQL integration |
| SQL facts | 9 skip: 1 metadata + 8 mutation | BLOCKED — chưa có isolated target được duyệt |
| Git whitespace/scope | `git diff --check` đạt; database/contracts Frontend không diff | FIXED AND VERIFIED |
| WinForms interactive + SQL UAT | Không khởi chạy app với DB thật | BLOCKED/NOT COMPLETED |
| Live schema, SQL credentials/permissions, backup restore readiness | Không kiểm tra live | BLOCKED |

Tổng mỗi cấu hình: **114 pass, 0 fail, 9 skip**. Không cộng số Debug/Release thành số test cases khác nhau. Test runner còn thông báo fallback temporary artifacts vì biến `VSTEST_ARTIFACTS_DIRECTORY` không có; không đổi package version chỉ để loại thông báo này.

Hồi quy Application có chứng cứ: ADMIN/STAFF login đúng giữ ID/hash; mật khẩu sai/locked bị từ chối; forged/altered principal bị chặn; stale role/hash/forced/locked/missing bị chặn; ordinary/forced tách biệt; failed password change không ghi; admin reset riêng; session rotate; expiry/logout và các race async; paths CRUD/booking/services/resources/payment/refund/report vẫn được gọi khi có quyền. Đây là repository-double tests, không khẳng định SQL operations đã chạy.

## F. Architecture và an toàn database

Hướng được Owner chọn: cùng máy studio, Windows Service tách quyền. Recovery giữ đây là quyết định kiến trúc; không triển khai service/IPC hoặc P03. ADR giải thích sự khác nhau giữa local và LAN; named pipe mới là đề xuất chưa được nghiệm thu. Các quyết định payment-before-booking, ownership sharing, recovery, media editing, identifier và banking còn cần chốt đúng phase.

- SQL studio được truy cập: **Không**.
- SQL test connection được mở: **Không**; cả 9 SQL facts skip trước fixture.
- SQL writes/migrations/seed/reset/backup/restore đã chạy: **Không**.
- Migration mới được tạo: **Không**.
- Mã test có fixture DML và cleanup có điều kiện, nhưng chưa chạy; không tự tạo/drop DB hoặc chạy scripts schema.
- Chưa có backup được tạo/verify bởi lượt recovery này. Backup readiness vẫn chưa xác minh; mọi execution SQL/schema sau này phải có target, schema, backup/recovery và Owner approval.
- Tài khoản/historical records hiện có không bị backfill, sửa giá, xóa hay reset credential.

## G. Git và bảo vệ worktrees

Không commit/push/merge/đổi branch. HEAD giữ nguyên baseline. Toàn bộ các file mới vẫn untracked để Owner review; danh sách ở mục D gồm tất cả source/tests/docs thay đổi. Build artifacts nằm trong bin/obj bị ignore.

Git diff cuối: 20 tracked files modified, 491 dòng thêm/138 dòng xóa; 18 file mới untracked. `git diff --stat` không bao gồm nội dung file untracked; không diễn giải số dòng tracked thành kích thước toàn bộ P02.

Worktree Frontend vẫn HEAD `c2c3c23`. Trạng thái read-only trước và sau giữ đúng hai mục untracked vốn có: `docs/v2.2/frontend/DEC_01_R2_CSHARP_BLAZOR_ARCHITECTURE.md` và `src/StudioManager.CustomerPortal/`; không sửa hoặc dọn chúng. Không thêm safe.directory toàn cục, chỉ dùng tùy chọn `git -c` cho kiểm tra đọc.

## H. Final Assessment và bước nghiệm thu còn thiếu

| Yêu cầu | Trạng thái và giới hạn |
|---|---|
| Khôi phục 9 build errors, giữ Session read-only | FIXED AND VERIFIED |
| Self-service/forced/admin reset và session issuance/revocation | FIXED AND VERIFIED tại Application; SQL phần triển khai chưa integration-tested |
| Guard transaction, password CAS, eligibility, snapshot SQL và audit | CODE COMPLETE BUT NOT TESTED với SQL Server; policy đã verified |
| Error allowlist/correlation và sửa UI callsites | FIXED AND VERIFIED ở shared helper + compile/source review; UI tương tác chưa UAT |
| SQL test architecture và default refusal | FIXED AND VERIFIED offline; actual SQL BLOCKED |
| ADR và future ownership/transaction contract | DESIGN ONLY cho service/customer boundary; chưa implementation P03 |
| WinForms + internal SQL compatibility toàn tuyến | BLOCKED / NOT COMPLETED |
| Live schema và restore/backup rehearsal | BLOCKED |
| Dependency vulnerability audit | BLOCKED do truy cập NuGet feed |

Không phát hiện regression biên dịch/unit tests còn lại. Các thay đổi hành vi chủ đích cần Owner biết: ordinary change phải nhập mật khẩu hiện tại; init fail trở lại login; gói cũ giữ giá, đổi gói phải chấp nhận báo giá; session giả/forced/stale bị chặn thay vì tiếp tục nghiệp vụ. Chưa đủ chứng cứ để nói không có regression ở runtime/SQL deployed.

Để đóng P02: phê duyệt/provision database test cô lập với schema được review, chạy 9 SQL facts và bổ sung smoke/UAT login–forced-change–ordinary-change–CRUD–booking–payment/refund–report trên dữ liệu test; kiểm kê metadata và backup/restore theo kế hoạch được duyệt. Không dùng studio DB làm fixture. Khôi phục truy cập NuGet audit và xem kết quả độc lập với build/test.

**P02 NOT READY — BLOCKERS REMAIN**. Dừng để Owner review; không bắt đầu P03, không tự thực thi SQL hoặc commit.
