# P04 — Final blocker remediation report

Date: **15/11/2026 (Asia/Saigon)**. Repository: **G:\AnhStudio_Modern_v2\StudioManager-v2.2-Backend**. Branch **feature/v2.2-backend**, accepted P03 HEAD **5f14c3b**, unchanged.

**IMPLEMENTATION: PASS for the approved source/design and offline verification scope.** This is not SQL execution or runtime acceptance. **B-01: FIXED IN SOURCE. B-02: FIXED IN SOURCE. B-03: DESIGN COMPLETE / OWNER DECISION REQUIRED.** The original SQL deployment preflight and functional audit are preserved as historical snapshots.

## 1. Original findings and disposition

| User blocker / original preflight ID | Original issue | Current disposition |
|---|---|---|
| B-01 / PF-01 | Migration checked database name but lacked exact instance and complete baseline gate | Fixed in source; disabled template now refuses incomplete target/reference/external approval inputs |
| B-02 / PF-02 | Legacy package/service save could accept sub-cent prices; editor used integer price controls | Fixed in source at raw input, application, repository, SQL parameter and real WinForms boundaries |
| B-03 / PF-03 | Fixed SCM/pipe/config contracts prevent an unchanged same-host parallel installation | Separate Windows VM/host design complete; no environment selected/provisioned/verified |
| PF-04 | SQL commit acknowledgement failure could become a generic retry-oriented SERVICE_ERROR | Unexpected P04 mutations now conservatively report existing OUTCOME_UNKNOWN and require reconciliation |
| PF-05 | Real SQL session SET/options/row counts/provider binding unknown | Remains live verification; no speculative runtime/global option change |
| PF-06 | Cross-writer physical lock behavior unknown | Remains live verification; no speculative P03 SQL/lock rewrite |

No second booking engine, new authentication provider, OTP, QR, receipt feature, database installer framework or runtime test service was introduced.

## 2. Exact changes and preserved scope

| File | Change |
|---|---|
| database/07_P04_BookingRequests_REVIEW_ONLY.sql:8 | Explicit disabled Owner/external approvals, mandatory target/run/reference inputs |
| database/07_P04_BookingRequests_REVIEW_ONLY.sql:33 | Real engine instance/machine properties, derived identity checks and exact observations |
| database/07_P04_BookingRequests_REVIEW_ONLY.sql:74 | Complete baseline column/FK/index declarations and validation before unchanged P04 DDL |
| database/07_P04_BookingRequests_REVIEW_ONLY.sql:387 | Canonical full-definition baseline SHA256 comparison |
| src/StudioManager.Application/CatalogPricePolicy.cs:6 | New shared raw catalog-price validation policy |
| src/StudioManager.Application/Services/StudioServices.cs:191 | Guard catalog prices before authorization/persistence |
| src/StudioManager.Infrastructure/SqlStudioRepository.cs:388 | Guard direct shared-save callers before opening SQL; both insert/update use validated price binder |
| src/StudioManager.Infrastructure/SqlStudioRepository.cs:629 | Explicit decimal(18,2) price parameter only after validation |
| src/StudioManager.WinForms/Controls/CatalogPriceInput.cs:6 | New two-decimal input that retains invalid raw text instead of rounding/clamping |
| src/StudioManager.WinForms/Pages/CrudPage.cs:1404 | Price-specific control and pre-save validation; integer controls/public save contract retained |
| src/StudioManager.IdentityService/IdentityDispatcher.cs:89 | P04 unexpected mutation errors reuse OUTCOME_UNKNOWN after disconnect |
| tests/StudioManager.Tests/P04BlockerRemediationTests.cs | 48 new cases for all catalog write paths/raw types/SQL parameters/migration structure/uncertain outcomes |
| tests/StudioManager.Tests/BookingRequestFixture.cs | Fake post-commit acknowledgement failure, in addition to existing pre-commit failure |
| tests/StudioManager.Tests/BookingRequestSqlReviewTests.cs | Permit only table-variable INSERT declarations in migration gate; persistent DML still forbidden |
| artifacts/p04-offline/desktop-smoke/Program.cs | Two additional real-control smoke cases, synthetic transport only |
| P04_IMPLEMENTATION.md / P04_SECURITY_AND_SQL_REVIEW.md | Current safeguards, limits and proposed topology |
| P04_FINAL_TEST_REPORT.md / P04_UAT_CHECKLIST.md | New executed offline evidence and outstanding live gates; older results retained as snapshots |
| docs/v2.2/backend/P04_BLOCKER_REMEDIATION_REPORT.md | This report |

