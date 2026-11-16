# Demo core implementation — 16/11/2026

Branch `feature/v2.2-backend`; accepted P03 HEAD `5f14c3b`, unchanged. Results below describe implemented source and offline verification. **No SQL connection, migration, live database acceptance or deployment occurred.** Existing uncommitted P04 work was continued; no reset/clean/stage/commit/push was performed.

## Final state

| Criterion | Result |
|---|---|
| CORE IMPLEMENTATION | **PASS — source/offline scope** |
| BOOKING INTEGRATION | **PASS — existing P03/P04 services and official writer retained** |
| SIMULATED PAYMENT | **PASS — cash / manually confirmed bank-transfer DEMO** |
| SQL PERSISTENCE CODE | **PASS — existing ThanhToan ledger, transactional insert/readback and audit** |
| RECEIPTS | **PASS — persisted identity, preview/A4 print/reprint; actual printer/PDF output not executed** |
| CUSTOMER PAYMENT VIEW | **PASS — backend-authorized, read-only request/booking/payment access** |
| DEBUG BUILD / RELEASE BUILD | **PASS / PASS**, 0 warnings, 0 errors |
| OFFLINE TESTS | Core snapshot: **270 application + 209 pure safety + 11 WinForms smoke = 490 PASS** per configuration; UI follow-up results below |
| LIVE DATABASE CHECK | **NOT EXECUTED** |
| DEPLOYMENT | **NOT PERFORMED** |
| GIT COMMIT/PUSH | **NOT PERFORMED** |

## Implemented functionality and reused components

- Reused real P03 registration/login/profile persistence and BookingRequestService, SqlBookingRequestRepository, request history/approval/rejection, shared official LichChup writer, scheduling and resources. The real customer application is **StudioManager.CustomerClient**. The old frontend demo launcher is explicitly labelled as a mock with no SQL persistence and points users to that application; internal login remains real ADMIN/STAFF authentication.
- Reused FinanceService, existing collection/refund rules, ThanhToan, HoanTien, live booking totals and audit. The original ReceiveAsync API remains compatible; its SQL writer delegates to the shared collection implementation. Old callers without method metadata remain unspecified, rather than being relabelled as cash. No second ledger or payment-provider integration was added.
- Added explicit CASH and BANK_TRANSFER_DEMO selection, separate from DAT_COC / BO_SUNG / CON_LAI collection purpose. Amount text rejects grouping ambiguity, subcent values, invalid ranges and silent rounding. SQL rechecks the authenticated staff, cancellation and current balance under the existing serializable booking lock. Monetary parameters use decimal(18,2).
- Added CollectAsync with a single pending write guard and a persistent unique action key. Repeating an identical confirmed key returns the same stored receipt; changing its contents is refused. No automatic write retry occurs. An uncertain outcome blocks further collections until the latest authoritative balance/history is reloaded. SQL commit failures are reported conservatively, without raw provider errors.
- Added transaction-scoped receipt snapshots from actual database customer/package/service/discount/staff/balance values. The returned transaction ID comes from SCOPE_IDENTITY; receipt opening uses that ID, not the last grid row or entered amount. Later reads use the saved row and snapshot, retaining the same code and values even if catalog/profile/discount/balance changes. Historical rows without snapshots show only verified values and explicitly omit unavailable historical totals/discount/balance/method.
- Added a shared receipt preview using persisted DTOs, multipage A4 printing and reprint. PDF uses an available Windows PDF printer through the print dialog, with no new library. Preview/print does not collect money. Removed the old DesktopDemoSessionStore document-builder action from the live booking receipt path. These receipts explicitly state that they are graduation-project demo receipts, not electronic tax invoices.
- Added customer payment summary/history/receipt commands through the existing authenticated service connection. The backend derives booking identity from the owned APPROVED request and enforces `r.TaiKhoanKhachHangId = actor` plus `r.KhachHangId = l.KhachHangId`. Reassignment denies current payment/receipt access while retaining own request history. Individual receipts must also match the booking and snapshot customer; historical receipts without sufficient ownership evidence remain staff-only. No customer collection/mark-paid command exists.

