# P04 — Recovery implementation and offline verification

Date: **15/11/2026, Asia/Saigon**. Branch `feature/v2.2-backend`; HEAD remains accepted P03 `5f14c3b`. Existing uncommitted P04 work was retained and completed. No commit/push/merge, migration, live SQL connection/mutation, deployment, installed Service/Client/configuration change or service stop/start occurred.

The recovery sections below are the earlier 15/11 snapshot. The appended **Final blocker remediation verification** section records the later source/tests and current counts; earlier results are not rewritten or presented as reruns.

## Recovery disposition — historical snapshot

| Item | Result |
|---|---|
| Implementation | **PASS — source complete for the scoped P04 workflow** |
| Debug solution rebuild | **PASS — 0 warnings, 0 errors** |
| Release solution rebuild | **PASS — 0 warnings, 0 errors** |
| xUnit, each configuration | **404 passed / 0 failed / 11 live tests skipped** |
| Hidden desktop smoke, each configuration | **9 passed / 0 failed**, counted separately |
| P04 focused tests | **62 passed/configuration**, included in the main suite |
| Existing P03 compatibility | **39 passed/configuration**, included in the main suite; wire shapes retained |
| Original business/security regressions | **72 passed/configuration**, included in the main suite; live SQL legacy acceptance not run |
| SQL review | **REVIEW ONLY**; 87 extracted SQL fragments parsed with zero syntax errors |
| Live P04 SQL/UAT | **NOT EXECUTED** |
| Deployment/runtime acceptance | **NOT PERFORMED / NOT COMPLETE** |

Source completion is not runtime acceptance or a production-readiness claim. The [historical functional audit](P04_FULL_FUNCTIONAL_AUDIT.md) remains unchanged. Current behavior and fixes are documented in [P04_IMPLEMENTATION.md](P04_IMPLEMENTATION.md) and [security/SQL review](P04_SECURITY_AND_SQL_REVIEW.md).

## Executed builds and tests

Evidence directory: **`artifacts/p04-offline/recovery-20261115/`**.

`source-and-build-sha256.json` records the final 34 functional/document candidates and 18 project assemblies; `verification-summary.json` records the four final TRX results and their hashes. These bind the review/evidence to this source snapshot and are not runtime deployment manifests.

| Check | Debug | Release | Durable evidence |
|---|---:|---:|---|
| `dotnet build StudioManager.sln -t:Rebuild ... --no-restore` | 0 warnings/errors | 0 warnings/errors | `build-debug-final.log`, `build-release-final.log` |
| `StudioManager.Tests` | 195 pass, 0 fail, 0 skip | 195 pass, 0 fail, 0 skip | `Debug-final-StudioManager.Tests.trx`, `Release-final-StudioManager.Tests.trx` |
| `StudioManager.SqlIntegrationTests`, offline | 209 pass, 0 fail, 11 skip | 209 pass, 0 fail, 11 skip | `Debug-StudioManager.SqlIntegrationTests.trx`, `Release-final-StudioManager.SqlIntegrationTests.trx` |
| Separate desktop executable | 9 pass | 9 pass | `desktop-debug-attempt1.log`, `desktop-release-final.log` |

The P04/P03/original-business counts are **subsets**, not extra passes to add to 195. The 72 original cases cover `BusinessRulesTests`, `BookingPackageSnapshotTests`, `FinanceAndBackupValidationTests`, `InternalSecurityRegressionTests`, `AccountSecurityServiceTests`, and `MasterDataValidationTests`. The remaining baseline main tests include frontend/source/diagnostic checks. All 133 pre-P04 main cases remain represented in the 195 total.

Each test process explicitly set these environment variables to empty before launching:

```powershell
$env:PHOENIX_SQL_TEST_MODE = ''
$env:PHOENIX_SQL_TEST_CONNECTION_STRING = ''
$env:PHOENIX_SQL_TEST_RUN_ID = ''
```

The SQL project's 11 facts were skipped before live fixture construction. No SQL target was enabled and the privileged developer identity was not used for a live metadata or mutation test.

Executed command shapes, with the configuration selected explicitly and TRX/log output saved under the directory above:

