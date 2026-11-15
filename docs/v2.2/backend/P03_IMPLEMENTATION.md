# P03 — Customer identity implementation and review

Ngày 13/11/2026 (Asia/Saigon). Baseline: 603a2e3, feature/v2.2-backend.
Status: Owner reports identity/profile/password, SQL PENDING ownership submission and staff review-list UAT passing. Staff approval is the remaining live blocker. The stale pipe defect has now been reproduced and corrected offline; see [P03 final runtime stabilization](P03_FINAL_RUNTIME_STABILIZATION.md) for evidence, final verification, Release packages and the Owner deployment/UAT gate. Codex has not executed migrations or live SQL acceptance.

## Delivered behavior

- Separate customer accounts, registration, email login, logout and current-password-protected password change.
- Own contact profile read/update; immutable normalized login email is separate from editable contact email.
- Pending ownership requests contain only customer-supplied references/evidence. Registration/profile changes never query or match historical KhachHang by email/phone.
- Active ADMIN/STAFF (not locked, not forced-change, employee still active) may approve/reject/revoke. Approval requires an explicitly selected existing KhachHangId and a recorded reason.
- One approved owner per KhachHang and one approved business profile per account; filtered unique indexes enforce this conservative P03 policy. Rejected/revoked requests remain in history. Account can submit a new request afterward.
- Revocation takes effect on the next owner query. No profile endpoint accepts an account ID; owner queries derive it from the service connection.
- Password change uses expected credentials/version and rotates a durable GUID SecurityVersion; every old customer session is rejected on its next call. Successful password change signs out the initiating connection too.
- Staff decisions store reviewer/time/reason, retain approval and revocation metadata, and append existing NhatKyHeThong in the same SQL transaction.
- No customer booking/payment/album operations, recovery/OTP claim, or modifications to the existing staff/admin desktop/auth/business code.

## Authority boundary

StudioManager.IdentityService is the Windows Service authority selected by the P02 ADR. Each pipe connection gets a separate CustomerIdentityService/dispatcher with private actor state. No actor, role, password hash or session object is accepted from the client. JSON rejects unknown properties and unknown commands; staff-only operations revalidate actual staff credentials.

StudioManager.CustomerClient is a small native WinForms application for P03 login/registration/profile/password/link request and staff review. It references only StudioManager.CustomerApi. Its build output contains CustomerClient.dll and CustomerApi.dll, no Infrastructure/SqlClient assembly or SQL configuration. The prior customer simulation and staff desktop are left unchanged; real P03 customer use starts this separate client.

Local pipe StudioManager.Identity.v1 has an explicit DACL: service identity full control, reviewed client group read/write/synchronize, NETWORK denied, no client CreateNewInstance/ChangePermissions/TakeOwnership. FirstPipeInstance refuses an existing name and a listener remains open while accepted clients are handled. Sixteen active connections, bounded frames, command serialization, timeouts and bounded login/registration budgets limit work. Exceeding the login budget clears connection authentication.

Before credentials are sent, the client compares the pipe server PID against the running service registered as StudioManagerIdentity in SCM. A fake/unregistered server is refused. Client opens only the local pipe, with individual access rights and anonymous SQOS; it never grants a server Windows impersonation rights. This follows [Microsoft named-pipe access guidance](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights); generic write rights can include pipe creation.

The service runs as a dedicated non-administrator Windows identity, holds Windows-integrated SQL access, and reads protected configuration only in its own process. It refuses interactive launch, empty/unreviewed SQL configuration and unencrypted SQL settings. It never reads WinForms appsettings as fallback. No TCP/web endpoint or extra server framework is introduced.

This boundary is for the same-machine topology. Local administrators remain trusted. Each customer Windows identity must lack SQL access and write access to service binaries/configuration; OS logon isolation is required between mutually untrusted users. The trusted legacy staff desktop still uses its existing internal SQL composition; P03 exposes no general staff repository operations to the customer pipe.

## Migration for review — not executed

File: database/06_P03_CustomerIdentity_REVIEW_ONLY.sql

Exactly two additive tables:

| Table | Purpose |
|---|---|
| dbo.TaiKhoanKhachHang | Login email/hash, active flag/security version, own contact fields and timestamps |
| dbo.SoHuuKhachHang | Request/evidence, controlled KhachHang FK, pending/approved/rejected/revoked state and reviewer/revocation metadata |

No duplicate business tables, contact matching, data migration, seed, SQL login/grant, database creation or production configuration change. TaiKhoan remains staff/admin only.

The script is a single guarded transaction with @OwnerApprovedExecution=0 and @ExpectedDatabaseName=NULL. Owner must approve the actual instance/database and an execution copy before enabling those values; keep the repository template disabled. It refuses system DBs, a missing baseline and pre-existing P03 objects. No USE/GO or reset/drop is included.

Review FKs/constraints/indexes before execution. Legacy KhachHang rows referenced by ownership history cannot be deleted while the FK remains; preserve these records rather than removing their ownership history to force deletion. No existing business writer is changed.

## Installation/deployment prerequisites — approval pending

1. Owner chooses an approved non-production acceptance database with the business baseline and reviews backup/recovery plus the migration execution copy. Do not reuse P02 mutation fixtures automatically. No migration has been run here.
2. Ensure the .NET 8 runtime (service) and .NET 8 Windows Desktop runtime (client) are available. Publish the service and client separately into protected installation folders. Local publish commands below prepare files only; they do not install/start a service:

~~~powershell
dotnet publish src/StudioManager.IdentityService/StudioManager.IdentityService.csproj -c Release --no-restore --self-contained false -o artifacts/p03/service
dotnet publish src/StudioManager.CustomerClient/StudioManager.CustomerClient.csproj -c Release --no-restore --self-contained false -o artifacts/p03/client
~~~

