# P03 — Final runtime stabilization

Offline stabilization snapshot: **13/11/2026**, Asia/Saigon. Runtime deployment and Live UAT record: **15/11/2026**, Asia/Saigon. Branch `feature/v2.2-backend`; baseline `603a2e3`. P03 is uncommitted.

Current acceptance: runtime deployment **PASS** and the critical Live UAT sequence **APPROVE → READ → REVOKE → DENY PASS** on the P03 UAT environment. Independent audit-row verification, a post-deployment live idle-timeout test and long-term service stability remain unverified; see Remaining Verification.

The 15/11 results below record the deployment checks and Live UAT/SQL evidence supplied by Owner. This documentation update does not rerun the 13/11 builds/tests, query SQL, operate the service/client or change release artifacts.

## Offline stabilization and demonstrated cause — 13/11/2026

At the close of the 13/11 offline stabilization pass, the stale pipe defect had been reproduced and corrected in source, with offline verification. Live SQL approval/revocation acceptance was still outstanding, and no installed service or deployed client had been changed during that pass. This is the historical snapshot; the 15/11 deployment and Live UAT results below supersede its pending acceptance status.

The old server closed a connection after its two-minute read/command deadline, disposed the stream and cleared the connection's actor. The client retained `_pipe`; checking that field did not establish that the other endpoint was still alive. WinForms still displayed the authenticated staff screen. The next approval tried to write to that dead handle and returned SERVICE_UNAVAILABLE at REQUEST_WRITE before reaching the dispatcher/repository.

The isolated reproduction used the real production client, local pipe, protocol, server handler and dispatcher, with synthetic in-memory accounts/repositories and the **unmodified 120-second production timeout**:

| Observation | Before | After |
|---|---|---|
| Staff login and request list | Success | Success |
| Server idle closure | 120.0 seconds | 120.0 seconds |
| Client stream IsConnected after server closure | True | True; no longer trusted for liveness |
| Next approval | SERVICE_UNAVAILABLE; IDENTITY_COMMAND.REQUEST_WRITE; PIPE_IO | SESSION_EXPIRED before any write |
| Request/repository | PENDING; zero review calls | PENDING; zero review calls |

Evidence is under `artifacts/p03-runtime-stabilization/evidence/`: `before-idle-reproduction.trx`, `after-idle-proof.trx` and their standalone test source snapshots. The two-minute proof tests were archived outside the regular test project; fast regressions cover the same boundary in the normal suite.

This proves a code defect reproducing the reported symptom. It does not establish the elapsed idle time of Owner's specific UAT click or claim that all possible deployed failures are resolved. Consecutive commands without expiry are separately tested through the production connection handler.

The audit also found that processing inherited the time remaining on the idle deadline. A complete frame now starts a separate bounded **30-second request deadline**, so a valid command is not cancelled merely because it arrived near the end of the idle wait. A regression delays repository processing beyond the shortened test idle interval and confirms success.

## Files and methods changed in the offline pass — 13/11/2026

| File | Change |
|---|---|
| `src/StudioManager.CustomerApi/CustomerPipeClient.cs` — SendAsync, ExpireIdleConnectionAsync, ClosePipeAsync, DisposeAsync | Track a conservative monotonic connection lease; discard expired streams before sending; distinguish expiry, transport loss and uncertain mutation outcome; terminal disposal rejects queued/future calls. |
| `src/StudioManager.CustomerApi/IdentityProtocol.cs` | Share finite 120-second server idle, 110-second client lease and 30-second request limits; centralize response codes that require signing in again. DTO/framing format unchanged. |
| `src/StudioManager.IdentityService/IdentityPipeServer.cs` — HandleAsync | Use shared deadlines; reset the deadline after a complete request; close after a session-ending response. Internal test override can only shorten the idle timeout. |
| `src/StudioManager.IdentityService/IdentityDispatcher.cs` — DispatchAsync | Return structured absolute-session expiry before executing a protected command; apply existing sanitized SQL classifications to the whole ownership group. |
| `src/StudioManager.Application/Services/CustomerIdentityService.cs` — IsSessionExpired | Expose only the server-side expiry predicate to the dispatcher; no identity/session data crosses the pipe. |
| `src/StudioManager.CustomerClient/Program.cs` — timer, Execute, Show, SignedOut and async continuations | Proactively sign out on idle expiry; prevent overlapping UI actions; ignore callbacks after disposal; display the full result warning. |
| `tests/StudioManager.Tests/CustomerPipeLifecycleTests.cs` | Eight focused regression cases for idle expiry/relogin, full ownership flow for both staff roles, separate deadlines, absolute expiry, lost replies and disposal races. |
| `tests/StudioManager.Tests/LocalIdentityPipe.cs` | Small SQL-free local-pipe fixture and monotonic test clock. Test-owned pipe injection does not alter production SCM verification. |
| `tests/StudioManager.Tests/CustomerPipeBoundaryTests.cs` | Adapt existing tests to explicit session closure/relogin and the new transport outcome codes. |
| `tests/StudioManager.Tests/CustomerIdentityTests.cs` | Test-only delay/failure injection; align fake repository business error codes with the existing SQL implementation. |
| P03 implementation/runtime documents | Link the current report and retain earlier reports as history. |

