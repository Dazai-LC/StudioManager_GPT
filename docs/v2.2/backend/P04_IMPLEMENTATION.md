# P04 — Booking request implementation

Baseline inspected before implementation: `5f14c3b`, `feature/v2.2-backend`, .NET 8 / SQL Server / native WinForms. Installed runtime, P02/P03 databases, P03 migrations/backups and Frontend worktree are outside this change.

Recovery completed in source and offline verification on **15/11/2026, Asia/Saigon**. **IMPLEMENTATION PASS; LIVE SQL/UAT NOT EXECUTED; DEPLOYMENT NOT PERFORMED.** The [functional audit](P04_FULL_FUNCTIONAL_AUDIT.md) is retained unchanged as the pre-recovery snapshot. Current fixes/evidence supersede its open-source-defect status, without retroactively changing its observations.

## Confirmed schema mapping and minimum design

Mapping established from `database/02_CreateSchema.sql`, `05_UpgradeToSpec2_1.sql`, `06_P03_CustomerIdentity_REVIEW_ONLY.sql`, Domain models and SqlStudioRepository before implementation:

| Existing records | P04 use |
|---|---|
| GoiChup: GoiChupId, MaGoiChup, TenGoiChup, GiaGoi decimal(18,2), ThoiLuongPhut, MoTa, TrangThai | Package catalog; only DANG_AP_DUNG. Existing schema has no separate published flag. |
| DichVu: DichVuId, TenDichVu, DonViTinh, DonGia decimal(18,2), MoTa, TrangThai | Standalone add-ons; only DANG_CUNG_CAP. Services tied to TaiNguyen.DichVuThueId are excluded because purchasing a rental must not imply equipment allocation. |
| PhongChup: PhongChupId, MaPhong, TenPhong, MoTa, TrangThai | Optional preferred room; staff must assign an active room when approving. |
| NhanVien: NhanVienId, HoTen, ChucVu, TrangThai | Staff selects an active NHIEP_ANH_GIA. Customer catalog does not expose employee details. |
| LichChup: KhachHangId, GoiChupId, TenGoiChot, GiaGoiChot, BatDau/KetThuc datetime2(0), NhiepAnhGiaId, PhongChupId, TrangThai, CreatedBy/UpdatedBy | Official booking created only by staff approval, initially DA_DAT_LICH. Existing status lifecycle remains separate. |
| LichChupDichVu: LichChupId, DichVuId, TenDichVuChot, DonViTinhChot, DonGiaChot, SoLuong decimal(10,2) | Official add-ons use the reviewed request snapshots. |
| TaiKhoanKhachHang / SoHuuKhachHang | P03 actor and approved historical customer link; no contact matching. |
| KhachHang: bigint ID, unique MaKhachHang, name, phone varchar(15), email, address | Use approved ownership if present; otherwise create a distinct customer within the approval transaction. Reuse only a customer originally created by an earlier approved P04 request for this same account and not owned by another account. No automatic P03 ownership grant. |
| NhatKyHeThong | Staff decisions and official booking creation audit within the approval transaction. Customer events use the new request history because this existing audit requires an internal TaiKhoanId. |

Three additive tables: YeuCauDatLich (actor, contact/quote/time/room snapshots, terminal review and official reference), YeuCauDatLichDichVu (selected add-on snapshots), YeuCauDatLichLichSu (customer-visible state events). No duplicate catalog/booking/business tables.

PENDING can transition exactly once to APPROVED, REJECTED or CANCELLED. Only staff approves/rejects; only the submitting customer withdraws a pending request. A unique request-to-LichChup index prevents reuse of an official booking. A customer-scoped submission key supports identifying an already accepted submission without automatic transport retries.

## Transaction and pricing decisions

The existing official-booking insertion is a transaction-scoped kernel shared by the internal booking repository and P04: `SqlStudioRepository.CreateBookingWithinTransactionAsync`. Public internal contracts remain unchanged. Approval owns the single transaction covering actor/status/catalog rechecks, room/photographer conflict checks, customer resolution, LichChup/add-ons, request state/history and audit. Existing internal schedule writers and P04 use the same room/photographer locking rules and half-open overlap test; no pending request reserves a resource.