The existing request lifecycle, preview/submit snapshot validation, ownership/customer resolution, shared official writer, allocation checks, finance formulas and ledger guards remain. No historical monetary value, business row or snapshot was touched. P02/P03 SQL migrations and P03 identity SQL remain unchanged. No frozen Frontend files, installed binaries, protected service.json or unrelated P02/P03 artifacts were changed.

There was no Git reset/clean/restore/stage/commit/push/merge/branch switch. Build/test outputs and review evidence are confined to Backend output/artifact directories; no package was published/deployed.

## 3. B-01 — Migration identity, database and baseline gates

### 3.1 Disabled inputs and exact SQL identity

Repository defaults remain **OwnerApprovedExecution = 0** and **ExpectedDatabaseName = NULL**. A second **ExternalPreflightApproved = 0** explicitly represents conditions SQL cannot establish. All expected identity/run/database/fingerprint inputs default NULL; missing/empty/unexpected inputs refuse execution.

Mandatory record:

| Expected input | Meaning |
|---|---|
| MachineName / PhysicalMachineName | Separately pinned engine machine and current physical NetBIOS host |
| InstanceName | Non-null named instance; default MSSQLSERVER refused |
| ServerName / ConfiguredServerName | Separately pinned SERVERPROPERTY('ServerName') and @@SERVERNAME |
| DatabaseName / RunId | Exact StudioManager_P04_Test_ followed by the run GUID's 32 lowercase hex characters |
| DatabaseId / DatabaseCreatedAt | Refuse accidental replacement/recreation under the same name |
| DatabaseCollation | Exact reviewed database/string contract |
| BaselineFingerprint | Exactly 32-byte SHA256 of the independently approved complete baseline inventory |
| ExternalPreflightApproved | Owner-approved endpoint/TLS/rights/exclusive provisioning/topology evidence |