## Proposed database change — review only

New [08_DemoPaymentReceipts_REVIEW_ONLY.sql](../../database/08_DemoPaymentReceipts_REVIEW_ONLY.sql), disabled by default. It requires explicit Owner approval and exact named instance/server/database ID/creation time/run-bound disposable name, with a reviewed P03/P04 baseline. It refuses existing/partial receipt columns and runs its additive DDL in one transaction.

Three nullable fields on **existing dbo.ThanhToan**:

| Field | Purpose |
|---|---|
| PhuongThucThu varchar(24) | CASH / BANK_TRANSFER_DEMO, independent of LoaiThu |
| YeuCauThuId uniqueidentifier | Unique confirmed-action key, with filtered unique index |
| BienNhanJson nvarchar(max) | Immutable receipt context at collection time; informational snapshot, never the authoritative ledger/balance calculation |

A CHECK requires either all three NULL (historical/legacy records) or a complete supported method/key/valid JSON tuple. No historical UPDATE/backfill, amount changes, FK changes, new business tables, grants or automatic migrations. Original migrations 02/05/06/07 and the P04 SQL UAT tooling/gates were not changed. New receipt-aware collection fails with a clear message before inserting if migration 08 is absent. Independent read-only history supports the older schema; unknown historical methods are not guessed.

The previously empty P04 database grants no execution authority. It still needs its separately reviewed baseline/P03/P04 dependencies and migration 08 before this demo workflow can be accepted. Do not apply these scripts to StudioManager or P02/P03 databases. Review runtime rights separately: staff collection uses its existing ledger INSERT/SELECT and audit rights; the customer service needs read-only access to collection/refund data. No service/configuration/grant was changed here.

## Working UI paths in source

| User | Flow |
|---|---|
| Staff | Booking list/detail → **Thu tiền** / **Thu mô phỏng / số dư** → current totals/history → cash or transfer DEMO + purpose + exact amount → explicit confirmation → saved receipt preview → print/reprint → refreshed balance/history |
| Staff navigation | Payment workspace now opens the real booking/payment workflow instead of PaymentWorkspacePlaceholderPage |
| Customer | CustomerClient → own approved request → **Số dư / thanh toán / biên nhận** → read-only totals, refunded amount and paginated history → authorized saved receipt preview/print |
| Pending customer request | Remains clearly separate from payment completion; no payment screen for an unapproved request |

New reads/writes await async service/repository calls, disable repeated actions and discard stale staff responses after session changes. Existing customer view generations/session reset behavior is reused, and failed authorization clears displayed payment data. Cash and transfer both remain explicitly simulated. Transfer instructions contain no real bank account or scannable payment QR and are not payment confirmation.

## Files created / changed for this slice

New application files:

- `src/StudioManager.Domain/Payments.cs`
- `src/StudioManager.Application/PaymentPolicy.cs`, `Services/FinanceService.Payments.cs`
- `src/StudioManager.Infrastructure/SqlStudioRepository.Payments.cs`, `SqlBookingRequestRepository.Payments.cs`
- `src/StudioManager.CustomerApi/PaymentProtocol.cs`
- `src/StudioManager.CustomerClient/PersistedReceiptForm.cs`
- `src/StudioManager.WinForms/Pages/PaymentWorkspacePage.cs`

Changed existing files:

