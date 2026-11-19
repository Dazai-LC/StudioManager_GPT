# Visual Studio F5 with the existing Local Demo

Implemented on `feature/v2.2-integration`, 2026-11-19. Use the existing populated demo; no SQL setup, bootstrap, seed, migration or binary-copy step is part of this workflow.

## Staff/Admin F5

1. Open `G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend\StudioManager.sln` in Visual Studio 2022 with **.NET desktop development**, .NET 8 SDK **8.0.4xx**, and the .NET 8 Windows Desktop runtime installed. This machine has VS 17.14 and SDK 8.0.425.
2. Select solution configuration **Debug**, **Any CPU**.
3. Right-click **StudioManager.WinForms** in Solution Explorer and choose **Set as Startup Project**.
4. In the green Run button's launch-profile dropdown, select **Staff-Admin (Local Demo)**. It is a normal **Project** debug target with the checked-in argument `--local-demo-dev`.
5. Set a breakpoint in `src/StudioManager.WinForms/Program.cs` or the Staff login code, then press **F5**. Use an existing demo Staff/Admin account.

Visual Studio builds and debugs `src/StudioManager.WinForms/bin/Debug/net8.0-windows/StudioManager.WinForms.exe`. Its Debug-only build dependency also builds the real CustomerClient into its own normal output. Neither GUI is copied to the packaged demo.

The Staff login's customer-login and customer-registration buttons start `src/StudioManager.CustomerClient/bin/Debug/net8.0-windows/StudioManager.CustomerClient.exe` with the same development selection; registration additionally passes `--register`. The actual executable, assembly identity, GUI subsystem and required companion files are checked. Missing/invalid output produces a startup error; there is no mock portal fallback.

## CustomerClient F5

1. Open the same solution with **Debug**, **Any CPU** selected.
2. Right-click **StudioManager.CustomerClient** and choose **Set as Startup Project**.
3. Select **CustomerClient (Local Demo)** in the launch-profile dropdown.
4. Set a breakpoint in `src/StudioManager.CustomerClient/Program.cs` or customer login code and press **F5**. Use an existing demo customer account.

This debugs the current source-built CustomerClient directly. Its startup profile selects the same existing demo and starts/reuses the same host automatically. No Staff window is required.

Visual Studio normally attaches to the startup process. A CustomerClient started by the Staff login is a child process with current binaries, but does not automatically inherit the debugger. To debug customer breakpoints, use CustomerClient as the F5 startup project, or use **Debug > Attach to Process** on that exact `StudioManager.CustomerClient.exe` instance. The packaged IdentityService is intentionally reused; these profiles do not replace or source-debug that verified backend host.

## Explicit configuration and isolation

The new repository file `local-demo.development.json` contains only a profile name and the repository-relative path of the existing runtime lease:

```json
{
  "Profile": "StudioManager.LocalDemo.20261116A.v1",
  "RuntimeFile": "artifacts/P04_LocalDemo_20261117-143416-995/local-demo-host.json"
}
```

The development switch is accepted only from the exact supported source Debug apphost layout under this solution. Paths are derived from the executable location, independently of Visual Studio's working directory. Custom output paths, Release outputs, unknown/extra arguments, paths outside repository `artifacts`, UNC paths and reparse points are refused. The existing `local-demo.profile` marker must match exactly. A change of selected runtime between Staff startup and customer launch is refused.

Both Debug GUI entrypoints require explicit demo selection, even if a launch profile was accidentally removed or the executable was opened without arguments. Rejection happens before creating login forms or selecting SQL/production identity clients. There is no environment-variable or production fallback.

Staff reads **the existing** `artifacts/P04_LocalDemo_20261117-143416-995/Staff/appsettings.json` in place. The existing SQL validator permits only `MSI\SQLEXPRESS` / `StudioManager_P04_Test_20261116A`, Windows authentication, mandatory encryption and the approved demo certificate exception. The source/output Staff `appsettings.json` is not used for a demo session. CustomerClient retains its API-only dependency boundary and gains no SQL connection, repository or credentials.

The host remains the existing packaged `IdentityService/StudioManager.IdentityService.exe`, reading its own existing `appsettings.json`. The dedicated pipe remains `StudioManager.Identity.LocalDemo.20261116A.v1`. Peer PID, exact executable path, start time, SID, session and protected lease checks remain in place before credentials are sent.

