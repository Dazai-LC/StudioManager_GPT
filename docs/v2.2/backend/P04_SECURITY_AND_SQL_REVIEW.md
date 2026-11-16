# P04 — Security, SQL and concurrency review

Date: **15/11/2026, Asia/Saigon**. Branch `feature/v2.2-backend`, accepted baseline `5f14c3b`. Status: **source fixes and offline review PASS; migration REVIEW ONLY; live SQL/installed-runtime acceptance NOT EXECUTED**.

The [full audit](P04_FULL_FUNCTIONAL_AUDIT.md) remains the unchanged historical snapshot. This review covers the completed recovery, not email OTP, payment reconciliation, real VietQR, PDF redesign or private media delivery.

## Authority and data isolation

The customer application still references only CustomerApi. The registered Windows Service holds SQL configuration/repositories and a fresh P03 authority per pipe connection. P04 obtains its customer/staff capabilities only from that authority. Fixed dispatcher commands choose the required role; caller account IDs, arbitrary role flags, email/phone matches and client-owned session objects do not grant access.

Existing P03 registration/login/profile/password/security-version checks, active-account/role revalidation, local-only pipe, SCM PID verification, group ACL/NETWORK deny, finite deadlines, strict bounded JSON, TLS configuration validation and sanitized diagnostics remain. No generic SQL/repository command was exposed. New optional protocol properties are omitted when null; P03 wire-shape regression and all 39 P03 tests passed in both configurations. Binary interoperability still requires the later deployment gate.

Customer list/detail/cancel queries use the authenticated account predicate. Current official details additionally require `r.KhachHangId=l.KhachHangId`. This fixes F-02 without adding a P04-table dependency to the original staff edit path: staff may perform the same authorized reassignment as before, but the requester no longer sees the reassigned booking's current schedule/price/personnel. Own original request/history/reference remain. Staff can inspect the booking under staff authorization; no automatic owner transfer occurs.

Customer contact snapshots are private business data, not login authority. Approved P03 ownership is used if present. Otherwise approval reuses only a customer created by an earlier approved request for that same account and not currently approved for another account, or creates a distinct record. This does not insert a P03 ownership grant or unlock historical bookings.

UI F-01 is fixed before authentication awaits. Generation checks prevent old RPC results and chained initialization from repopulating reset or disposed controls. Failed staff re-login remains signed out. These are presentation defenses in addition to backend predicates. Internal constructor injection exists only for offline UI composition; production constructors always use the SCM-verified client, with no test pipe name/fallback in production paths.

## Exact-cent settlement boundary — F-03

Owner explicitly selected **exact 0.01 VND; no automatic rounding**. `BookingMoneyPolicy` validates package price, unit price, each quantity extension and total before new P04 persistence/approval. Maximum is `9,999,999,999,999,999.99`, matching `decimal(18,2)`. Fractional quantities are allowed when their product is exact cents; precision is never “fixed” with a fallback price or adjustment row.

- Request totals and SQL parameters are `decimal(18,2)`. Request-line constraints compare the exact extension with `ROUND(extension,2)` and enforce its range. `ROUND` is not used as the stored amount.
- Submission re-reads authoritative quote/contact; approval/shared writer validate stored snapshot arithmetic again without current-price repricing.
- New staff payment/refund/discount inputs reject precision loss. Service additions/quantity changes also reject sub-cent extensions before writing. Existing authorization, status, remaining-balance, refund limit and audit rules remain.
- Explicit SQL aggregate casts retain four fractional digits for existing raw service products and two for ledger sums before subsequent arithmetic, avoiding scale loss through unconstrained `SUM` result types. This changes no stored historical amount. SQL binding/numerical execution remains to be verified live.
- Payment UI accepts cents; booking/basic receipt strings and grids preserve fractional values. Historical unrepresentable balances are stopped for explicit review; no silent rounding, repair or historical reprice is performed.
- Final B-02 remediation also guards package/service catalog saves in Application and Repository. Original values are checked before explicit decimal(18,2) parameter assignment; raw text is checked before decimal parsing can round. The real catalog input preserves invalid raw text and rejects it rather than silently coercing it. Partial price-free edits retain stored values; no historical data is changed.

Existing `BusinessRules.TamTinh` stays exact, including historical sub-cent products. The multi-format demo receipt and real reconciliation gaps in the audit are not claimed fixed by these basic amount-display changes.

## Transaction/lock sequence — F-04

All P04 repository operations use a caller-owned serializable transaction. The shared official writer never commits independently.

