# P02 — Targeted SQL connection diagnostic

Ngày: 12/11/2026. Scope: safe stage diagnostics and offline verification only.

## Finding and current evidence

Previously, OpenConnectionAsync wrapped both SqlConnection.OpenAsync and the whole validation path in one catch. It discarded the original exception and always threw "Cannot connect...". A target, permissions, file-read or fingerprint failure therefore looked like a transport failure. A failure during disposal could also replace the original error.

The Owner reports successful encrypted sqlcmd access under MSI\PhoenixP02Runner, the exact MSI\SQLEXPRESS/test DB identity, restricted permissions, 14 tables and matching stored/calculated schema fingerprints. These are Owner-provided results; Codex did not connect to SQL or independently reproduce them. In particular, sqlcmd does not exercise .NET's manifest file access/source checksum or the SqlClient connection path. The exact current failing stage remains unobserved until the runner reruns the instrumented metadata test.

## Fixed diagnostic stages

| Identifier | Boundary |
|---|---|
| CONNECTION_OPEN | SqlConnection construction and OpenAsync, including provider/authentication/encryption/certificate failures |
| TARGET_IDENTITY | Preconnection settings/mode validation; observed machine/server/instance/database; run/version/source-script checksum markers and local manifest reads; DB_ID/create_date continuity |
| PERMISSIONS | Effective server/database/object permissions, sysadmin/db_owner/unknown-role rejection, protected DB access and trust/chaining checks |
| SCHEMA_FINGERPRINT | Required stored metadata fingerprint, shared SELECT file loading, calculation and exact comparison |
| SCHEMA_SHAPE | Existing write-fixture shape guard; also the read-only metadata Fact's structure assertions/query after verified connection open |

Example: SQL test failed at stage TARGET_IDENTITY. Diagnostic details are intentionally omitted.

The diagnostic exception contains a fixed stage only. It does not retain the original exception as InnerException, copy Data/Source/HelpLink, or log provider details. Message and ToString are fixed-format; StackTrace is suppressed so a test runner cannot print local build paths. Cleanup is attempted after failure; a cleanup exception cannot replace the original stage or expose its payload. Later stages never run after a failure.

## Code and guard preservation

- VerifiedSqlTestDatabase.cs: split validation into named methods, use the shared staged sequence, and include connection construction/open in the CONNECTION_OPEN boundary. Return a connection only after all required checks succeed.
- SqlTestDiagnostics.cs: allowlisted stages, sanitized exception and staged execution/cleanup boundary.
- SqlMetadataIntegrationTests.cs: retain every query/assertion and wrap the schema portion as SCHEMA_SHAPE. Connection failures keep the stage assigned by the connection path.
- SqlTestDiagnosticsTests.cs: offline fault injection through the production sequence, stage/redaction tests and actual elevated/unknown-role guard coverage.
- SqlTestSafety.cs was inspected and left unchanged. Exact target/run restrictions, manifest checksum logic, Windows authentication, permission policy and write-mode requirements remain in force.

All existing SQL predicates are preserved. The nonempty stored metadata fingerprint check now runs inside SCHEMA_FINGERPRINT, before fingerprint calculation and before any connection is returned. READ_ONLY still performs no fixture mutation and does not run the write-fixture shape guard; its metadata Fact still checks schema structure. No encryption/certificate setting, SQL script, grant or schema was changed.

## Offline verification

Both Debug and Release solution builds: 0 warnings, 0 errors, using --no-restore.

Each configuration: main unit project 94 passed; SQL integration project 81 offline cases passed, 11 live SQL facts skipped. Total: 175 passed, 0 failed, 11 skipped. The 21 new cases cover five failing stages with successful/failing cleanup, ordering and short-circuiting, successful/read-only flows, settings/file failures, sysadmin/db_owner/NULL rejection, schema assertions, stage spoofing and unknown identifiers.

Each test process explicitly cleared PHOENIX_SQL_TEST_MODE, PHOENIX_SQL_TEST_CONNECTION_STRING and PHOENIX_SQL_TEST_RUN_ID. No application settings were read/changed. The existing VSTEST temporary-artifact fallback notice remains; it did not fail tests.

No SQL connection/DDL/DML/mutation suite was executed by Codex. Existing uncommitted P02 work and .p02-approved artifacts were preserved. No SQL permissions/schema/config changes, commit/push, frozen Frontend changes or P03 work.

## Exact runner-only rerun

Run this block only in the existing MSI\PhoenixP02Runner PowerShell window, with its approved PHOENIX_SQL_TEST_CONNECTION_STRING already set. The block deliberately reuses that setting without printing it or changing encryption/certificate options. Use the freshly built Debug output. No restore/build or mutation Fact is selected.

```powershell
Set-Location -LiteralPath 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
if ([Security.Principal.WindowsIdentity]::GetCurrent().Name -ne 'MSI\PhoenixP02Runner') {
    throw 'Run this only as MSI\PhoenixP02Runner.'
}
if ([string]::IsNullOrWhiteSpace($env:PHOENIX_SQL_TEST_CONNECTION_STRING)) {
    throw 'Approved connection string is missing; restore the existing test setting without changing TLS options.'
}
$env:PHOENIX_SQL_TEST_RUN_ID = '7d70a08ac15349acbd207f82c8349f6e'
$metadataTest = 'FullyQualifiedName=StudioManager.SqlIntegrationTests.SqlMetadataIntegrationTests.ReviewedSchema_HasRequiredTablesColumnsAndConstraintIndexes'
try {
    $env:PHOENIX_SQL_TEST_MODE = 'READ_ONLY'
    dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Debug --no-build --no-restore --filter $metadataTest --logger 'console;verbosity=normal' --results-directory "$env:TEMP\PhoenixP02SqlMetadata"
    if ($LASTEXITCODE -ne 0) { throw 'Metadata test failed; inspect the fixed diagnostic stage above.' }
}
finally {
    Remove-Item Env:PHOENIX_SQL_TEST_MODE -ErrorAction SilentlyContinue
}
```

Expected selection: exactly one test, no skips. If it fails, use its fixed stage to narrow the next read-only investigation; do not change permissions/markers/schema/TLS or enable write mode to force a pass. The command block was syntax-checked offline, not executed against SQL by Codex.
