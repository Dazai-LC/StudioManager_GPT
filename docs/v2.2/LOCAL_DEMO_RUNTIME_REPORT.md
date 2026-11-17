# Local demo runtime — 2026-11-17

Branch `feature/v2.2-backend`; starting commit `9b0bcbc`. Source implementation and offline verification only. SQL setup and a real three-process demo have **not** been executed.

## Implementation

All three executables accept exactly `--local-demo --runtime <absolute local-demo-host.json path>`. They require the matching `local-demo.profile` marker and separate output layout shown below. A marked demo output refuses startup without these arguments, preventing accidental use of the installed P03 endpoint. Invalid arguments, configuration, profile or host identity stop startup; there is no production fallback.

- IdentityService runs the existing `IdentityPipeServer`, `IdentityDispatcher`, customer identity and booking services as an ordinary interactive console process on MSI. Ctrl+C cancels listeners/active sessions, awaits their completion and removes its host lease. No Windows Service is installed.
- The dedicated pipe is `StudioManager.Identity.LocalDemo.20261116A.v1`. Its protected ACL grants only the current Windows user and retains network denial. Use the **same non-elevated Windows user/session** for all three demo processes.
- A small host lease contains only profile, pipe, PID, process start time, executable path, SID and session ID. It is created atomically with a current-user-only ACL and cannot be overwritten while the host runs. Clients compare these fields with OS process/token information and the actual named-pipe server PID **before sending credentials**. The host EXE must be the matching `IdentityService/StudioManager.IdentityService.exe` under the selected runtime root.
- CustomerClient and the staff booking-review page continue using `CustomerPipeClient`. Startup selects its immutable process profile before any form is constructed. CustomerClient gains no SQL, Application, Infrastructure or repository dependency.
- Console host and staff use their own `appsettings.json`, copied from the already prepared `database/demo-local/staff.appsettings.demo.json`. A shared validator requires exactly `MSI\SQLEXPRESS` / `StudioManager_P04_Test_20261116A`, Windows authentication, mandatory encryption and the local-demo-only certificate exception. Credentials, alternate targets, attachments and failover settings are refused. There is no default demo connection string.
- Unmarked outputs with no arguments keep the existing P03 behavior: protected `service.json`, registered service name, production pipe, SCM PID verification and service-account checks. These default checks are retained. No installed service/configuration was accessed for modification.

Changed tracked files: `CustomerApi/CustomerPipeClient.cs`, `IdentityService/Program.cs`, `IdentityService/IdentityPipeServer.cs`, `CustomerClient/Program.cs`, `WinForms/Program.cs` under `src/`. The GUI entrypoints only select configuration and reuse the existing Atelier error dialog; layouts, theme and navigation are unchanged.

New files: `src/StudioManager.CustomerApi/{LocalDemoProfile,LocalDemoHostIdentity}.cs`, `src/StudioManager.IdentityService/LocalDemoConsoleHost.cs`, `src/StudioManager.Infrastructure/LocalDemoSqlSettings.cs`, `tests/StudioManager.Tests/LocalDemoRuntimeTests.cs`, and this report. No packages, projects, dispatcher, business rules, payment/receipt logic or SQL migrations were added/changed.

## Offline results

| Check | Result |
|---|---|
| Solution Debug build | PASS; 0 warnings, 0 errors |
| Solution Release build | PASS; 0 warnings, 0 errors |
| Existing main test project, Debug | **323 passed, 0 failed, 0 skipped** |
| Existing main test project, Release | **323 passed, 0 failed, 0 skipped** |
| New demo regression cases within each total | **53 passed** |
| Migrations 01–08, five prepared bootstrap files, approved Theme.cs, Git index | All 15 baseline hashes unchanged |
| Whitespace/diff check | PASS |

Tests cover unchanged production endpoint/SCM rejection, exact demo SQL configuration and redaction, missing/invalid files/options, all three executable roles, explicit profile markers, customer/staff endpoint selection, PID/start/path/SID/session checks, native pipe PID rejection, current-user ACLs, credential withholding from a squatted demo pipe, and real server/dispatcher cancellation with in-memory repositories. The clean-shutdown test logs in a synthetic customer and verifies its session is revoked and its lease removed. No live SQL test is included or run. Full expected-host acceptance against the published console EXE remains a future live demo check.

Evidence: `artifacts/local-demo-runtime-20261117/` contains build logs, `Debug-unit.trx`, `Release-unit.trx`, and `protected-source-verification.json`. An initial build into the usual Debug directory encountered a DLL locked by an existing WinForms process; builds were moved to a separate artifacts output without stopping that process. Earlier intermediate failures remain in their logs. Final results above use the isolated output. Test SDK emits a temporary-artifacts-directory notice; test totals and exit codes pass.

Reproduce offline checks from the repository with cached .NET 8 dependencies and a compatible SDK (this run used SDK 10.0.404):

```powershell
$repo = 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
Set-Location -LiteralPath $repo
$checks = Join-Path $repo 'artifacts\local-demo-runtime-20261117'
$build = Join-Path $checks 'build'
New-Item -ItemType Directory -Path $checks -Force | Out-Null
Set-Content -LiteralPath "$checks\NuGet.offline.config" -Encoding Ascii -Value '<configuration><packageSources><clear /></packageSources></configuration>'
foreach ($configuration in 'Debug', 'Release') {
    dotnet build StudioManager.sln -c $configuration --artifacts-path $build --configfile "$checks\NuGet.offline.config" -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; stop.' }
    dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c $configuration --no-build --no-restore --artifacts-path $build --logger "trx;LogFileName=$configuration-unit.trx" --results-directory $checks
    if ($LASTEXITCODE -ne 0) { throw 'Offline tests failed; stop.' }
}
```

