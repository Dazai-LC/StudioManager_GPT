# P02 — TARGET_IDENTITY reason diagnostics

Ngày 12/11/2026. Scope: TARGET_IDENTITY only. No live SQL execution by Codex.

## Root cause status

The runtime cause is **not yet established**. The Owner reports all seven SQL comparisons true under MSI\PhoenixP02Runner, matching source/metadata checksums and restricted permissions. These results do not identify the C# exception that the previous stage-only wrapper discarded.

Review found no confirmed ordinal or SQL-to-C# conversion defect in the original eleven-field projection: string fields were explicitly nvarchar, ordinal 7 was DB_ID()/Int32, ordinal 8 was sys.databases.create_date/DateTime, ordinal 9 the metadata fingerprint, ordinal 10 the server name. Offline typed-reader tests reproduce that mapping successfully.

A confirmed diagnostic defect was that query/reader errors, local manifest I/O, value mismatches and continuity errors all became the same TARGET_IDENTITY message. The diagnostic boundary now preserves only an allowlisted typed reason and rebuilds a sanitized exception, discarding the original payload. It does not claim that the runtime metadata test now passes.

SQL equality does not prove C# ordinal string equality: case or trailing whitespace can compare equal under SQL semantics. The existing ordinal comparisons were retained; no trimming, case folding or numeric/date coercion was added. The independent local-machine comparison, config checks and DB ID/create-date continuity still apply.

## Changes and line references

- VerifiedSqlTestDatabase.cs:35 — guarded query/reader resource handling, unchanged target predicates and continuity validation.
- SqlTestSafety.cs:64 — one reason per observed identity comparison; :29 — separate preconnection config reasons; :94 — distinct manifest lookup/read/hash failures.
- SqlTargetIdentityReader.cs — original SELECT expressions/order with explicit aliases; typed/null-aware reader mapping and database ID/create-date continuity.
- SqlTestDiagnostics.cs — fixed reason enum, resource failure/disposal handling and same-stage typed reason preservation while rebuilding a clean exception.
- SqlTargetIdentityTests.cs — 66 added offline cases. Existing safety/diagnostic tests now assert the intended diagnostic exception type/reason.

The SELECT expressions and ordinal order were compared with the saved source before this change; only aliases and whitespace differ. Approved target constants are unchanged. Permissions, fingerprint, shape and connection-opening implementation after the target method are unchanged. No schema, grants, SQL setup artifact or TLS setting was modified.

## Meaning of the remaining reason

| Reason | Meaning |
|---|---|
| CONFIG_* | Preconnection settings failed; CONFIG_DATA_SOURCE_MISMATCH includes use of a hostname alias instead of the already-approved localhost\SQLEXPRESS |
| DATABASE/MACHINE/INSTANCE/SERVER/RUN_ID/VERSION/CHECKSUM_MISMATCH | The named C# exact comparison failed; values are not printed |
| LOCAL_MACHINE_READ_FAILED / LOCAL_MACHINE_MISMATCH | Independent .NET machine-name check failed |
| QUERY_EXECUTION_FAILED | Creating/executing the bounded metadata command failed |
| READER_READ_FAILED / READER_EMPTY / READER_LAYOUT_INVALID | Reader advance, absent row or unexpected projection |
| READ_*_FAILED | Typed/null access failed for the named field; no provider message is exposed |
| MANIFEST_ROOT_UNAVAILABLE / MANIFEST_ROOT_LOOKUP_FAILED | Local repository lookup failed; unavailable may mean missing or inaccessible because File.Exists suppresses access errors |
| MANIFEST_ACCESS_DENIED / MANIFEST_SCRIPT_NOT_FOUND / MANIFEST_SCRIPT_READ_FAILED / MANIFEST_HASH_FAILED | Local source-file/checksum operation failed, distinct from a checksum mismatch |
| DATABASE_ID_CHANGED / DATABASE_CREATED_AT_CHANGED | Same VerifiedSqlTestDatabase observed a changed DB identity between connections |
| DATABASE_NOT_WRITABLE / WRITE_MODE_REQUIRED | Existing write-mode guard failed; no SQL mutation is attempted |
| READER_DISPOSE_FAILED / COMMAND_DISPOSE_FAILED | Reader/command close failed; an earlier primary failure takes precedence |
| UNEXPECTED_FAILURE | An unclassified exception remains; it is not proof of a target mismatch |

Example: SQL test failed at stage TARGET_IDENTITY; reason READ_CREATED_AT_FAILED. Diagnostic details are intentionally omitted.

No raw exception message, connection string, credential, SQL value or sensitive path is included. InnerException/Data are discarded, StackTrace suppressed and ToString fixed-format. Unknown reason values or reasons attached to other stages are refused.

## Offline verification

Debug and Release solution builds: **0 warnings, 0 errors**.

Each configuration: main unit project **94 pass**; SQL project **147 offline pass, 11 live SQL facts skip**. Total **241 pass, 0 fail, 11 skip**, including 66 new reader/identity/config/I/O/redaction/continuity/resource cases. SQL test process mode/connection/run environment variables were cleared before invocation.

No live SQL, DDL/DML, mutation suite, permission/schema/approved-config change, commit/push or P03. Existing P02 work and frozen Frontend worktree preserved.

## One runner-only metadata rerun

Use the current MSI\PhoenixP02Runner PowerShell window with its approved connection string already set. This block reuses it without printing or changing TLS options, selects exactly one read-only metadata test and clears test mode afterward.

~~~powershell
& {
    Set-Location -LiteralPath 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
    if ([Security.Principal.WindowsIdentity]::GetCurrent().Name -ne 'MSI\PhoenixP02Runner') {
        throw 'Run this only as MSI\PhoenixP02Runner.'
    }
    if ([string]::IsNullOrWhiteSpace($env:PHOENIX_SQL_TEST_CONNECTION_STRING)) {
        throw 'Approved connection string is missing.'
    }
    $env:PHOENIX_SQL_TEST_RUN_ID = '7d70a08ac15349acbd207f82c8349f6e'
    try {
        $env:PHOENIX_SQL_TEST_MODE = 'READ_ONLY'
        dotnet test tests/StudioManager.SqlIntegrationTests/StudioManager.SqlIntegrationTests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName=StudioManager.SqlIntegrationTests.SqlMetadataIntegrationTests.ReviewedSchema_HasRequiredTablesColumnsAndConstraintIndexes' --logger 'console;verbosity=normal' --results-directory "$env:TEMP\PhoenixP02SqlMetadata"
        if ($LASTEXITCODE -ne 0) { throw 'Metadata test failed; retain its fixed stage/reason identifier.' }
    }
    finally {
        Remove-Item Env:PHOENIX_SQL_TEST_MODE -ErrorAction SilentlyContinue
    }
}
~~~

The command block was syntax-checked offline, not run against SQL. The next reason observed under the runner is the remaining diagnostic needed to establish the runtime cause; do not infer a fix from offline success or change guards/permissions/TLS to force a pass.