`SqlCustomerIdentityRepository.cs` is unchanged; its hash matches the previous review manifest. No schema, migration, SQL grants, account/password, connection settings, TLS, package dependencies, P02 source or Frontend source were changed.

## Connection and session behavior established offline — 13/11/2026

- The client measures its lease with a monotonic clock starting **before** each outbound request. It expires at 110 seconds, conservatively before the server's 120-second idle window. Processing/response time counts toward this client lease. This small margin is intentional; neither timeout is disabled or extended.
- WinForms checks expiry every second without issuing SQL/pipe keepalives. It clears authenticated controls and requests login. Every SendAsync repeats the check under the same semaphore, covering delayed UI timers, queued actions and resume after a pause.
- Only an explicit login/registration may establish a new connection after session loss. Every new production connection still verifies the server PID against SCM before sending credentials. No credentials or actor are replayed automatically; the service creates a new private authority for that pipe.
- Logout disposes the pipe, making its actor unreachable; server cleanup clears it. Successful password change, failed authorization, rate limiting, protocol errors and service errors consistently end the relevant connection. Absolute eight-hour authentication expiry is still enforced inside the service.
- Ordinary business errors such as INVALID_INPUT, NOT_FOUND, LINK_EXISTS, LINK_STATE_CHANGED and OWNERSHIP_CONFLICT retain the live connection. SQL failures remain sanitized SERVICE_ERROR responses with fixed ownership categories.
- Expiry detected before sending returns SESSION_EXPIRED and states that the request was not sent. Unexpected transport loss during a read-only command returns CONNECTION_LOST. A failed write/read after attempting a persistent mutation returns OUTCOME_UNKNOWN, instructing the operator to log in and reload state before another action. It never retries the mutation.
- ConnectLocalPipeAsync retries only opening a handle before an application payload is sent. Its individual access rights, anonymous SQOS, local-only path, SCM check, server SID/group ACL, NETWORK deny and FirstPipeInstance protections remain unchanged.
- DisposeAsync marks the client closed before waiting for an active RPC. Queued/future calls return CLIENT_CLOSED instead of reopening a connection. The UI busy guard prevents overlapping commands and expiry checks.

The application still enforces actor ownership and roles in the backend. No customer-supplied account/KhachHang ID becomes authorization authority, and no contact match grants ownership.

## Offline build and regression verification — 13/11/2026

| Final check | Debug | Release |
|---|---:|---:|
| Full solution rebuild | 0 warnings, 0 errors | 0 warnings, 0 errors |
| Main tests | 133 passed | 133 passed |
| P03 subset included above | 39 passed | 39 passed |
| Existing SQL-project offline tests | 209 passed, 11 live tests skipped | 209 passed, 11 live tests skipped |
| Total | **342 passed, 0 failed** | **342 passed, 0 failed** |

These are the build/test results recorded on 13/11; they were not rerun for the 15/11 deployment/UAT report update. Final TRX files use the `final-Debug-*` / `final-Release-*` names in the evidence folder. Each test process explicitly cleared PHOENIX_SQL_TEST_MODE, PHOENIX_SQL_TEST_CONNECTION_STRING and PHOENIX_SQL_TEST_RUN_ID. No live SQL test or database mutation was run during that offline pass. The existing temporary-artifact SDK notice is informational.

Focused cases cover:

1. Both a remotely closed idle connection and proactive client expiry: an approval is not sent, then explicit fresh staff login approves exactly once.
2. STAFF and ADMIN: customer request/status, unavailable history before approval, explicitly selected existing customer, business error without losing the pipe, approval, owner read, competing owner rejection, revocation taking effect on the next read, rejection/resubmission and retained history.
3. A complete request has its own processing deadline.
4. Absolute staff-session expiry is rejected before repository review.
5. Approval commits but its response is lost: OUTCOME_UNKNOWN, one review call, no replay; fresh login reloads APPROVED.
6. Dispose during an RPC rejects a queued login and cannot reopen the pipe.