- Application: `Contracts.cs`, `BookingRequestContracts.cs`, `Services/StudioServices.cs`, `Services/BookingRequestService.cs`.
- Infrastructure: `SqlStudioRepository.cs` (shared payment delegation), `SqlBookingRequestRepository.cs` (partial class for authorized read extension).
- Transport/service: `CustomerApi/IdentityProtocol.cs`, `IdentityService/IdentityDispatcher.Booking.cs` (appended read-only commands; existing enum values/framing/SCM/pipe/TLS behavior retained).
- UI: `CustomerClient/BookingWorkspaceControl.cs`, `WinForms/Pages/BookingsPage.cs`, `WinForms/Forms/MainForm.cs`, `Forms/LoginForm.cs`, `StudioManager.WinForms.csproj` (shared receipt form link).
- Tests: new `DemoPaymentTests.cs`, `DemoCustomerPaymentTests.cs`; extended `BookingRequestFixture.cs` with authorized read doubles and adjusted `BookingRequestSqlReviewTests.cs` to inspect the shared payment partial while retaining its money/ownership assertions.
- New migration 08 and this report. Dedicated evidence under `artifacts/demo-core/offline-20261116-142345/`; exact file hashes/delta in its manifest. No new test project or SQL UAT framework was introduced.

## Offline verification

Initial demo-core snapshot, before the UI consistency follow-up: both Debug and Release rebuilt the existing solution successfully with cached dependencies, no new packages and no warnings/errors. Each configuration passed:

- **270 application tests**, including **27 new demo tests**: method/purpose persistence contract, deposit/partial/remaining payment, overcollection, double-click/pending write, idempotent stored identity, uncertain-result reload, session authorization, receipt arithmetic/reprint/legacy omissions, registration/profile continuity and real P03/P04 local-pipe customer isolation/reassignment. Persistence adapters in these tests are explicit doubles, not SQL evidence.
- **209 existing pure offline safety tests**, explicitly filtered to six classes; no metadata/mutation integration cases were selected. P02 execution selectors were cleared for each command. No live SQL fixture was executed.
- **11 existing hidden WinForms smoke checks** for initialization/session switching/stale callbacks/disposal, staff money/receipt cent display and catalog precision, using fake RPCs and a uniquely named local test pipe. No registered production pipe/SCM or visible UI was used. These are regression checks, not execution of the new cash/printing workflow against SQL.

SQL Server 2022 ScriptDom parsed **18** migration/query fragments and assembled branches with zero syntax errors, without opening SQL. This does not establish semantic binding, live rights, atomic rollback/idempotency behavior or real database persistence. Accepted logs/TRX: `build-{Debug,Release}-accepted.log`, `{Debug,Release}-main-accepted.trx`, `{Debug,Release}-safety-offline.trx`, `desktop-{Debug,Release}.log`, `sql-syntax.json` in the evidence folder. Intermediate compile/notification-test failures were corrected and their logs retained. The test SDK fallback-directory notice did not fail tests.

## Prepared manual demo — **NOT EXECUTED**

Only after Owner reviews the migration, disposable demo database/permissions and matching Service/CustomerClient/staff build:

1. Start the real CustomerClient; register synthetic customer A. Confirm it is not the old mock portal.
2. Login as A; edit contact profile. Close/restart CustomerClient, login and verify saved values.
3. Select a real active package/add-ons, preview the exact price and submit a request. Confirm PENDING/history and no official booking yet.
4. Staff signs in to request review, selects room/photographer and approves. Confirm one official booking and APPROVED status visible to A.
5. Open that official booking in staff UI; inspect current total, collected, refunded and remaining values.
6. Record a **CASH / DAT_COC** simulated deposit with explicit confirmation. Test rapid repeated clicks; expect one saved transaction. Record its actual ID/code.
7. Independently verify the exact dbo.ThanhToan row (booking, amount, LoaiThu, method/key/snapshot), THU_TIEN audit and ledger-derived balance in the approved disposable database. No transfer of real funds.
8. Record a partial **BANK_TRANSFER_DEMO / BO_SUNG** collection. Check demo wording and manual confirmation; instructions/illustrations must not imply bank verification.
9. Record **CON_LAI** for the authoritative remaining balance; verify zero remaining. Reject subcent/overcollection/cancelled-booking attempts without added ledger/audit rows.
10. View/print each saved receipt. Verify stored code, customer, package/service extensions, discount, collection amount, purpose/method, staff/time and balance immediately after that payment. Test A4 pagination and optional installed PDF printer.
11. Reopen the earlier deposit from history after later payments; require the same ID/code and original snapshot balance. Change a permitted current catalog/profile/discount value in a separately approved test and verify receipt snapshots do not drift. Legacy rows must not fabricate missing fields.
12. Restart staff and customer applications; reload totals/history and reprint the same transaction from SQL. This is the required persistence/restart evidence, not satisfied by the offline doubles.
13. Login as customer B; deny A's request/payment/receipt, including altered request/payment IDs. Verify staff login cannot use customer-only commands. Reassign the booking through the existing authorized staff flow; deny A's current payment/receipt while retaining own request history.
14. Test an interrupted/uncertain collection only under a separately approved manual test: inspect authoritative ledger/history before any new action, with no automatic resubmission. Record pass/fail, IDs and discrepancies without secrets.

