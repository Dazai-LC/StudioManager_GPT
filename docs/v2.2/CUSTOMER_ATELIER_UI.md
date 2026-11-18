# Real CustomerClient — Atelier UI integration

Baseline: `c72b4f3`, branch `feature/v2.2-backend`. Implementation and offline checks complete; no SQL connection, live mutation or runtime deployment was performed.

The real CustomerClient now uses the approved Atelier sidebar, palette, typography, PageHeader, CardPanel, ModernButton, styled grids, password inputs/reveal controls and AtelierDialog. The input and navigation renderers were extracted from the existing UI; no simulation store, persona, authentication or payment logic was copied. CustomerClient still references only CustomerApi and rendering sources, with no SQL/repository access. Customer DPI configuration now matches the staff application's PerMonitorV2/asInvoker approach.

Preserved real API bindings: registration/login, contact profile, password change/logout, ownership-link requests/review, active catalog/add-ons, preview and submission-key booking requests, request history/cancellation, official booking, persisted payment history and receipts. Booking submission still creates PENDING requests; customers cannot approve bookings or record payments. Empty lists show guidance. Font changes and smaller viewports retain scrolling and readable actions.

Customer navigation contains only personal features. The separate anonymous **Nhân sự · Liên kết hồ sơ** entry retains existing staff identity-link review; authenticated staff see only that workflow. Operational booking review remains in Staff WinForms. The recent MainForm-owned staff service session fix is unchanged.

The staff login buttons **Cổng Khách hàng** and **Đăng ký Khách hàng** now launch the real executable; registration adds the implemented UI-only `--register` option. Demo launches preserve `--local-demo --runtime <same lease>` using the selected profile, without hardcoded timestamp folders. Normal launch supports the verified sibling layout `<root>/Staff` + `<root>/CustomerClient`; missing/inconsistent files, redirected paths or mixed profiles show an error. The launcher checks executable/assembly identity and required companion files; it never opens the legacy mock portal. Legacy demo classes remain inactive reference code.

## Files

- New: `src/StudioManager.CustomerClient/AtelierUi.cs`, `CustomerPortalPresentation.cs`; `src/StudioManager.WinForms/CustomerPortalLauncher.cs`, `Forms/AtelierInput.cs`, `Controls/AtelierNavButton.cs`.
- Modified customer files: `Program.cs`, `BookingWorkspaceControl.cs`, `StudioManager.CustomerClient.csproj`.
- Modified staff files: `Forms/AuthChrome.cs` and legacy `Forms/CustomerMainForm.cs` (render extraction), `Forms/LoginForm.cs` and `Program.cs` (real launcher/profile handoff), `StudioManager.WinForms.csproj` (shared rendering link).
- Existing offline DesktopSmoke harness extended; no new test project/framework. Evidence is under `artifacts/customer-atelier-20261117/`.

## Verification

- Solution Debug: **PASS**, 0 warnings/errors.
- Customer portal offline checks: **8/8 PASS**; real UI handlers with an injected fixture transport, including registration, profile edits, preview/confirmation/submission key, history, payment/receipt DTOs, launcher isolation, role separation and stale-callback cleanup.
- Existing staff session regression: **6/6 PASS**, including Dashboard/Payment navigation, same live test pipe, account change, expiry and delayed responses.
- Focused existing auth/ownership/booking/pipe unit tests: **14/14 PASS**.
- Four native customer layouts checked, including synthetic font scales 100/125/150%; screenshots saved as `customer-*.png`. This is offline geometry checking, not live SQL or multi-monitor DPI acceptance.
- Original migrations 01–08, demo bootstrap, Application services, CustomerPipeClient, LocalDemoProfile, IdentityService, Theme and the three staff-session files remain unchanged. Protected source/index checks: **42/42 unchanged**.

## Owner-only update commands — not executed