The 13/11 registration/login, profile, password-change/invalidation, locked accounts, wrong passwords, redaction, service-process rejection, pipe ACL and unchanged internal ADMIN/STAFF regressions all passed. Those fake-repository tests did not establish live SQL transaction/audit persistence. The source analysis recorded that existing SQL updates review/revocation metadata and NhatKyHeThong atomically and retains the unique ownership constraints/history. The 15/11 SQL evidence below confirms ownership metadata; independent verification of the corresponding NhatKyHeThong rows is still missing.

## Release artifacts prepared — 13/11/2026

Artifact folder prepared during the offline pass: `artifacts/p03-runtime-stabilization/`.

- `service/` and `client/`: complete matching framework-dependent Release packages.
- `SHA256.json`: package, relevant source and evidence hashes.
- `P03_Runtime_Stabilization_Release.zip` and its `.sha256` file: portable service/client bundle, manifest and the 13/11 report snapshot.
- `before/` and `evidence/`: review snapshots and test evidence; not installed/deployed.

Runtime binaries changed in that pass: CustomerApi.dll in both packages; Application.dll and IdentityService.dll in the service package; CustomerClient.dll in the client package. The delivery plan required complete matching packages and staged assemblies matching the final workspace Release hashes. The client package has only CustomerClient.dll and CustomerApi.dll, without SqlClient, Infrastructure or SQL configuration.

Historical preparation commands, executed on 13/11 only inside the Backend workspace and retained for traceability; they were not rerun for this documentation update:

```powershell
dotnet publish src/StudioManager.IdentityService/StudioManager.IdentityService.csproj -c Release --no-build --no-restore --self-contained false -o artifacts/p03-runtime-stabilization/service
dotnet publish src/StudioManager.CustomerClient/StudioManager.CustomerClient.csproj -c Release --no-build --no-restore --self-contained false -o artifacts/p03-runtime-stabilization/client
```

The ZIP's REVIEW.md remains the original 13/11 snapshot. This repository report now includes the 15/11 acceptance record; the ZIP, manifest and other artifacts have not been regenerated or modified.

## Runtime deployment acceptance — 15/11/2026

Owner supplied the following completed deployment results:

| Check | Recorded result |
|---|---|
| Staging | **PASS — 63/63 files**, comprising 61 runtime files and 2 metadata files |
| Staging directory | `G:\AnhStudio_Modern_v2\P03_Runtime_Staging\P03_Release_20261115-142014-843` |
| Runtime package | **61 files** |
| Pre-deploy backup | `C:\ProgramData\StudioManager\P03-Backups\runtime-20261115-145404` |
| Backup verification | **PASS — 61 files** |
| File deployment | **PASS — 61/61 runtime files** |
| Windows Service | `StudioManagerIdentity` — **Running** after startup and still present at the five-second check |
| PID at acceptance check | `13460` — an observation at that time, not a fixed or expected PID for later starts |
| Service identity | `NT SERVICE\StudioManagerIdentity` |
| Protected configuration | `C:\ProgramData\StudioManager\IdentityService\service.json` — unchanged |

Approved release ZIP SHA256:

```text
0C017F835E5C00C8D47AFA74C84DA92157A7FF55604EAFE93FA8DBED62D7B330
```

Recorded service.json SHA256 **before and after deployment**, identical:

```text
1CBFD2EE470AFC0695DAC29D5367AE4D9B17A5BCBDCA8D9AF64C4163814C41F5
```

The startup check establishes successful startup and presence after five seconds only. It does not establish long-term runtime stability.

## Live UAT acceptance — 15/11/2026

Environment: SQL instance `MSI\SQLEXPRESS`, database `StudioManager_P03_Test`. Customer account: `p03test01@example.com`. Staff account: `p03reviewer`. No passwords, connection-string contents, keys or tokens are recorded here.

The accepted ownership request identifies:

| Field | Value |
|---|---|
| SoHuuKhachHangId | `1` |
| TaiKhoanKhachHangId | `1` |
| KhachHangId | `1` |
| MaKhachHang | `KH900001` |

| Step | Observed scenario | Result |
|---|---|---|
| 1 | CustomerClient ran under a non-Administrator Windows UAT account | **PASS** |
| 2 | Staff signed in and saw request ID 1 in PENDING state | **PASS** |
| 3 | Staff performed APPROVE successfully | **PASS** |
| 4 | SQL confirmed APPROVED and the approval metadata recorded below | **PASS** |
| 5 | Customer signed in and successfully read studio profile KH900001 | **PASS** |
| 6 | Staff confirmed APPROVED state and performed REVOKE | **PASS** |
| 7 | SQL confirmed REVOKED and the revocation metadata recorded below | **PASS** |
| 8 | After REVOKE, the customer could no longer read the studio profile | **PASS** |

SQL confirmation supplied for the approval:

