# Local demo preparation — 16/11/2026

**LOCAL DEMO BOOTSTRAP: READY for review. DATABASE CONNECTED: NO. SQL EXECUTED: NO. EXISTING SERVICE MODIFIED: NO. MIGRATIONS 01–08 MODIFIED: NO. NEXT OWNER APPROVAL REQUIRED: YES.**

Approved target only: `MSI\SQLEXPRESS` / existing `StudioManager_P04_Test_20261116A`. The database's existence/emptiness is supplied by the Owner and has **not** been queried in this task. Future execution verifies exact engine/machine/instance/database identity and emptiness before initial DDL. No default-instance/database fallback exists. Never use MSSQLSERVER, StudioManager or a P02/P03 database.

## Files and what they create

- `01_BOOTSTRAP_REVIEW_ONLY.sql`: one disabled fresh-install transaction. Original 02 business schema + the two P03 identity tables from 06 + the three P04 booking-request tables from 07 + receipt columns/constraint/unique action-key index from 08. **19 business tables in total**, with the original PK/FK/CHECK/unique/money constraints. Source payloads are copied separately; original migration gates/files remain intact.
- Synthetic staff/catalog/resource subset of 03: 8 employees, 3 internal accounts, 5 packages, 8 services, 3 rooms, 10 resources. Staff/resource FKs use code-based lookups. No pre-created customer account, inferred ownership, booking, payment or refund; create these through the real application during the demo. Original seed values are fictional/public demo data, not copied production data.
- `Invoke-LocalDemo.ps1`: default file-only validation; live execution needs **both** `-Execute -OwnerApproved`. It opens only the exact existing target using Windows Integrated Authentication and runs the reviewed batch, enabling its approval bit in memory. Nothing executes during builds/tests.
- `staff.appsettings.demo.json`: proposed configuration for a separate staff-app demo output folder only. It is not read automatically and has not replaced any appsettings/service.json. The local `Encrypt=True;TrustServerCertificate=True` exception is specific to this approved disposable instance/database; never copy it into production.
- `source-review.json`: original 01–08 and prepared batch checksums for review. No passwords supplied by real users, runtime configuration or SQL credentials are collected.

There is no CREATE/DROP/ALTER DATABASE, data overwrite/reset, SQL login/Windows account creation, grant, certificate setup or service installation. All session SET options are local to the setup connection. Original repository/application authentication, ownership, transaction/locking, exact-cent money and duplicate-payment behavior are retained. Run setup exclusively, without another app writing to this database. Nonempty/partial/already-initialized databases are refused, never repaired automatically. A failed transaction rolls back; a lost completion acknowledgement still requires inspection before any retry.

## Source-script decisions

| Original | Local use |
|---|---|
| 01 | **Not run.** Database already exists. Its hardcoded database creation/RCSI change is not needed. No database/server option is changed. |
| 02 | Copy table/index/constraint payload, remove USE/GO; enforce dbo context. |
| 03 | Copy only synthetic staff/catalog/resources. Reuse documented public demo account hash and forced password change; no historical business/payment fixtures. |
| 04 | **Not run.** Hardcoded context and fixed 2099 availability query are not needed to install or verify the demo. |
| 05 | **Upgrade ALTER/backfill is unnecessary for fresh 02**: HoanThanhLuc and both dependent tables' UpdatedBy/UpdatedAt/FKs already exist. Copy only its three additional indexes for completion/payment/refund dates. |
| 06 | Copy unchanged identity CREATE payload into the local transaction. No auto-linking or customer personas. |
| 07 | Copy unchanged three-table/index/constraint DDL into this **local-demo-specific** bootstrap. Replace enterprise/run-name/fingerprint preconditions only in this separate copy with exact local target + empty-database guards. Original 07 is unchanged. |
| 08 | Copy unchanged additive receipt DDL/dynamic constraint/index batch into the transaction. No second ledger or historical money changes. Original 08 is unchanged. |

## Exact commands — preparation first, execution only after approval

Use **Windows PowerShell 5.1 on MSI**. No sqlcmd/NuGet/extra service dependency. The process-only ExecutionPolicy argument below runs this reviewed local script without changing machine/user execution policy.

```powershell
Set-Location -LiteralPath 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend'

# 1. SAFE NOW: file/hash validation only; no SQL connection.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\database\demo-local\Invoke-LocalDemo.ps1
```