| Operation | Relevant sequence | Persisted outcome |
|---|---|---|
| Customer preview/catalog/list/detail | Current account/security-version check; bounded own/catalog reads | No business write |
| Submit | Customer account update lock → submission-key/request range; authoritative quote recheck | PENDING + snapshots + first history event; no official/customer creation |
| Customer withdrawal | Current customer account update lock → own request update lock → conditional PENDING transition | CANCELLED + history, same transaction |
| Staff check/reject/approve | Staff revalidation → non-retained account locator → customer account update lock → request lock + located-owner recheck | Availability read only, or one terminal decision |
| Approval continuation | Existing photographer/room allocation locks and conflict ranges → catalog → approved-link/provenance/customer resolution → shared official insert → decision/history/audit | One official booking and its approval commit together |
| Legacy direct creation/edit | Existing staff actor/allocation rules; shared create kernel or existing edit transaction | Existing official workflow retained |

The locator is `SELECT ... WITH(READCOMMITTED)` inside the transaction. It supplies an account ID only; its request shared lock is not retained before account locking. It is not an authorization/state observation. After locking the account then the request, code verifies that the request still belongs to the located account; mismatch fails closed. Actual pending transitions retain `UPDLOCK/HOLDLOCK`, exact row counts, FK/unique constraints and serializable isolation. No `NOLOCK`, uncommitted read, retry loop or lock bypass was introduced.

Allocation is checked before catalog locks to follow the established writer's order. Room/photographer conflicts are half-open overlaps of non-cancelled official bookings; pending requests do not reserve resources. A catalog-active room is not a guaranteed free slot.

**Limit:** source-order tests and a serializing in-memory fixture do not prove SQL lock behavior or eliminate every deadlock. Live acceptance must include different staff account IDs, approve↔withdraw, legacy create/edit, and P03 ownership review/revocation interactions. A deadlock/timeout must preserve rollback or uncertain-outcome reporting; do not blindly retry a write. P03 SQL routines were not rewritten in this recovery.

## Migration 07 review

`database/07_P04_BookingRequests_REVIEW_ONLY.sql` remains disabled with both approval bits zero and expected values NULL. Final B-01 remediation pins machine/physical machine/named instance/engine server/configured server, run-bound disposable name, database ID/creation time/collation and independently approved complete baseline fingerprint. It refuses missing/unknown/partial/unexpected objects and unsupported default/LocalDB/cluster/AG targets. Required structure: 16 tables, 164 columns, 27 required trusted FKs and 48 required PK/unique/filtered/scheduling indexes, plus complete CHECK/default/column/index/FK definitions pinned by reference fingerprint. P02 acceptance markers are not required or reused.

The supported SQL contract is standalone named SQL Server 2022+ / compatibility >=160. Engine `InstanceName`, machine properties and derived machine-instance shape are checked independently of configurable `@@SERVERNAME`. Exact expected observations are binary/length compared, including separately pinned case differences. Client DNS/alias/endpoint/TLS, approved reference-baseline semantics, exclusive provisioning and restricted effective runtime rights require the explicit external gate; the SQL script does not certify them. No fingerprint has been approved/calculated against a live engine in this pass. Never bless an unreviewed candidate by copying its own hash.

There is no `USE`, cross-database write, business seed/backfill, GRANT or destructive cascade. INSERTs into table variables describe the gate only. The original three-table DDL remains unchanged in one XACT_ABORT transaction with rollback/rethrow and all seven filtered-index SET options. P02/P03 migrations remain unchanged. Syntax/AST checks are offline, not SQL engine execution.

| New table | Purpose and constraints |
|---|---|
| `dbo.YeuCauDatLich` | Customer FK, immutable submission/quote snapshots, four-state terminal metadata, preferred room, reviewer, resolved business customer and official booking FKs; unique `(account,key)` and filtered unique official reference; owner/queue indexes. |
| `dbo.YeuCauDatLichDichVu` | Request/service composite PK, existing service FK, immutable name/unit/price/quantity snapshots; exact-cent extension and range checks. |
| `dbo.YeuCauDatLichLichSu` | Request/staff FKs, UTC state events, one event per request/state, correct customer/staff actor shape. |

Existing `LichChup`, `LichChupDichVu`, `KhachHang`, catalog, finance and audit tables remain the business model. No parallel booking engine/table is added. P02/P03 migrations were not changed. Source/schema syntax was reviewed offline; no target, permissions, schema binding or DDL execution was verified.

The submission key provides exact key/content idempotency, not a ban on every identical independently previewed pending request. Official uniqueness/state/resource guards are the final duplicate/conflict boundary. No write is automatically replayed after `OUTCOME_UNKNOWN`.

## Minimum service principal permissions for Owner review

This is a capability inventory, **not executed grants**. Confirm exact object-level permissions on a separately approved disposable P04 target. Retain existing restricted P03 rights and add only what P04 actually uses.