The original packaged `Staff/`, `CustomerClient/`, `IdentityService/` layout and `--local-demo --runtime <absolute lease path>` arguments are still supported. Packaged clients retain their original explicit host-start workflow. Normal Release/production endpoint selection is unchanged.

`global.json` selects SDK 8.0.4xx with patch roll-forward, instead of implicitly selecting this machine's SDK 10.0.404. This keeps CLI and installed VS 2022 builds on compatible tooling. See Microsoft's [SDK/MSBuild/Visual Studio compatibility table](https://learn.microsoft.com/dotnet/core/porting/versioning-sdk-msbuild-vs).

## Host startup and lease recovery

F5 handles host startup automatically; no separate PowerShell command is required. Use the **same Windows user and interactive session** for both clients and any manually started demo host.

1. Validate the exact demo marker and selected runtime. Serialize concurrent developer startups using a mutex for that runtime.
2. If a lease exists, validate its size, complete/unambiguous JSON fields, profile, pipe, exact packaged host path, positive PID/start time, SID, session data, owner and protected ACL.
3. If that PID is live, compare its actual executable, start time, Windows identity and session. Reuse only the matching host. A reused PID, other process/session/user or inaccessible identity stops startup clearly. No live/unverifiable PID is cleaned up or terminated.
4. Only a positively dead PID permits recovery. Reopen with sharing that a running host cannot allow, recheck the descriptor and PID, then rename the stale lease to `local-demo-host.json.stale-<UTC timestamp>-<unique suffix>`. Preserve its bytes and ACL as evidence; never overwrite or delete it. A locked/changing lease stops recovery.
5. Start only the exact existing packaged demo EXE with `--local-demo --runtime <same absolute lease>`. Missing package/configuration fails; nothing is generated, copied or repaired. The host's existing SQL validator runs before publishing its lease.
6. Wait for the matching lease and verify the real named-pipe server identity. The readiness connection sends **no protocol request, credential or SQL command**. An unready live host is left running and reported; it is not restarted.

The automatically started host is an ordinary hidden console process, not a service. It stays running when you stop GUI debugging, so subsequent F5 sessions reuse it. No automatic host shutdown/kill is implemented; Windows session termination/reboot ends it, and the next F5 handles a confirmed stale lease safely. A host started manually in a console can still be stopped with its existing Ctrl+C handler after closing its clients.

The helper never queries, stops, restarts, installs or reconfigures `StudioManagerIdentity`. It cannot terminate any process. Corrupt, foreign, inaccessible or ambiguous leases are deliberately retained with an actionable error; do not bypass that error by deleting a lease or killing its PID.

## Verification performed

- `dotnet build StudioManager.sln -c Debug --nologo`: **PASS**, SDK **8.0.425**, **0 errors**, **10 NU1900 warnings** because the NuGet vulnerability feed was unreachable. The initial SDK 10 build also passed before the SDK pin.
- Final build after setting the CustomerClient dependency's `Private=false`: `dotnet build StudioManager.sln -c Debug --no-restore --nologo` **PASS**, **0 errors**, **5 NU1900 warnings**. The dependency builds CustomerClient without propagating its apphost or assets into Staff output.
- Visual Studio 2022's own `MSBuild.exe StudioManager.sln /t:Build /p:Configuration=Debug "/p:Platform=Any CPU" /nologo /v:minimal`: **PASS**, exit **0**, with the same NuGet-feed warning category. No interactive debugger session was launched by this build.
- `dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Debug --no-build --no-restore --filter "FullyQualifiedName~LocalDemoDevelopmentTests|FullyQualifiedName~LocalDemoRuntimeTests" --nologo`: **87 passed, 0 failed, 0 skipped** under SDK 8.
- MSBuild evaluation: both GUI targets use their ordinary source Debug outputs, `UseAppHost=true`, portable PDBs, `DebugSymbols=true`, `Optimize=false`; Staff has the Debug-only CustomerClient build dependency.
- A separate startup probe checked the actual checked-in launch profiles, actual source Debug EXEs/PDBs, real customer executable/assembly resolution and both login/registration arguments.
- The same probe invoked the actual host-start helper against the existing demo: recovered the confirmed dead-PID lease, started the exact packaged host, verified pipe identity and reused that host for both profiles. **No application requests, login, financial operation or SQL command were sent.**
- The former PID 18044 lease is preserved as `local-demo-host.json.stale-20261119T040104017-6dff3e12c24c40d9982bdf8c16b33f47`. The probe left demo host PID **18280**, session **3**, running at verification time.
- Read-only service snapshots before/after: production `StudioManagerIdentity` remained **Stopped**, PID **0**. No service operation was issued.
- SHA-256 of the existing marker, Staff/IdentityService `appsettings.json` and existing `Start-LocalDemo.ps1` remained unchanged. No existing binary or receipt was replaced. No database/SQL or business implementation file was modified, and no SQL tests/UAT were run.
- Offline recovery checks cover valid reuse, dead PID archival, live PID reuse, foreign path/user/session, denied observation, malformed/duplicate/missing fields, locked leases and a PID becoming live during recovery. Tests use unique temporary fixture directories, never the real demo.
- `git diff --check`: **PASS**. The initial untracked artifacts, `.p02-approved`, SQL UAT and other untracked work are preserved. No branch switch, reset, clean, commit, push or deployment was performed.

Startup probe source/output is retained under `artifacts/f5-startup-20261119-bf098839/`. An initial probe assertion was corrected to normalize Windows path separators, and its lease snapshot was corrected to use the host-compatible read sharing mode. Those probe issues did not require application startup changes. A temporary CLI home was used for SDK 8's first-use files because the execution sandbox cannot write the normal user SDK sentinel; cached packages remained in the existing user NuGet cache. This is a verification-environment detail, not an F5 setup step.

## Remaining interactive acceptance

An interactive Visual Studio F5 session and breakpoint binding have **not** been driven automatically. In the Owner's session:

1. Follow Staff F5 above; confirm its startup breakpoint binds, then open the customer-login and registration entrypoints and confirm the real customer EXE path. Close any customer window before rebuilding its Debug output.
2. Follow CustomerClient F5 above; confirm its startup/login breakpoint binds and the existing demo account can use the real app. Normal interactive account/business operations use the preserved database and remain the Owner's choice; they were not exercised by the startup probe.
3. Stop and repeat F5; confirm the same verified demo host is reused. Recovery mechanics have already been tested offline and against the existing stale lease; an actual reboot followed by F5 remains an optional manual check.

If a prior local `.csproj.user` selection overrides the toolbar target, explicitly select the checked-in **Local Demo** profile again. Close running source Debug GUIs before a rebuild if Windows reports a locked binary. Do not copy binaries, remove artifacts or restart the production service to resolve a build lock.

## Source/configuration files changed

| File | Purpose |
|---|---|
| `global.json` | VS-compatible .NET 8 SDK selection |
| `local-demo.development.json` | Explicit reference to the existing demo; no copied configuration |
| `src/StudioManager.WinForms/Properties/launchSettings.json` | Staff/Admin Project debug profile |
| `src/StudioManager.CustomerClient/Properties/launchSettings.json` | CustomerClient Project debug profile |
| `src/StudioManager.WinForms/StudioManager.WinForms.csproj` | Debug-only build dependency on the real CustomerClient |
| `src/StudioManager.CustomerApi/LocalDemoDevelopment.cs` | Bounded source Debug executable/configuration discovery |
| `src/StudioManager.CustomerApi/LocalDemoProfile.cs` | Explicit development profile selection and packaged config locations |
| `src/StudioManager.CustomerApi/LocalDemoHostStartup.cs` | Safe start/reuse/readiness and stale lease archival |
| `src/StudioManager.CustomerApi/LocalDemoHostIdentity.cs` | Shared strict descriptor validation before process observation/recovery |
| `src/StudioManager.WinForms/Program.cs` | Debug opt-in guard and existing demo SQL settings selection |
| `src/StudioManager.CustomerClient/Program.cs` | Debug opt-in guard; existing registration argument preserved |
| `src/StudioManager.WinForms/CustomerPortalLauncher.cs` | Real CustomerClient discovery in both supported layouts |
| `src/StudioManager.WinForms/Forms/LoginForm.cs` | Actionable development startup error only |
| `tests/StudioManager.Tests/LocalDemoDevelopmentTests.cs` | Focused offline startup/lifecycle coverage |
| `tests/StudioManager.Tests/StudioManager.Tests.csproj` | Exercise the actual launch planner without loading WinForms |
| `README.md`, `docs/v2.2/LOCAL_DEMO_F5.md` | Supported F5 instructions and verification/limitations |