| Field | Recorded value |
|---|---|
| TrangThai | `APPROVED` |
| KhachHangId | `1` |
| ReviewedBy | `1` |
| ReviewedAtUtc | `2026-11-15 08:21:39` |

SQL confirmation supplied for the revocation:

| Field | Recorded value |
|---|---|
| TrangThai | `REVOKED` |
| RevokedBy | `1` |
| RevokedAtUtc | `2026-11-15 08:30:46` |
| RevokeReason | Non-empty text describing the P03 test; the exact text is not reproduced |

ReviewedAtUtc and RevokedAtUtc above are **UTC**, not Asia/Saigon local times. The critical live sequence **APPROVE → READ → REVOKE → DENY passed** in the stated UAT environment. These observations do not imply that other live scenarios or audit-table contents were independently verified.

## Historical deployment and rollback runbook — 13/11/2026

The following is the original approval-gated plan recorded on 13/11, retained as historical procedure. It is not a statement that deployment or critical ownership UAT is still pending; their completed 15/11 results are recorded above. It does not authorize any new runtime or SQL action during this documentation update.

The original administrator procedure was:

1. Verify the bundle/manifest. Locate the existing registered service executable through SCM. Preserve the supplied backup `C:\ProgramData\StudioManager\P03-Backups\backup-20261113-164125`; take a **new**, access-controlled backup of the currently installed complete service package, `G:\AnhStudio_Modern_v2\P03_Publish\Client`, protected service.json and existing service/config ACLs. Do not overwrite the earlier backup or copy credentials/configuration into the review bundle.
2. Close CustomerClient. Stop **only StudioManagerIdentity**, wait for Stopped, and copy the matching service/client packages into the existing installation locations. Preserve service registration, identity, SID/client group, directory ACLs and protected service.json. No SQL Server restart, schema/grant change, account change or blank template replacement is required.
3. Compare installed service/client files with SHA256.json; start the same service and confirm Running. Launch the updated client as MSI\P03ClientTester. Every prior pipe session must log in again.
4. Approve a narrowly scoped live UAT on **MSI\SQLEXPRESS / StudioManager_P03_Test only**, through the existing restricted service identity and the existing p03reviewer/customer accounts: reload request 1, verify it is still PENDING, explicitly select existing KhachHangId 1 and record the real UAT review reason; approve; verify the correct customer can read its profile; revoke with a reason; verify the next customer read is denied. This writes the ownership decision/revocation metadata and the corresponding existing audit rows. Do not change passwords, create accounts or run P02 mutation tests. If the initial state has changed, reload and reassess before any write.
5. Verify persisted APPROVED/REVOKED metadata and audit history with an Owner-controlled read-only check. Also leave the client idle past the lease once: the UI must explicitly require login without attempting a queued approval. This idle check itself performs no SQL write.

The operational rule remains: if any response is OUTCOME_UNKNOWN, sign in and reload status before deciding on another mutation. No automatic retry, SQL repair, grant expansion or migration is proposed.

Rollback, within the same approved maintenance window: close clients, stop only StudioManagerIdentity, restore the **new** pre-update complete packages/configuration with their protected ACLs, restart that service and sign in again. Roll back binaries only; do not restore/reset the SQL database or delete ownership/audit history. Confirm current link state through read-only inspection before continuing UAT.

## Remaining Verification

- **Independent audit-row verification remains unverified.** No independent SQL query evidence has been supplied for the corresponding NhatKyHeThong records. Source-level transaction behavior and ownership-table confirmation do not establish that those specific audit rows were independently checked.
- **Post-deployment live idle-timeout behavior remains unverified.** A live idle-timeout test was not performed after the new deployment. The successful 13/11 offline reproduction/regressions do not substitute for this remaining live check.
- **Long-term service stability remains unverified.** Startup and presence after five seconds passed; no extended observation or long-duration stability test is established by that check.

The 110-second client lease remains deliberately conservative. Unexpected lost responses can still leave an uncertain transaction outcome, which is reported rather than retried. Any new SQL error must be assessed from the sanitized ownership category instead of granting broader privileges.

## Current conclusion — 15/11/2026

Runtime deployment **PASS** and the critical Live UAT sequence **APPROVE → READ → REVOKE → DENY PASS** are recorded for the specified P03 UAT environment, based on Owner-supplied deployment and SQL/UAT evidence. The three Remaining Verification items above are still open; this is not a claim that all verification is complete or that production readiness has been established.

The 13/11 technical history and build/test evidence remain historical results, not tests rerun on 15/11. This report update changes only the Markdown record; it does not modify source/tests, database, service/client, artifacts or the Git index. Commit/push/merge remain subject to separate review and approval.