## Limits, remaining mocks and UI handoff

Live SQL persistence/restart, real permission/session/binding behavior, contention/rollback and physical print/PDF output are not verified. Existing installed P03 binaries/configuration are untouched; they do not automatically acquire these new commands. Coordinated deployment remains an Owner action. PDF has no independent exporter; it depends on a configured Windows PDF printer. Historical receipts without a customer snapshot are intentionally staff-only. No real banking, webhook, OAuth or email OTP is implemented; email verification is postponed to a future release.

Old WinForms CustomerPages/CustomerLogin/CustomerRegistration, DesktopDemoSessionStore, mock payment/album/document layouts and seed-based artwork remain in the old frontend demo area. They are not used for registration, profile, booking persistence, collection or receipts in the real CustomerClient/staff paths described above. The old launcher is clearly labelled; payment navigation in the real staff app no longer mounts the placeholder.

Owner UI rule: new functionality must use the approved existing visual style. Antigravity may refine these screens only within the existing Theme palette, typography, spacing, navigation and control conventions; no new visual identity/framework or redesign of working screens. Preserve raw exact-cent validation, separate method/purpose controls, confirmation/busy/reload states, stored-ID navigation, immutable receipt values, all backend ownership checks and visible DEMO/historical-data wording. Do not reintroduce the old demo receipt builder into the live flow.

### UI consistency follow-up — 16/11/2026

The four new payment/receipt screens now reuse the existing Theme/ModernButton/vector icons, PageHeader, CardPanel, grid styling and AtelierDialog. The rendering-only PageHeader/CardPanel and dialog definitions were moved to standalone source files without changing their bodies, then linked into CustomerClient. CustomerClient still has only its CustomerApi project reference: no staff executable, Application, Domain, repository or SQL dependency was added. Theme.cs is unchanged. Existing authentication/navigation screens retain their behavior and layout.

Fixed the cropped payment summary after font scaling, fixed-width collection fields and fixed-height footer action areas. Summary height follows measured text; field widths follow the card; wrapped action rows stay visible at minimum window size. No business service, payment contract, amount rule, repository, migration, service/configuration or installed runtime was changed by this UI follow-up.

Evidence: `artifacts/ui-consistency/20261116/`, including unchanged-component extraction checks, hidden-form renders and 24 layout variants across four screens, preferred/minimum sizes and synthetic geometry/font scale factors 1.0 / 1.25 / 1.5. Native DPI observed was 120; synthetic scale checks do not establish actual monitor-DPI transitions, live SQL workflows or printer acceptance. The existing native smoke suite now includes these checks as its twelfth case. Owner-approved design conventions also apply to all future screens.

Follow-up verification: Debug/Release builds PASS with 0 warnings/errors; **270 application tests and 12 native smoke checks PASS per configuration**. The new smoke case covers all 24 layout variants. The 209 pure SQL-safety tests retain the earlier core snapshot; they were not rerun for these rendering-only changes. No SQL connection, migration, deployment, installed configuration change or Git mutation occurred.

**Exact next action:** Owner reviews this source diff and migration 08, selects the approved disposable demo target/dependencies/rights, then separately authorizes migration and matched runtime deployment/manual checklist. This task stops after source/offline delivery; it does not authorize those execution steps.