Request timestamps are UTC. Existing LichChup schedule columns are Vietnam local wall time; explicitly convert at that boundary, retaining compatibility with the internal screens and existing schedules. Customer/staff P04 UI displays Vietnam time. Package duration is authoritative and snapshotted; staff assigns photographer/room without silently rescheduling the requested time.

Preview reads authoritative catalog values and is cached only in the authenticated service connection. Submission rechecks the quote before persisting PENDING. Snapshots include names, unit prices, quantities, duration, requested room, contact and notes. Approval explicitly accepts the stored estimate as the confirmed price (zero discount); changed active catalog prices never silently rewrite the request. Staff can reject an outdated offer instead.

**Owner-approved F-03 rule:** new P04 package prices, each add-on extension and the total must be exactly representable to **0.01 VND**, within `decimal(18,2)`. Quantities remain positive, at most 1000, with at most two decimals. `123.44 × 1.25 = 154.30` is accepted; `123.45 × 1.25 = 154.3125` is rejected with `PRICE_PRECISION`. There is no rounded replacement, adjustment line or silent database conversion. `decimal.Round`/SQL `ROUND` are comparison-only precision checks. Migration 07 now stores request totals as `decimal(18,2)` and constrains exact add-on extensions. No historical amount is rewritten and the existing exact `BusinessRules` formula remains.

New staff collection/refund/discount inputs reject sub-cent or out-of-range amounts before persistence; new/updated service quantities/extensions are checked too. SQL monetary aggregates are explicitly bounded before addition/subtraction to retain fractional scale; stored historical lines remain unchanged. The staff payment dialog accepts cents, and booking/basic receipt displays preserve significant fractional digits. A historical balance not representable in the ledger is reported for explicit review, never auto-rounded or repaired. No new payment, reconciliation, VietQR or PDF workflow is introduced.

## Desktop and authority boundary

Extend the existing P03 service/actor scope with P04 commands. CustomerClient has catalog, preview/submission, own history/details and pending withdrawal; its staff login has a review queue, history, conflict check, approve/reject and official booking view. A shared native control is linked into the existing internal staff request navigation. That page requires explicit P03 service staff login; it never fabricates or transports the internal desktop's session/password hash.

New optional protocol fields are omitted when null, retaining P03 wire shapes. P04 requires reviewed coordinated service/client deployment and the additive migration; old services fail closed for unknown commands. Existing P03 deadlines, SCM/PID/group checks, actor-per-connection and OUTCOME_UNKNOWN handling remain. No write is automatically replayed.

## Completed recovery fixes

| Finding | Source change | Offline evidence |
|---|---|---|
| F-01 | CustomerClient and staff page clear all sensitive booking/profile state before every login attempt. Form/page/control generations discard stale responses, including follow-up initialization; failed staff login remains signed out. | Actual hidden WinForms A→B/role-switch/failed-login/reset-during-await smoke cases; P03/P04 actor tests. |
| F-02 | Customer official query requires both authenticated request ownership and `YeuCauDatLich.KhachHangId = LichChup.KhachHangId`. Legacy staff reassignment remains authorized and audited; it does not transfer request ownership. | Actual legacy `BookingService.UpdateAsync` with shared fake store: same-customer schedule edit remains readable; reassignment hides current details, preserves request history, and staff retains access. SQL predicate review assertion. |
| F-03 | `BookingMoneyPolicy` enforces exact cents at preview/submit/approval/shared writer, with schema/parameter compatibility and related staff input/display guards. | Precision-boundary, quantity, maximum, discount/remainder/overcollection and historical-formula tests; actual cent-input/basic-receipt smoke. |
| F-04 | Staff processing locates account without a retained request lock, locks customer account first, then request and verifies the located owner again. Allocation-before-catalog order matches the original writer. | SQL source-order assertions and fake concurrent approve/cancel outcomes. Actual SQL deadlock behavior is **not** established. |
| F-08 | MainForm disposes removed `BookingRequestsPage` instances only; unrelated pages retain their existing lifetime. Disposing closes its timer, cancels its RPC lifetime, terminally disposes the client, and invalidates callbacks. | Eight repeated actual MainForm navigations, EOF on a unique test-owned pipe, delayed-RPC cancellation and disposed-response suppression. |

The authoritative current booking view is distinct from request history. Reassigning an official booking hides its current details from the original requester while that customer may still read their own original quote, outcome, reason and historical reference. An approved legacy P03 ownership link is not required to read a new own request. A later staff reassignment back to the stored customer satisfies the same continuity rule again; no account is selected by contact matching.