Close **both GUI applications**, leaving the existing demo console host running. Run from the repository after review/approval. These commands copy only four named build files per GUI, preserve existing SQL configuration and dependencies, and back up the files first. No service restart, database setup, migrations or cleanup are needed. Use the exact existing demo root, not a new timestamp directory.

```powershell
$ErrorActionPreference = 'Stop'
$repoPath = 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'
Set-Location -LiteralPath $repoPath
$demoRoot = Read-Host 'Paste the existing local demo root containing Staff, CustomerClient and local-demo-host.json'
if (-not (Test-Path -LiteralPath "$demoRoot\local-demo-host.json") -or
    (Get-Content -LiteralPath "$demoRoot\local-demo.profile" -Raw).Trim() -ne 'StudioManager.LocalDemo.20261116A.v1') {
    throw 'The matching existing local demo root is required.'
}
$backup = Join-Path $repoPath ('artifacts\customer-atelier-before-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup | Out-Null
$guiProjects = [ordered]@{ CustomerClient = 'StudioManager.CustomerClient'; Staff = 'StudioManager.WinForms' }
foreach ($entry in $guiProjects.GetEnumerator()) {
    $source = Join-Path $repoPath "src\$($entry.Value)\bin\Debug\net8.0-windows"
    $target = Join-Path $demoRoot $entry.Key
    $saved = Join-Path $backup $entry.Key
    if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw 'Existing GUI output folder missing.' }
    New-Item -ItemType Directory -Path $saved | Out-Null
    foreach ($suffix in 'exe', 'dll', 'deps.json', 'runtimeconfig.json') {
        $name = "$($entry.Value).$suffix"
        Copy-Item -LiteralPath "$target\$name" -Destination $saved
        Copy-Item -LiteralPath "$source\$name" -Destination $target -Force
    }
}
& "$demoRoot\Staff\StudioManager.WinForms.exe" --local-demo --runtime "$demoRoot\local-demo-host.json"
# Alternatively open the customer executable directly:
& "$demoRoot\CustomerClient\StudioManager.CustomerClient.exe" --local-demo --runtime "$demoRoot\local-demo-host.json"
# Direct registration entry, if needed:
# & "$demoRoot\CustomerClient\StudioManager.CustomerClient.exe" --local-demo --runtime "$demoRoot\local-demo-host.json" --register
```

`appsettings.json`, protected `service.json`, host binaries, CustomerApi/dependencies and the demo marker/lease are not copied or edited. Source builds are already verified; if rebuilding after later edits, run `dotnet build StudioManager.sln -c Debug --no-restore` before these commands and stop on failure. PDBs are optional debugging artifacts, not required runtime changes.

## Manual walkthrough — remaining live checks

1. From staff login, open each customer button: verify real CustomerClient and correct login/registration view. Missing real client must show an error; no demo persona screen should open.
2. Under separately approved live testing, register a synthetic customer, verify success returns to login, then log in. No email verification/OTP is claimed or expected.
3. Read/edit contact details, reload, test password reveal and profile password change/logout. Sign in again and verify persisted data; do not reuse another account's private data.
4. Browse actual package/service prices; choose package/add-ons, date/time and notes. Preview, confirm once, then view the returned PENDING request and history. It must not become official before staff approval.
5. For an already approved, authorized booking, open official booking, payment history and its saved receipt. Compare stored amounts/code; reprint must retain the same code. Customer UI offers no collection/mark-paid operation. Do not change the previously approved real booking merely to test this UI.
6. Use Staff WinForms for operational review. Navigate Dashboard → Booking Requests → Payments → Booking Requests: the valid staff service session remains available. Do not approve/reject existing requests without separate authorization.
7. Switch pages, resize at normal/minimum size and test actual display scaling. Log out/change accounts: old profile, draft, rows and permissions must clear; true expiry requires reauthentication. Restart the matching demo GUIs to check persistence after approved live operations.

SQL CONNECTED: **NO** · LIVE DATA MODIFIED: **NO** · SERVICE MODIFIED: **NO** · DEPLOYED: **NO** · COMMIT/PUSH: **NO**.
