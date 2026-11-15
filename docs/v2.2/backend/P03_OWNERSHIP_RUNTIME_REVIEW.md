# P03 ownership request — runtime review

Historical report. Owner subsequently accepted SQL PENDING submission; the staff approval blocker has a demonstrated stale-pipe defect. See [P03 final runtime stabilization](P03_FINAL_RUNTIME_STABILIZATION.md) for the current source fix, evidence, verification and deployment/UAT gate.

Baseline: `603a2e3`, branch `feature/v2.2-backend`. P03 remains uncommitted.

## Conclusion and evidence

The live failure's root cause is **not established**. This change corrects the loss of diagnostic information; it does not claim to fix SQL persistence or complete live acceptance.

Owner reports registration, login, profile read/update and password rotation passing against the deployed service. Owner clarified that the ownership error appears immediately after recent successful activity. The two-minute pipe idle timeout is therefore not the leading explanation; timeout settings are unchanged.

The request traverses:

`IdentityForm.BuildProfile` → `IdentityRequest(RequestLink)` with Reference/Evidence → `CustomerPipeClient.SendAsync` → length-prefixed `IdentityProtocol` JSON → `IdentityPipeServer.HandleAsync` → `IdentityDispatcher.DispatchAsync` → `CustomerIdentityService.RequestLinkAsync` → `SqlCustomerIdentityRepository.RequestLinkAsync` → response on the same connection. After success, the UI immediately sends **GetMyLink** through `RefreshLink`; a failure of this second command can replace the initial success message.

The SQL path validates current account credentials under transaction locks, checks for an existing PENDING/APPROVED link, inserts the authenticated account's reference/evidence, and commits. The migration's existing default makes the new row PENDING with no KhachHang target. It does not resolve the submitted reference into ownership.

Offline evidence:

- The supplied Vietnamese evidence fits the protocol and service validation. A real local-pipe exchange using production `CustomerPipeClient.SendAsync`, framing, handler, dispatcher and authority succeeds after password change/relogin. The in-memory repository records PENDING for account A despite a supplied CustomerId belonging to B; status reads succeed, historical data remains unavailable and self-approval/revocation are denied.
- An actual `SqlException` object with error 229, constructed entirely in memory, returns a sanitized **SERVICE_ERROR**; the pipe remains usable for login and retry. An unexpected repository exception has the same structured response behavior. Neither is rewritten into SERVICE_UNAVAILABLE.
- Closed transport and invalid response JSON are caught by the client and return sanitized SERVICE_UNAVAILABLE with fixed operation/stage/reason identifiers.
- SCM read-only inspection confirmed StudioManagerIdentity Running. Installed assembly hashes could not be read because the installation directory's Windows ACL denied access. A mismatch between deployed and workspace assemblies has not been ruled out.

The remaining uncertainty is whether the deployed failure occurs while writing RequestLink, reading its response, or reading the automatic GetMyLink response; the old generic message cannot distinguish these. A deployed SQL/permission failure is also unconfirmed. No SQL grants or schema changes are justified by the available evidence.

## Focused source changes

| File / method | Change |
|---|---|
| CustomerApi/CustomerPipeClient.cs — SendAsync | Keeps SERVICE_UNAVAILABLE and connection disposal; appends only fixed operation, stage and reason identifiers. No raw exception information. |
| IdentityService/IdentityDispatcher.cs — DispatchAsync catch | Keeps logout and SERVICE_ERROR; adds fixed ownership failure categories and passes the numeric SQL provider code to the existing SafeDiagnostics facility. |
| IdentityService/IdentityPipeServer.cs — HandleAsync | Changes visibility from private to internal so tests invoke the actual lifecycle. No behavior change. |
| Tests/CustomerPipeBoundaryTests.cs | Six additional cases: full ownership exchange after password rotation, SQL permission failure, unexpected repository failure, closed write, closed response and invalid status response. |
| Tests/CustomerIdentityTests.cs — Store | Test-only failure injection for RequestLink. |

No changes to CustomerIdentityService, SqlCustomerIdentityRepository, IdentityProtocol DTOs/framing, migration, schema, grants, authentication/ownership guards, TLS configuration or timeout values.

Client identifiers are an allowlist:

- Operation: OWNERSHIP_REQUEST, OWNERSHIP_STATUS, IDENTITY_COMMAND.
- Stage: CONNECTION_OPEN, SERVICE_IDENTITY, REQUEST_WRITE, RESPONSE_READ.
- Reason: INVALID_MESSAGE, PIPE_CLOSED, TIMEOUT, PIPE_IO, CLIENT_FAILURE.