`LockStaffRequest` uses `READCOMMITTED` **only for an initial non-authoritative account locator**. The actual account/request locks and subsequent owner/state checks remain within the serializable transaction. Rejection and availability use the same ordering; approval still checks the target account's active state. This is not a claim of universal deadlock freedom or a change to P03's SQL routines.

## Recovery verification snapshot — before final blocker remediation

Per configuration: solution rebuild **0 warnings / 0 errors**; main suite **195 PASS** (including **62 P04**, **39 P03**, and **72 original business/security cases**); SQL project **209 PASS / 11 live SKIP**. Total xUnit: **404 PASS / 0 FAIL / 11 SKIP** in each of Debug and Release. Nine separate desktop smoke cases passed in each configuration. SQL syntax review parsed 87 extracted fragments with zero failures, without a connection.

Final counts, commands, first-attempt correction and auxiliary-tool warning are in [P04_FINAL_TEST_REPORT.md](P04_FINAL_TEST_REPORT.md). Evidence is under `artifacts/p04-offline/recovery-20261115/`. The desktop helper is not part of the solution or a production deployment artifact.

See [security and SQL review](P04_SECURITY_AND_SQL_REVIEW.md) for permissions/transaction limits and [manual UAT checklist](P04_UAT_CHECKLIST.md) for the exact Owner-controlled sequence. No migration, SQL target provisioning, installed-runtime change or deployment is authorized by these documents alone.

## Final blocker remediation — 15/11/2026

The earlier recovery counts above remain historical. The [SQL preflight](P04_SQL_DEPLOYMENT_PREFLIGHT.md) is also preserved unchanged. Its PF-01/PF-02 source gaps are addressed by B-01/B-02; PF-03/B-03 remains an Owner environment decision. Full details and current evidence are in [P04_BLOCKER_REMEDIATION_REPORT.md](P04_BLOCKER_REMEDIATION_REPORT.md).

- **B-01 / PF-01:** migration 07 remains disabled. It now requires exact engine machine/physical machine/named-instance/server/configured-server observations, run-bound disposable database name, database ID/creation time/collation, explicit external preflight and an approved complete baseline fingerprint. The gate checks 16 baseline tables, 164 columns, 27 required FKs and 48 required indexes before unchanged P04 DDL. Supports reviewed standalone named SQL Server 2022+ with compatibility >=160; default instances, LocalDB, clusters, AG databases and unknown observations are refused. Client endpoint/TLS, independent reference-baseline approval and effective runtime rights remain external gates.
- **B-02 / PF-02:** shared `CatalogPricePolicy` validates original decimal/integer or strictly checked culture-specific decimal text before SQL narrowing. Package/service creation/update are guarded at Application and Repository boundaries; actual price parameters are explicitly decimal(18,2) only after validation. Binary floats, malformed text, sub-cent and overflow inputs are rejected. Partial edits omitting price preserve the existing price. `CatalogPriceInput` displays two decimals, preserves invalid raw text instead of rounding/clamping, and blocks save; duration/stock remain integers. No historical values/snapshots are rewritten.
- **PF-04:** unexpected P04 submit/withdraw/approve/reject failures now conservatively return the existing `OUTCOME_UNKNOWN` contract after disconnect. A failed commit acknowledgement is not presented as a known rollback or an invitation to blindly retry. Known business-rule responses and P03 handling remain unchanged.
- **B-03 / PF-03:** design completed for a separate Windows VM/host, with correctly registered service and local clients/SQL; fixed service/pipe names are valid in that separate OS namespace. No installation profile/security bypass was added. Availability/selection is still **OWNER DECISION REQUIRED**.

Current executed offline evidence per configuration: clean Debug/Release rebuilds; main **243 PASS**, SQL offline **209 PASS / 11 SKIP**; **110 P04** and **39 P03 security** focused passes (subsets, not additional passes); separate hidden WinForms smoke **11 PASS**. ScriptDom: **87 fragments / 0 errors**, plus AST baseline comparison **164 columns / 27 FKs / 48 indexes / 0 differences**. Evidence directory: `artifacts/p04-offline/blocker-remediation-20261115-230550/`. Auxiliary smoke builds retain NU1900 for unavailable vulnerability metadata; application solution builds are clean. Live SQL and runtime acceptance are not established.