## Future launch commands — Owner approval first

Prerequisites: separately approve/run the existing bootstrap according to `database/demo-local/README.md`; do not reapply it to a nonempty database. The intended demo Windows user must already have the SQL rights needed by the existing application on **only the disposable database**. This change creates no account or SQL grant. Run the offline builds above first. The following preparation/start commands have **not** been run in this task.

In PowerShell window A, set up a **new** output directory. No installed P03 folders or protected files are used:

```powershell
$repo = 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
Set-Location -LiteralPath $repo
$build = Join-Path $repo 'artifacts\local-demo-runtime-20261117\build'
$demoRoot = Join-Path $repo ('artifacts\P04_LocalDemo_' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
if (Test-Path -LiteralPath $demoRoot) { throw 'Use a new output directory.' }
New-Item -ItemType Directory -Path $demoRoot | Out-Null
Set-Content -LiteralPath "$demoRoot\local-demo.profile" -Encoding Ascii -NoNewline -Value 'StudioManager.LocalDemo.20261116A.v1'

dotnet publish src/StudioManager.IdentityService/StudioManager.IdentityService.csproj -c Release --no-restore --artifacts-path $build -p:UseAppHost=true -o "$demoRoot\IdentityService"
if ($LASTEXITCODE -ne 0) { throw 'Host publish failed.' }
dotnet publish src/StudioManager.CustomerClient/StudioManager.CustomerClient.csproj -c Release --no-restore --artifacts-path $build -p:UseAppHost=true -o "$demoRoot\CustomerClient"
if ($LASTEXITCODE -ne 0) { throw 'Customer publish failed.' }
dotnet publish src/StudioManager.WinForms/StudioManager.WinForms.csproj -c Release --no-restore --artifacts-path $build -p:UseAppHost=true -o "$demoRoot\Staff"
if ($LASTEXITCODE -ne 0) { throw 'Staff publish failed.' }

Copy-Item -LiteralPath "$repo\database\demo-local\staff.appsettings.demo.json" -Destination "$demoRoot\IdentityService\appsettings.json"
Copy-Item -LiteralPath "$repo\database\demo-local\staff.appsettings.demo.json" -Destination "$demoRoot\Staff\appsettings.json" -Force
$runtime = Join-Path $demoRoot 'local-demo-host.json'
Write-Host "Demo root: $demoRoot"
& "$demoRoot\IdentityService\StudioManager.IdentityService.exe" --local-demo --runtime $runtime
if ($LASTEXITCODE -ne 0) { throw 'Demo host failed; inspect this console before retrying.' }
```

Wait for `LOCAL DEMO host ready`. This indicates a protected listener and published lease, **not** successful SQL setup/connection or business UAT. In PowerShell window B under the same Windows user/session, paste the **actual printed demo root**, then start both GUIs:

```powershell
$demoRoot = Read-Host 'Paste the exact Demo root printed in window A'
$runtime = Join-Path $demoRoot 'local-demo-host.json'
$arguments = @('--local-demo', '--runtime', ('"{0}"' -f $runtime))
$customer = Start-Process -FilePath "$demoRoot\CustomerClient\StudioManager.CustomerClient.exe" -ArgumentList $arguments -PassThru
$staff = Start-Process -FilePath "$demoRoot\Staff\StudioManager.WinForms.exe" -ArgumentList $arguments -PassThru
```

Use the normal staff login and required initial password-change workflow before staff API review. For manual acceptance, reuse the prepared checklist: register → login → edit/reload profile → submit booking request → staff approval → demo payment → persisted receipt → close/restart these same demo processes → verify persisted data and denied cross-customer access. Record results only after execution.

Stop only these GUIs from window B, or close their own windows normally:

```powershell
foreach ($process in @($customer, $staff)) {
    if (-not $process.HasExited) {
        if (-not $process.CloseMainWindow()) { throw 'Close this demo window manually; do not kill other processes.' }
        if (-not $process.WaitForExit(10000)) { throw 'Demo window is still busy; inspect it before continuing.' }
    }
}
```

Then press **Ctrl+C in window A** and wait for the command to return. Clean shutdown removes `local-demo-host.json`; the persistent profile marker remains. Restart the same host command before restarting the GUIs. Never use `Stop-Service` or kill processes by name. After an abnormal crash, an occupied/stale lease deliberately blocks startup: verify that the recorded host has exited before Owner-reviewed removal of **only that lease**, never automatically overwrite it.

## Remaining verification and status

No source dependency remains blocked. Owner approval is still required for SQL bootstrap/execution, preparation of runnable demo outputs, and live demo actions. No approval is needed to install/change P03 because no such change is proposed. SQL permissions, real console-host verification, full three-process startup/restart and the end-to-end checklist remain **unverified live**. The local certificate exception and same-user writable demo binaries are acceptable only within this trusted local demonstration; this is not a production deployment/security claim. The bootstrap README's earlier runtime-blocked statement is the pre-implementation snapshot; its SQL instructions and files are unchanged.

- LOCAL DEMO RUNTIME SOURCE: **PASS**
- DEFAULT P03 SERVICE COMPATIBILITY: **PASS — source/offline; installed service not controlled or live-tested**
- DEBUG / RELEASE: **PASS / PASS**
- OFFLINE TESTS: **323/323 per configuration, including 53 new demo cases**
- SQL CONNECTED: **NO**
- BOOTSTRAP EXECUTED: **NO**
- EXISTING SERVICE MODIFIED: **NO**
- DEPLOYED: **NO**
- COMMIT/PUSH: **NO**