3. After explicit Owner approval, an administrator registers an own-process Windows Service named StudioManagerIdentity using a dedicated non-admin virtual/service account (suggested NT SERVICE\StudioManagerIdentity). Configure its binary path with quotes, service SID and read-only binary/config ACLs. Do not run as developer, LocalSystem or an administrator.
4. Create/review a dedicated local Windows client group and membership. Place a protected copy of p03-service.template.json at %ProgramData%\StudioManager\IdentityService\service.json, populated with its group SID and the approved Windows-integrated encrypted SQL connection string. Only administrators/SYSTEM/service identity may read or change it; customer users may not modify service settings or binaries. No passwords belong in the client.
5. Provision a SQL login/user for the service identity only on the approved target. Minimum requested rights: SELECT on TaiKhoan/NhanVien/KhachHang; SELECT/INSERT/UPDATE on TaiKhoanKhachHang and SoHuuKhachHang; INSERT on NhatKyHeThong. No DELETE, DDL, backup/restore, db_owner, sysadmin or server-wide write rights. Grant/provisioning actions require separate approval and have not been executed.
6. Start the registered service only after migration/configuration/permissions review. Launch StudioManager.CustomerClient.exe under a client Windows identity with no SQL rights. The service must be RUNNING in SCM before the client accepts it.

Account locking remains controlled administrative data maintenance: lock and rotate SecurityVersion atomically when revoking customer sessions. P03 does not add a customer-account admin/reset/recovery screen.

## Straightforward acceptance after approval

Use synthetic data only, one A/B pair of customer accounts and one active ADMIN/STAFF account.

1. Register A/B with different login emails but identical contact phone/email; verify zero ownership grants and no changes to KhachHang.
2. Login correctly, reject wrong password/locked account; logout and retry a profile read. Change password with wrong then correct current password; old password and other old connections must fail.
3. Update A contact profile; B and legacy KhachHang remain unchanged. A-supplied CustomerId/LinkId on a profile command cannot select B.
4. Submit a pending legacy reference. A cannot call review commands or read candidate history; staff reviews evidence using the existing internal customer workspace, selects the actual KhachHangId and supplies a reason.
5. Approve, verify A can read only its linked customer contact, reject B competing for the same active owner, then revoke and verify A loses access on the next call. Test rejection and resubmission.
6. Lock/reset/change role of the staff account between login and a review; backend must deny. Force-change staff must complete its existing desktop flow first.
7. Close/restart service and verify old pipe sessions do not resume. Test ordinary client credentials, protected configuration ACL, blocked remote pipe access and server identity verification under actual separate Windows identities.
8. Run unchanged internal ADMIN/STAFF login and existing business smoke checks. Customer client contains no booking/payment/media functionality in P03.

Review commands are state-dependent; retry after uncertain outcome must reload the request state instead of assuming failure. Profile writes are constrained to the authenticated row; SQL decisions/password changes have compare/state guards. This is not a request/payment idempotency implementation.

## Verification and current limits

Debug/Release builds and all unit tests are required. Focused tests use the real PBKDF2 implementation and isolated in-memory repositories; pipe tests use actual unique local pipes and the production framing/dispatcher/access helpers, with no SQL connection. Existing P02 SQL facts remain disabled; only their offline tests run.

Actual Debug and Release results, each configuration: solution build 0 warnings/errors; main test project 119 passed (94 existing + 25 P03); SQL-project offline 209 passed, 11 live SQL facts skipped. Total 328 passed, 0 failed, 11 skipped. NuGet vulnerability audit for all nine projects completed without reported vulnerable dependencies. The ordinary test runner temporary-artifact fallback notice remains and did not fail tests.

NuGet restore/audit uses the standard source; no audit/TLS suppression or package upgrades to the old projects. One framework-matched ServiceController package supports the Windows service lifecycle; no ASP.NET/HTTP/ORM/DI framework is added. Initial sandbox network access was refused; restore/audit succeeded through the approved network execution path. SQL was never contacted.

Not yet verified: migration compilation/execution on SQL Server, SQL concurrency/rollback/permission behavior, installed SCM lifecycle under the real service account, client group/SQL isolation across real Windows accounts, interactive WinForms UAT. These are deployment acceptance dependencies, not claims inferred from green unit tests. No Windows account/group/service/login, SQL grant, database/schema, migration, commit/push/merge was executed during implementation.

## File inventory

- StudioManager.sln
- database/06_P03_CustomerIdentity_REVIEW_ONLY.sql
- docs/v2.2/backend/p03-service.template.json and P03_IMPLEMENTATION.md
- src/StudioManager.Domain/CustomerIdentity.cs
- src/StudioManager.Application/CustomerIdentityContracts.cs
- src/StudioManager.Application/Services/CustomerIdentityService.cs
- src/StudioManager.Infrastructure/SqlCustomerIdentityRepository.cs
- src/StudioManager.CustomerApi/{StudioManager.CustomerApi.csproj,IdentityProtocol.cs,CustomerPipeClient.cs,AssemblyInfo.cs}
- src/StudioManager.IdentityService/{StudioManager.IdentityService.csproj,Program.cs,IdentityPipeServer.cs,IdentityDispatcher.cs,AssemblyInfo.cs}
- src/StudioManager.CustomerClient/{StudioManager.CustomerClient.csproj,Program.cs}
- tests/StudioManager.Tests/StudioManager.Tests.csproj
- tests/StudioManager.Tests/{CustomerIdentityTests.cs,CustomerPipeBoundaryTests.cs,CustomerMigrationSafetyTests.cs}

Pre-existing .p02-approved and P02 patch artifacts are preserved. Frozen Frontend worktree is untouched. No customer request/payment/album workflow is implemented.