For example, `OWNERSHIP_STATUS.RESPONSE_READ; INVALID_MESSAGE` identifies the follow-up status response, not the INSERT. `OWNERSHIP_REQUEST.RESPONSE_READ; PIPE_CLOSED` identifies a disconnected exchange and does not prove the transaction failed. Reload current state after reauthentication before resubmitting an uncertain write.

Service identifiers for RequestLink exceptions: OWNERSHIP_SQL_PERMISSION (229), OWNERSHIP_SQL_TIMEOUT (-2/1205/1222), OWNERSHIP_SQL_EXECUTION (other SQL exceptions), OWNERSHIP_ACTOR_REVALIDATION, OWNERSHIP_REQUEST_HANDLER. All preserve SERVICE_ERROR and session invalidation. Client-visible errors contain no provider messages, SQL text, account/contact values, paths, credentials or connection strings.

## Ownership review and verification

Existing source retains staff account/role/active/password revalidation and requires an existing explicitly selected KhachHangId for approval. Review/revocation update metadata and append NhatKyHeThong within the same transaction. Unique filtered indexes prevent a second approved owner. Rejected/revoked rows are retained; owner reads require the current APPROVED state on every call.

Existing ADMIN/STAFF approval/rejection/revocation, competing-owner, history, wrong-password, locked-account and customer-isolation unit tests pass. SQL locking, audit persistence and real staff review remain live UAT items; no in-memory test is presented as SQL acceptance.

Final verification, each configuration:

| Check | Debug | Release |
|---|---:|---:|
| Full solution rebuild | 0 warnings / 0 errors | 0 warnings / 0 errors |
| Main unit tests | 125 passed | 125 passed |
| P03 subset within main tests | 31 passed | 31 passed |
| Existing SQL-project offline tests | 209 passed; 11 live tests skipped | 209 passed; 11 live tests skipped |
| Total | 334 passed; 0 failed | 334 passed; 0 failed |

All test processes explicitly cleared PHOENIX_SQL_TEST_MODE, PHOENIX_SQL_TEST_CONNECTION_STRING and PHOENIX_SQL_TEST_RUN_ID. No live SQL test or SQL mutation was executed. The existing test runner temporary-artifact fallback notice is informational.

## Prepared package and Owner deployment gate

Reviewed workspace artifacts:

- `artifacts/p03-ownership-review/service/`: complete framework-dependent Release service package.
- `artifacts/p03-ownership-review/client/`: complete framework-dependent Release CustomerClient package.
- `artifacts/p03-ownership-review/SHA256.json`: published file hashes plus relevant source hashes.

The changed runtime binaries are **StudioManager.CustomerApi.dll** (both packages) and **StudioManager.IdentityService.dll** (service). Deploy the complete matching packages. The client still contains only CustomerClient.dll and CustomerApi.dll, with no SqlClient/Infrastructure assembly or SQL configuration. Published service assemblies match the workspace Release build hashes.

Reproduction of package preparation only:

```powershell
dotnet publish src/StudioManager.IdentityService/StudioManager.IdentityService.csproj -c Release --no-build --no-restore --self-contained false -o artifacts/p03-ownership-review/service
dotnet publish src/StudioManager.CustomerClient/StudioManager.CustomerClient.csproj -c Release --no-build --no-restore --self-contained false -o artifacts/p03-ownership-review/client
```

**Only after explicit Owner approval**, an administrator should:

1. Review the manifest and back up the current service/client packages and protected configuration for rollback. Confirm the registered service executable location; do not infer it from the workspace.
2. Close CustomerClient. Stop only StudioManagerIdentity and wait for Stopped. Leave SQL Server running.
3. Copy the complete reviewed packages into their existing protected service/client folders. Preserve existing directory ACLs, service identity, service registration, client group and `%ProgramData%/StudioManager/IdentityService/service.json`. Do not copy a blank configuration template or change SQL grants/schema.
4. Compare installed files against SHA256.json. Start StudioManagerIdentity and confirm Running. Launch the matching client as MSI\P03ClientTester; old pipe sessions must log in again.
5. Using synthetic account A, submit KH900001 with the provided evidence, then check its status. Live success requires CustomerClient to display PENDING and an Owner read-only check against StudioManager_P03_Test to confirm the row is PENDING, linked to A, and KhachHangId is NULL.
6. Continue the existing staff/admin review, approval, competing-owner and revocation UAT. Verify audit metadata/history and immediate loss of owner reads after revocation.
7. If submission still fails, retain only the command, fixed displayed codes and correlation ID. Do not supply credentials, customer evidence, connection strings or raw SQL errors. If necessary, restore the backed-up packages during the approved maintenance window and start the same service.

No service install/stop/restart/replacement, migration, permission change, production database connection, commit/push/merge or P04–P08 work was performed. The installed service is intact. Approval to deploy and live PENDING acceptance are still outstanding; commit/push remain separately gated.