Review the SQL, source checksums, exact target and proposed seed first. **Step 2 is NOT AUTHORIZED by this preparation task.** After a new explicit Owner approval, use the provisioning Windows identity that can create the schema in this disposable DB, with dbo as its default schema:

```powershell
# 2. FUTURE OWNER-APPROVED EXECUTION ONLY. Exactly one batch, one transaction.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\database\demo-local\Invoke-LocalDemo.ps1 -Execute -OwnerApproved
if ($LASTEXITCODE -ne 0) { throw 'STOP: inspect the exact demo database outcome; do not reset or blindly retry.' }
```

Do not run original 01–08, use a folder glob, point at master/StudioManager, or edit the repository SQL approval bit. Successful setup reports the approved database and 19 tables. Later invocations refuse because objects already exist. Final output is not live app acceptance. No cleanup/drop script is provided.

The copied public demo accounts are `admin`, `nhanvien1`, `nhanvien2`; the original seed's public demo password is `Admin@123`. All retain **PhaiDoiMatKhau=1**. On an approved matching runtime, login in the staff desktop and complete the required password change for each account before using its service review login. Choose new local demo passwords; do not publish them or copy these sample accounts into a business database.

## Runtime — separate, explicit remaining dependency

**The existing application does not currently support an isolated console/dev pipe. No runtime code was changed by this preparation.** This is a real runtime limitation, not a requirement for a certificate, VM or more SQL UAT tooling:

- IdentityService `Program.Main` refuses interactive execution and uses SCM plus the protected ProgramData service.json.
- IdentityPipeServer hardcodes `StudioManager.Identity.v1`.
- CustomerPipeClient connects to that fixed pipe and verifies its PID against the installed `StudioManagerIdentity` Service through SCM before sending passwords. The staff booking-review page uses this same client.

Consequently, do **not** start the unmodified CustomerClient for this demo: it would select the existing P03 endpoint, not this disposable database. There is no supported `--pipe`/`--console`/connection-string argument to suggest here. Do not edit protected service.json, rename/install a service, redirect the production pipe, or remove SCM verification to force it.

Minimum follow-up source change, kept **separate from SQL bootstrap**: an explicitly opt-in local-development profile that hosts the **same** IdentityPipeServer/Dispatcher/P03/P04 services in an ordinary console process, uses a unique demo pipe, and lets demo CustomerClient + staff review client verify that specific local host before credentials are sent. The installed-service default path, production SCM checks, application auth/ownership and SQL writers must remain unchanged. Both the console host and isolated staff app must use the exact demo connection string above; the customer client still receives no SQL/repository API. This profile is not implemented or authorized for execution here and needs its own small reviewed code change, not a new service architecture.

For the staff executable alone, a separate output directory with the supplied JSON named appsettings.json would select the demo database through the existing startup code. No such output/config copy or launch was performed. This does not solve CustomerClient/service/review routing, so it is **not** a full-demo workaround.

**SQL preparation is ready; the complete three-process demo runtime is BLOCKED until that explicit local-development profile is prepared/reviewed.** No VM, trusted certificate infrastructure, service installation or existing P03 maintenance window is proposed.

## Prepared end-to-end checklist — NOT EXECUTED

After separate approvals for setup and the matched isolated development runtime:

1. Verify console authority, staff app and customer client all select the same demo target/unique pipe, while installed P03 stays untouched. Change the seeded staff passwords through the real staff auth workflow.
2. Register synthetic customer A in the real CustomerClient → login → edit profile.
3. Close/restart client → login → verify persisted profile.
4. Choose package/add-ons and a future Vietnam-local slot → preview → submit PENDING. Confirm history and no official booking/payment yet.
5. Staff service login → approve with an active room/photographer → verify exactly one official booking visible to A.
6. Staff opens that booking → records CASH deposit and/or BANK_TRANSFER_DEMO partial/remaining collection with explicit confirmation → verify saved ledger, method, balance and audit. No real funds transfer.
7. Open/print the saved receipt → reopen the same ID/code from history. Reject subcent/overcollection/double-click duplicates.
8. Restart the matched demo processes → reload profile, booking, balance/history and reprint the same receipt from SQL.
9. Login as B → deny A's booking/payment/receipt. Reassignment should deny A's current booking/payment view while retaining own request history.

Record actual results only after these steps run. Current preparation proves file structure/payload preservation and offline parsing only; it proves neither SQL execution nor application/runtime acceptance.