```powershell
dotnet build StudioManager.sln -t:Rebuild -c Debug --no-restore --nologo
dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Debug --no-build --no-restore --nologo --logger 'trx;LogFileName=Debug-final-StudioManager.Tests.trx' --results-directory artifacts/p04-offline/recovery-20261115
dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Debug --no-build --no-restore --nologo --logger 'trx;LogFileName=Debug-StudioManager.SqlIntegrationTests.trx' --results-directory artifacts/p04-offline/recovery-20261115
dotnet build StudioManager.sln -t:Rebuild -c Release --no-restore --nologo
dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Release --no-build --no-restore --nologo --logger 'trx;LogFileName=Release-final-StudioManager.Tests.trx' --results-directory artifacts/p04-offline/recovery-20261115
dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Release --no-build --no-restore --nologo --logger 'trx;LogFileName=Release-final-StudioManager.SqlIntegrationTests.trx' --results-directory artifacts/p04-offline/recovery-20261115
```

First Debug main run: 194 pass / 1 fail. The remaining pipe test still expected the pre-recovery `154.3125` fixture extension; it was corrected to the Owner-approved exact-cent fixture `154.30`, matching the updated fake catalog. The final full Debug and Release runs both passed. Initial TRX/log evidence was retained rather than presented as a final PASS.

The auxiliary desktop project restored the existing dependency graph without new package references. Its NuGet vulnerability metadata refresh could not reach the configured NuGet source and emitted **NU1900**; this warning remains in its restore/build log. Dependency auditing was not disabled, and no package upgrade was introduced. This does not affect the clean full-solution build result, but no fresh successful online vulnerability-audit claim is made. VSTest's existing temporary-artifact fallback notice is also retained.

## Recovery findings and evidence

| Finding | Source status | Executed offline evidence | Still unverified |
|---|---|---|---|
| F-01 principal/cache isolation | FIXED | Actual WinForms A→B, customer→staff, pre-auth clearing, failed staff re-login, stale login/catalog response suppression | Installed client behavior under real Windows users |
| F-02 request/official customer continuity | FIXED | Actual legacy application edit with shared fake store: reassign denies original customer's current view, same-customer schedule edit stays readable, own history/staff access retained; SQL predicate assertion | SQL join/permissions with real reassignment |
| F-03 monetary compatibility | FIXED with explicit Owner rule | Exact-cent and half-cent-boundary rejection, fractional quantities, ledger maximum, historical formula, discount/remainder/overcollection, pre-SQL input guards; actual cent input/basic receipt display | Actual SQL parameter/aggregate behavior and collection/discount/receipt UAT |
| F-04 lock order | FIXED for P04 account/request ordering | Source-order assertions and fake approve↔withdraw terminal-outcome tests; existing fake approval competition tests | SQL deadlocks, range locks, plans and interactions with different staff/legacy/P03 writers |
| F-08 navigation cleanup | FIXED | Actual MainForm repeated navigation; unique owned pipe EOF; client terminal state; delayed mock RPC cancellation and ignored completion | Installed service connection counts and long-running interactive navigation |

The 62 P04 cases comprise existing policy/pipe tests plus recovery tests and SQL structure assertions. Fake transaction tests prove orchestration contracts, not SQL rollback/deadlock properties. P03 authentication, ownership/revocation, session expiry, role isolation, fake-server checks and uncertain-outcome/no-replay regressions remain green.

## Desktop smoke scope

`artifacts/p04-offline/desktop-smoke/` references the actual CustomerClient and staff WinForms projects. It runs on an STA thread, instantiates hidden real controls/forms, invokes their handlers with internal mock transport, and never shows a form, sends real credentials or contacts the production pipe/service/database.

Nine executed cases in each configuration:

1. Native customer/staff initialization and reset.
2. Customer A→B and role switching clear data before authentication.
3. Failed staff re-login leaves the workspace signed out and empty.
4. Reset rejects a delayed catalog result and prevents its follow-up RPC.
5. Disposed controls reject delayed responses.
6. A login completion after sign-out cannot restore profile/contact.
7. Actual MainForm navigation disposes eight removed request pages, preserves the unrelated page lifetime, and closes a unique test-owned pipe to EOF.
8. Page disposal cancels the delayed login token and prevents initialization after completion.
9. Actual staff payment input and basic receipt retain a `0.01` amount.

Recorded per smoke run: 25 mock RPC calls, **0 production-pipe calls, 0 SQL connections, 0 visible forms**. The unique pipe is an offline disposal probe only. This does not replace service/ACL/client deployment UAT.

## SQL syntax and migration review