Actual SQL engine properties are used, not an arbitrary client server label. @@SERVERNAME is configurable and may be stale after a rename; it is not sole authority. The script verifies a consistent machine + named-instance shape as well as exact separately expected observations. Property case differences can be legitimate, so the structural check is case-insensitive while each observed value is pinned with binary collation and byte length, including trailing-character rejection. It never repairs server metadata or changes SQL configuration. [Microsoft: SERVERPROPERTY semantics](https://learn.microsoft.com/en-us/sql/t-sql/functions/serverproperty-transact-sql?view=sql-server-ver17), [@@SERVERNAME and rename behavior](https://learn.microsoft.com/en-us/sql/t-sql/functions/servername-transact-sql?view=sql-server-ver17).

Supported scope is deliberately explicit: **standalone named SQL Server 2022+**, approved compatibility **>=160**, ordinary writable online database. Default instances, LocalDB, clustered instances, AG databases, database snapshots, unknown engine observations and inconsistent/stale names refuse. The run-bound name rejects StudioManager and existing P02/P03 databases without supplying any real target in source.

### 3.2 Complete required baseline and unchanged additive DDL

Before any persistent DDL, the template validates:

- **16 baseline tables and 164 columns**, including complete current business and P03 identity fields: native type, string byte length, decimal precision/scale, datetime2 scale, nullable/identity shape, identity seed/increment and string collations. Unexpected/missing columns/tables are refused.
- **27 required trusted single-column FKs**, parent/child field mapping and non-cascade behavior.
- **48 required PK/unique/filtered/scheduling indexes**, ordered keys, uniqueness/PK flags, active state and simple filtered predicates. This includes necessary 05 indexes and P03 approved/pending uniqueness.
- No unexpected baseline user tables/views/procedures/synonyms/functions/triggers; no existing/partial/incompatible P04 object in another schema/type.
- Full CHECK/default/column/index/FK definitions via a canonical XML inventory and SHA2_256 equality against the approved reference. Generated constraint names and object IDs are omitted. Missing/untrusted CHECKs and semantic changes are not accepted merely because columns exist.

The fingerprint protects complete definitions, not merely a count or SQL source-file checksum. It is an explicit external dependency: approve the full reference inventory independently, then record its digest. The reference may be the newly provisioned controlled baseline **after** independent definition review; a second database is not inherently required. Never copy an unreviewed candidate's current hash as proof it is valid. Engine formatting/version/schema differences can fail closed and require review; no automatic normalization/acceptance is supplied.

For future reference review, the canonical block beginning at DECLARE @BaselineInventory and ending at DECLARE @ObservedBaselineFingerprint contains metadata SELECTs only. An approved operator can use that exact block followed by a digest/inventory SELECT in a separately reviewed read-only procedure. That is not permission to execute the migration, its table-variable setup or any SQL now. No live digest was collected here.

The P04 three-table CREATE/constraint/index tail is retained unchanged. XACT_ABORT, seven filtered-index SET options, one transaction, rollback/rethrow, original system/unexpected-database guard and rerun refusal remain. Gate INSERTs populate local table variables only; no baseline/business DML, grants, seed/backfill, database switch or cross-database write was added. No P02 acceptance-run marker/table/principal is required.

### 3.3 Conditions that remain external

SQL properties do not authenticate the client's DNS/alias/endpoint or certificate, prove OS/SQL effective minimum rights, select an available VM, or prevent a privileged administrator changing the environment. The required external approval covers endpoint/TLS/certificate policy, independently trusted reference inventory, exact database record, exclusive provisioning window, setup identity and restricted later runtime/verifier/staff identities.

Keep a fresh dedicated connection with no ambient transaction; verify and execute on the same approved target, under separate Owner execution approval. Do not weaken TLS, broaden runtime rights or run as SQL sysadmin to force acceptance. The template's metadata visibility requirement belongs to the migration identity, not the Service.

Static syntax/AST verification succeeded. Actual SQL semantic compilation, effective rights, gate negative cases, DDL rollback and runtime binding remain **NOT EXECUTED**.

## 4. B-02 — Original price validation before narrowing

The public catalog dictionary/save signatures remain unchanged. Partial updates omitting price keep stored price. Supplied prices must be valid non-negative exact cents within **9,999,999,999,999,999.99**.

- Decimal values are checked unchanged with BookingMoneyPolicy.FitsLedger before any SQL metadata assignment.
- Integer values convert exactly. Binary float/double, null and malformed price values are rejected.
- Text callers remain supported with the current culture's decimal separator, without thousands separators/exponents. Fractional information beyond two digits is inspected **before decimal.TryParse**; non-zero sub-cent input is rejected even if a long decimal parser would round it. Additional fractional zeros do not change the amount.
- Both AdministrationService.SaveAsync and direct SqlStudioRepository.SaveSimpleAsync reject invalid price before persistence. Actual insert/update price parameters use Decimal precision 18/scale 2 only after that validation; the binder checks again and provides no rounded fallback.
- Real package/service editors display two decimals and retain raw invalid text on validation/value access/spin actions. Save refuses the raw value. Integer duration/stock controls retain their earlier behavior; price range now matches storage rather than clamping an existing valid cent price to the old UI maximum.

| Input / calculation | Decision |
|---|---|
| 100.00 / 123.45 / zero / exact ledger maximum | Accept unchanged |
| 123.456 / 0.001 / negative / overflow | Reject, no persistence |
| Long raw 123.450...001 | Reject before parser rounding |
| 123.44 × 1.25 = 154.30 | Existing P04 extension remains accepted |
| 123.45 × 1.25 = 154.3125 | Existing P04 extension remains rejected |
| Historical prices/payment/snapshots | Not rewritten, rounded or repriced |

SQL narrowing can round over-scale inputs before a column CHECK sees them; therefore parameter scale/CHECK alone is insufficient. The new protection validates raw input first. [Microsoft: decimal conversion](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql?view=sql-server-ver17).

P04 quote/total limits, quantity checks, overflow handling, payment/refund/discount guards, remaining-balance arithmetic and real basic receipt amounts remain. No new finance/receipt features or whole-VND policy were introduced.

## 5. PF-04 — Uncertain mutation outcomes

The dispatcher now maps any unexpected exception during P04 submit/withdraw/approve/reject to the existing **OUTCOME_UNKNOWN** contract, after clearing authentication and cached preview. It gives a correlation ID and explicit reauthenticate/reload/no-automatic-resend guidance. Raw provider messages, SQL text, credentials and paths remain omitted.

This is conservative: an unexpected pre-commit failure also reports uncertain because the dispatcher cannot safely prove a transaction's commit stage. Known business-rule refusals retain their existing specific codes. P03 exception handling/commands remain unchanged.

Two new fake transaction cases cover failure before commit and failure after fake committed state but before acknowledgement; both require sign-in/reload, expose no synthetic secret, and never replay automatically. Existing real local test-pipe lost-reply/no-replay coverage also remains green. These tests do not prove SQL commit-loss/rollback behavior.

## 6. B-03 — Separate registered Windows acceptance runtime

**DESIGN COMPLETE / OWNER DECISION REQUIRED. No VM/host availability, account, SQL target or runtime has been provisioned/verified.**

Preferred topology is a dedicated Windows VM, or separate Windows host, with SQL and both native WinForms clients on that same guest/host. This respects the same-machine Windows Service authority boundary.

| Component | Required property |
|---|---|
| Windows environment | Separate OS namespace, .NET 8 Windows/desktop runtime and reviewed dependencies; ordinary client desktop session |
| SQL | Named standalone SQL Server 2022+ local to that OS, compatibility >=160; new run-bound disposable database and synthetic data only |
| Registered service | StudioManagerIdentity, properly registered with SCM, reviewed protected executable path/account/startup/security; interactive host rejected |
| Pipe | StudioManager.Identity.v1 in that guest/host; fixed protocol, network deny, explicit service/client ACLs and PID/SCM verification retained |
| Service configuration | That OS's %ProgramData%\StudioManager\IdentityService\service.json; Owner-protected ACL; Windows authentication and reviewed certificate validation |
| Service SQL identity | Restricted non-administrator Service identity mapped only to the disposable target; no db_owner/sysadmin/DDL/Finance/backup/restore authority |
| Client group | Dedicated local guest/host group and its actual SID; ordinary customer users have no SQL/protected-config rights |
| Staff application | Separate trusted staff SQL composition for legacy Booking/Finance/CRUD; request page still signs into Service |
| Read-only verifier | Separate synthetic-evidence/metadata permissions, no mutation or credential-read requirement |
| Binaries | Matching P04 CustomerApi/IdentityService/CustomerClient and updated staff package/dependencies; bind hashes to accepted source |

Source assumptions are compatible with another Windows OS: there is no production MSI host name in the service/pipe verification contract. Configuration location is derived from that OS's common application data directory. Client group SID is configuration, not a reusable MSI SID. The compiled service/pipe names can stay identical because SCM and local named-pipe namespaces are separate there.

Prefer local SQL so the virtual NT SERVICE\StudioManagerIdentity account can be reviewed/mapped locally. Do not assume that SID authenticates to remote SQL; a remote SQL topology needs a separately approved domain/network-capable restricted account and endpoint/TLS design. Do not distribute SQL access/configuration to customer clients.

### Setup order after separate approvals

1. Owner selects/verifies the available VM/host, guest isolation, exact target/run and four identity profiles; reviews packages/ACL/backup plan. Shared host source/package access should be read-only as needed.
2. **B approval:** provision the disposable environment, reviewed target-bound business/required-05/P03 baseline, synthetic principals/fixtures and exact minimum rights. Do not run original hardcoded StudioManager scripts or copy production/P02/P03 data.
3. Independently review full baseline/definitions and approve reference digest/target observations. Verify restricted tokens, protected DB denial and runtime-session options. No permissive repair.
4. **C approval:** enable only a hashed execution copy of 07 after all target/baseline/external inputs are pinned. Execute/verify within its approved isolated scope.
5. **D approval:** register/configure/deploy the real service and matched clients in the chosen OS with fresh backups and hash checks. No mock/unregistered host or SCM bypass.
6. Verify SCM Running/PID/identity/configuration/ACL and exact pipe-server PID under an ordinary local client. Successful startup is not long-term stability or booking acceptance.
7. Perform P04_UAT_CHECKLIST.md with independent SQL evidence: submission, state/ownership isolation, approval/atomic snapshots/audit, rejection/withdrawal, exact money, legacy Booking/Finance/resources, P03 ownership and cross-writer races.

### Backup/rollback boundaries and alternative

Before D, take a fresh complete hash-verified backup of guest service/customer/staff payloads and protected configuration/ACLs, plus the approved disposable database backup/checkpoint at the reviewed boundary. Keep backups outside customer access. Record approved target/run/source/package hashes without secrets.

Failed file deployment/startup restores matched payloads/configuration/ACLs and verifies original state. Database recovery is separate: preserve approved request/official/audit/history data; use forward correction or separately approved restore/recreation of **only** the disposable database. A binary rollback is not permission to delete business/audit rows. Review VM checkpoints with database state consistency; never restore over host P03 or production.

If a VM is unavailable, a separate physical Windows host with local SQL is a compatible alternative requiring explicit selection/approval. If neither separate OS is available, stop for explicit approval of a same-host maintenance window with fresh backup, coordinated P03 service/config/client change and tested rollback. Fixed names do not allow same-host parallel operation merely by renaming the service. No alternative source profile, service installation or security bypass was implemented.

## 7. Actual offline regression evidence

Evidence directory: **artifacts/p04-offline/blocker-remediation-20261115-230550/**.

| Executed check | Debug | Release |
|---|---:|---:|
| Full solution rebuild | PASS, 0 warnings/0 errors | PASS, 0 warnings/0 errors |
| StudioManager.Tests | **243 PASS / 0 FAIL / 0 SKIP** | **243 PASS / 0 FAIL / 0 SKIP** |
| StudioManager.SqlIntegrationTests OFFLINE | **209 PASS / 0 FAIL / 11 SKIP** | **209 PASS / 0 FAIL / 11 SKIP** |
| P04 focused rerun, subset of main | **110 PASS** | **110 PASS** |
| P03 security focused rerun, subset of main | **39 PASS** | **39 PASS** |
| Separate hidden WinForms smoke | **11 PASS / 0 FAIL** | **11 PASS / 0 FAIL** |
| SQL syntax, configuration-independent | 87 fragments / 0 errors | Same static review |
| Baseline ScriptDom AST comparison | 16 tables / 164 columns / 27 FKs / 48 indexes / 0 differences | Same static review |

Total xUnit **452 PASS / 0 FAIL / 11 SKIP per configuration**. Focused passes are subsets, not additional passes. Desktop smoke is separate. No live SQL fact was enabled; all three PHOENIX_SQL_TEST_* selectors were explicitly cleared in each test/smoke process.

Executed commands used full solution Rebuild with --no-restore, test --no-build/--no-restore with TRX output, explicit Debug/Release and the filters recorded in P04_FINAL_TEST_REPORT.md. Auxiliary desktop build/run used the existing P04-only helper; its two added cases exercise actual price controls/dialog loading/save boundaries, with **25 mock RPCs / 0 production-pipe calls / 0 SQL connections / 0 visible forms** in each run.

Durable artifacts include accepted build logs, main/SQL/P04/P03 TRX files, desktop logs, SQL parser result, AST contract result, source/build SHA256 manifest and verification summary. The accepted source/binary inventory includes the new report/docs; artifact helpers/logs remain review evidence, not automatically commit/deployment candidates.

Initial failures were kept: first Debug main **239 PASS/3 FAIL** and parser unexpected EOF caught a local migration-tail assembly error; restored unchanged original DDL passes final checks. First new desktop case used the wrong synthetic service-price column header (Giá versus Đơn giá), giving 9 PASS/1 FAIL; corrected fixture gives 11/11. The final extra raw shared-save fact raised main/P04 from 242/109 to 243/110. No live database was involved in any failed or corrected check.

Auxiliary desktop builds retain **NU1900** for unavailable NuGet vulnerability metadata. Solution builds remain 0 warnings/errors; no dependency auditing suppression, package upgrade/install or fresh successful online vulnerability-audit claim. Existing VSTest private temporary-artifact fallback notices are retained.

## 8. Remaining unknowns and risks

- The complete baseline/reference digest, real target identity/permissions and migration semantics have not been verified on SQL Server. B-01 is a source fix, not migration acceptance.
- PF-05 actual SqlClient parameters/readers/session SET/NOCOUNT/affected-row behavior remains to be verified under the restricted runtime identity. No global SQL setting was changed.
- PF-06 actual range locks, plans, deadlocks/timeouts and cross-writer P03/legacy interactions remain unverified. No deadlock-freedom claim.
- SQL rollback/commit acknowledgement failure, independent audit persistence and installed Windows token/pipe/configuration behavior need the approved real environment. Fake tests cannot establish these.
- Historical sub-cent balances remain unchanged and require explicit review rather than invented rounding/accounting repair. No live historical data was inspected.
- B-03 availability/selection is still pending. Matching offline assemblies are not a deployed or accepted package.

Existing minimum Service rights remain those in the security/preflight matrices. Do not grant catalog/Finance/general legacy UPDATE/DELETE, sysadmin/db_owner or server-wide privileges to make UAT pass. Provisioner, runtime, verifier and trusted staff remain separate.

## 9. Required Owner decisions and execution gates

| Gate | Status / required Owner action |
|---|---|
| Source/offline remediation | Complete; review these changes and retained evidence |
| B-03 environment | Select and verify available separate Windows VM/host, exact SQL target/run and identities; **not selected by this task** |
| B — provisioning | Separate explicit approval for new disposable OS/SQL baseline/principals/fixtures; not granted or executed here |
| C — migration | Separate approval of exact reviewed execution copy/target/reference fingerprint after independent preflight; repository template remains disabled |
| D — deployment/live UAT | Separate approval for installation/configuration/binaries/service operations/UAT with fresh backup and rollback; not performed |
| Git | No staging/commit/push/merge authorized or executed |

The next concrete Owner-controlled step is **review/accept the remediation, select and verify the separate Windows acceptance environment and exact disposable target/run/identities, then explicitly approve B if desired**. C and D remain subsequent independent approvals; do not enable the template or operate the host P03 service from this report.

## 10. Final assessment

**IMPLEMENTATION: PASS. B-01: FIXED IN SOURCE. B-02: FIXED IN SOURCE. B-03: DESIGN COMPLETE / OWNER DECISION REQUIRED.**

The two confirmed source/migration-safety blockers and PF-04's message/uncertainty gap are addressed. One environment-selection blocker and actual SQL/runtime evidence remain. The historical preflight conclusions were not rewritten.

**Migration execution: NOT EXECUTED. SQL access: NONE. Deployment: NOT PERFORMED. No database/principal provisioning, installed service/configuration/binary change or Git mutation was performed. Stopped after implementation, offline verification and this report.**