| Objects | Requested runtime operations |
|---|---|
| `TaiKhoan`, `NhanVien` | SELECT for current internal actor/photographer checks |
| `GoiChup`, `DichVu`, `PhongChup`, `TaiNguyen` | SELECT for catalog/activity/rental exclusion and allocation checks |
| `TaiKhoanKhachHang`, `SoHuuKhachHang` | Existing P03 SELECT/INSERT/UPDATE; P04 reads/revalidates locks |
| `KhachHang` | SELECT and INSERT only for approved-link resolution or distinct new customer |
| `YeuCauDatLich` | SELECT/INSERT/UPDATE |
| `YeuCauDatLichDichVu`, `YeuCauDatLichLichSu` | SELECT/INSERT |
| `LichChup`, `LichChupDichVu` | SELECT/INSERT for official creation/summary; no general legacy mutations are exposed on the pipe |
| `NhatKyHeThong` | INSERT; independent audit verification uses a separately approved read identity |

No runtime `DELETE`, DDL, backup/restore, `CONTROL`, `db_owner`, `sysadmin`, server-wide writes or access to another database is proposed. Object metadata/lock-hint behavior and the actual effective rights still require verification by the restricted service identity. A provisioning identity must not be used as runtime/test authority. The existing internal staff app retains its separate trusted composition/permissions.

## Navigation lifecycle — F-08

MainForm explicitly disposes removed connection-owning request pages, with layout resumed in `finally`; unrelated pages keep their existing lifetime. The request page stops/disposes its timer, cancels the RPC token, invalidates its generation, and begins terminal client disposal. It does not synchronously block the UI for an in-flight RPC. Client disposal already marks the client terminal before waiting for its gate; a queued or future login cannot reopen it. Late continuations check lifetime/generation before updating controls or opening official dialogs.

Offline smoke verified repeated actual MainForm unmounts, cancellation of a delayed mock login, suppression of stale/disposed responses, and EOF on a uniquely named test-owned pipe. It did not connect to `StudioManager.Identity.v1`, inspect/operate the installed service, or query SQL.

## Deployment, rollback and remaining gates

No P04 database is assumed approved. Do not reuse production, P02 or P03 test databases. Review a named disposable P04 database with the baseline/P03 schema and synthetic records before enabling an Owner-controlled copy of migration 07. Review restricted identity grants separately.

Production transport has fixed SCM service/pipe/configuration identity and rejects interactive hosting. B-03 proposes a separately approved Windows VM/host with its own correctly registered service, matching local clients and preferably local named SQL instance. Its OS namespace prevents collision with the host's accepted P03 installation. Local client-group SIDs must be created/verified there; do not copy the MSI group's SID. A remote SQL design would require a separately approved network-capable restricted service identity rather than assuming a local virtual service SID authenticates remotely.

If no separate OS is available, a same-host coordinated maintenance/reconfiguration/rollback plan needs explicit Owner approval; no same-host parallel profile is implemented. Do not bypass SCM or launch an unregistered look-alike server. B-03 is **DESIGN COMPLETE / OWNER DECISION REQUIRED**, not runtime acceptance. No installed service/configuration changes are authorized by this report.

Back up complete matching binaries/configuration/ACLs before a later approved deployment. Binary rollback and SQL recovery are separate: do not delete approved requests, official bookings or audit/history to make older binaries fit. Migration is one-time; existing objects cause refusal, not destructive rerun. After business data exists, prefer reviewed forward correction or an explicitly approved isolated database restore.

Remaining acceptance is listed in [P04_UAT_CHECKLIST.md](P04_UAT_CHECKLIST.md): actual SQL permissions/binding/atomicity/rounding behavior, cross-writer concurrency, real Windows identities/client lifecycle and end-to-end desktop behavior. No claim of production readiness or universal SQL deadlock freedom is made.

## Final remediation evidence and uncertainty handling

PF-04 unexpected P04 mutation exceptions now return `OUTCOME_UNKNOWN` after clearing session/preview; the response contains a correlation ID, never raw provider details. This conservatively includes pre-commit failures because the dispatcher cannot prove the commit stage. Fixed business-rule outcomes remain specific. Tests cover both pre-commit failure and fake committed-write/failed-acknowledgement cases; reload never replays the write. Existing P03 exceptions and transport guards are retained.

Current per-configuration results: main 243 PASS, SQL offline 209 PASS/11 SKIP, focused P04 110 PASS, focused P03 security 39 PASS, separate desktop smoke 11 PASS. The [remediation report](P04_BLOCKER_REMEDIATION_REPORT.md) records commands, source/binary hashes and limits. PF-05 real runtime session/options/provider behavior and PF-06 cross-writer physical locking remain **UNKNOWN**, not source fixes inferred from fake tests. Do not broaden privileges or rewrite P03 SQL to hide these unknowns. The original [preflight](P04_SQL_DEPLOYMENT_PREFLIGHT.md) remains historical.