The installed SQL Server ScriptDom parser processed **87 extracted migration/repository fragments**, including assembled P04 request/catalog queries and monetary SQL literals, with zero syntax failures. The recovery script/result are archived separately from the earlier 31-fragment historical result. No SQL connection, DDL or DML was executed. Extraction is not exhaustive semantic compilation or provider binding.

Migration 07 remains execution-disabled and additive. P02/P03 migration scripts and P03 SQL identity implementation remain unchanged. The runtime permission inventory, transaction order, rollback limits and fixed-service deployment prerequisites are in the security/SQL review.

## File inventory

The implementation retains all recovered P04 components. Full P04 functional candidates relative to HEAD, excluding old P02/P03 material and artifact/tool outputs:

| Area | Files |
|---|---|
| Domain | New `src/StudioManager.Domain/BookingRequests.cs` |
| Application | New `BookingMoneyPolicy.cs`, `BookingRequestContracts.cs`, `BookingRequestPolicy.cs`, `Services/BookingRequestService.cs`; modified `Services/CustomerIdentityService.cs`, `Services/StudioServices.cs`, all under `src/StudioManager.Application/` |
| API | New `src/StudioManager.CustomerApi/BookingProtocol.cs`; modified `IdentityProtocol.cs`, `CustomerPipeClient.cs` in that directory |
| Customer desktop | New `src/StudioManager.CustomerClient/BookingWorkspaceControl.cs`; modified `Program.cs` |
| Service | New `src/StudioManager.IdentityService/IdentityDispatcher.Booking.cs`; modified `IdentityDispatcher.cs`, `Program.cs` |
| Infrastructure | New `src/StudioManager.Infrastructure/SqlBookingRequestRepository.cs`, `SqlStudioRepository.BookingWriter.cs`; modified `SqlStudioRepository.cs` |
| Staff desktop | New `src/StudioManager.WinForms/Pages/BookingRequestsPage.cs`; modified `Forms/MainForm.cs`, `Pages/BookingsPage.cs`, `StudioManager.WinForms.csproj` |
| Tests | New `tests/StudioManager.Tests/BookingRequestFixture.cs`, `BookingRequestTests.cs`, `BookingRequestPipeTests.cs`, `BookingRequestSqlReviewTests.cs`, `P04RecoveryTests.cs`; modified `LocalIdentityPipe.cs` |
| SQL | New `database/07_P04_BookingRequests_REVIEW_ONLY.sql` |
| Documentation | Updated `P04_IMPLEMENTATION.md`; new `P04_SECURITY_AND_SQL_REVIEW.md`, `P04_FINAL_TEST_REPORT.md`, `P04_UAT_CHECKLIST.md`; prior untracked `P04_FULL_FUNCTIONAL_AUDIT.md` preserved unchanged, all under `docs/v2.2/backend/` |

This is **12 tracked modifications and 22 untracked functional/document files**, including the historical audit: 34 candidates relative to the P03 commit. The recovery did not create the historical audit or discard the earlier implementation. Auxiliary smoke tooling and final logs/TRX/parser/manifests remain under P04 artifact directories, not deployment/commit content by implication. The 148 P03 artifacts and five P02 files remain outside the P04 candidate list.

## Recovery remaining risks and exact next gate — historical snapshot

No approved P04 SQL target or actual migration/grant/runtime execution is established by this pass. Current-source SQL binding, restricted permissions, actual atomicity/rollback, cross-writer lock behavior, installed pipe/Windows identity checks and full desktop business UAT remain unverified. The auxiliary dependency metadata refresh limitation is recorded above. Existing historical sub-cent balances require explicit review rather than an invented accounting repair.

**Next safe action:** Owner reviews migration 07, the minimum-permission/maintenance plan and [P04_UAT_CHECKLIST.md](P04_UAT_CHECKLIST.md), names a distinct disposable P04 SQL target, and explicitly approves provisioning/migration plus any later coordinated runtime change. Stop before executing that gate. Do not reuse P02/P03 or production databases, run their mutation harness, bypass SCM, or deploy merely because offline tests passed.

## Final blocker remediation verification — 15/11/2026

Current evidence: **`artifacts/p04-offline/blocker-remediation-20261115-230550/`**. B-01/PF-01 and B-02/PF-02 are fixed in source; PF-04 has conservative uncertain-outcome handling. B-03/PF-03 design is complete, but environment selection/availability requires Owner action. The original [preflight](P04_SQL_DEPLOYMENT_PREFLIGHT.md) and historical functional audit remain unchanged.

| Actual executed check | Debug | Release | Evidence |
|---|---:|---:|---|
| Full solution rebuild | 0 warnings / 0 errors | 0 warnings / 0 errors | `build-Debug-accepted.log`, `build-Release-accepted.log` |
| StudioManager.Tests | **243 PASS / 0 FAIL / 0 SKIP** | **243 PASS / 0 FAIL / 0 SKIP** | `Debug-main-accepted.trx`, `Release-main-accepted.trx` |
| SQL project, explicitly offline | **209 PASS / 0 FAIL / 11 SKIP** | **209 PASS / 0 FAIL / 11 SKIP** | `Debug-sql-offline.trx`, `Release-sql-offline.trx` |
| P04 focused rerun | **110 PASS** | **110 PASS** | `Debug-P04-accepted.trx`, `Release-P04-accepted.trx` |
| P03 security focused rerun | **39 PASS** | **39 PASS** | `Debug-P03-security.trx`, `Release-P03-security.trx` |
| Separate hidden real WinForms smoke | **11 PASS** | **11 PASS** | `desktop-debug-final.log`, `desktop-release.log` |
| ScriptDom SQL syntax | 87 fragments / 0 errors | Configuration-independent | `sql-syntax-review.json` |
| Baseline AST contract comparison | 16 tables / 164 columns / 27 required FKs / 48 required indexes / 0 differences | Configuration-independent | `baseline-contract-review.json` |

Total xUnit **452 PASS / 0 FAIL / 11 SKIP per configuration**; focused counts are subsets, and desktop smoke is separate. Main tests include **48 new blocker-remediation cases**, 62 previous P04 cases and all 133 pre-P04 cases. The raw shared-save fact also exercises package/service × create/update × null/text/binary-float inputs. No live SQL selector was enabled; all 11 live SQL facts remained skipped before fixture construction.

All test/smoke processes explicitly clear `PHOENIX_SQL_TEST_MODE`, `PHOENIX_SQL_TEST_CONNECTION_STRING` and `PHOENIX_SQL_TEST_RUN_ID`. Builds use `dotnet build StudioManager.sln -t:Rebuild -c <Debug|Release> --no-restore --nologo`; tests use the relevant project/configuration with `--no-build --no-restore`, TRX logger and the evidence directory. P04 filter: `FullyQualifiedName~BookingRequest|FullyQualifiedName~P04RecoveryTests|FullyQualifiedName~P04BlockerRemediationTests`. P03 filter covers CustomerIdentityTests, CustomerPipeBoundaryTests, CustomerPipeLifecycleTests and CustomerMigrationSafetyTests. Smoke builds/runs the existing P04-only auxiliary project/DLL, with no restore or production pipe/SQL connection.

New real-control smoke covers both package/service existing-price load/save at 123.45, integer duration, invalid raw 123.456/0.001/overflow/negative price preservation through validation/value reads/spin buttons and save boundary. Both configurations recorded **25 mock RPCs, 0 production-pipe calls, 0 SQL connections and 0 visible forms**.

Initial evidence is retained: the first main Debug run had **239 PASS / 3 FAIL** because a local migration-file assembly mistake temporarily omitted its DDL tail; the syntax parser independently detected unexpected EOF. The original DDL was restored unchanged and final syntax/main runs pass. First desktop retry had 9 PASS/1 FAIL because the new service-price fixture used the package's `Giá` header instead of the actual `Đơn giá`; correcting that fixture yielded 11/11. No failed intermediate run is presented as an initial PASS. A final additional raw shared-save regression brought the accepted main count from 242 to 243 and P04 from 109 to 110.

Auxiliary smoke builds retain **NU1900** because vulnerability metadata from NuGet was unavailable; full solution rebuilds are clean. No audit suppression, dependency upgrade or package installation was introduced. VSTest's existing private temporary-artifact fallback notice is retained separately from test failures.

The AST comparison verifies migration declarations against source, not engine catalog definitions or live gate behavior. Baseline reference fingerprint, restricted grants, actual rollback/commit/locking/timeout behavior, installed binaries/configuration and live UAT remain unverified. See [P04_BLOCKER_REMEDIATION_REPORT.md](P04_BLOCKER_REMEDIATION_REPORT.md) for remaining Owner/execution gates.
