# Backend final status — 18/11/2026

Repository: `StudioManager-v2.2-Backend`, branch `feature/v2.2-backend`, pushed baseline `c72b4f3` plus the existing uncommitted CustomerClient Atelier integration. That work has been preserved.

**Backend is sufficient for the agreed customer/booking/payment/receipt demo and frontend handoff. No demonstrated Backend blocker or missing API was found in this pass.** Only the two requested handoff documents were added; no application, repository, API, UI, migration or runtime configuration was changed.

| Module | Current implementation |
|---|---|
| P03 identity | Separate customer accounts; real registration/login, contact read/update, password change/logout, durable credential revalidation and account isolation. Historical customer linking requires staff approval; no email/phone matching. |
| P04 requests | Active catalog, decimal add-ons, authoritative preview, submission-key duplicate protection, PENDING/history/cancellation, staff review/approval/rejection and transactional official booking creation. |
| Payments | Active staff UI calls `FinanceService.CollectAsync`; CASH and manually confirmed BANK_TRANSFER_DEMO, separate purpose codes, positive exact-cent amounts, authoritative balance, action-key idempotency, ledger/audit/snapshot write in one transaction and uncertain-outcome handling. Uses existing `ThanhToan`; no second ledger. |
| Refunds/balance | Admin-only refund of cancelled official bookings, bounded by collected minus already refunded. Active remaining balance is total minus gross collections; cancelled bookings return zero remaining. Refunds and net revenue remain separate totals. |
| Receipts | Saved transaction ID/code and JSON snapshot; staff/customer-authorized reads, historical reopening, print/preview and Windows PDF printer support. Missing legacy snapshots are not fabricated; customer receipt access is denied when continuity/snapshot requirements fail. |
| Runtime/session | Existing local console authority and dedicated verified pipe; customer has no SQL access. Staff operations retain AppFacade/SQL plus one MainForm-owned review-service connection. Navigation does not log out; real expiry, account change and close revoke the relevant session. |

## Fixes and integration already present

Earlier work fixed the unresponsive Atelier success dialog, repeated password submission, mixed customer/staff pages, page-owned staff service sessions and obsolete mock customer entry buttons. These fixes are **existing source**, not new fixes claimed for this pass. Current uncommitted customer presentation and the real-client launcher were retained, without redesigning either application.

Real entrypoints are connected: CustomerClient → CustomerApi → IdentityDispatcher → P03/P04 services/repositories; staff collection/history/receipts → FinanceService → SqlStudioRepository. There is no disconnected payment implementation requiring another API. New finance UI should use `CollectAsync`, not the older compatibility-only `ReceiveAsync` path.

Legacy `CustomerLoginForm`, `CustomerMainForm`, `CustomerPages` and `Simulation/DesktopDemoSessionStore` still contain mock experiences as inactive reference code. Current staff customer-entry buttons launch the real executable instead. BANK_TRANSFER_DEMO means a manually recorded SQL transaction, **not** verified bank transfer. OTP/OAuth, real payment gateways, webhooks, electronic invoicing and customer album/media delivery are outside this release.

## Evidence and limits

- **This pass:** one normal `dotnet build StudioManager.sln -c Debug --no-restore` — **PASS, 0 warnings, 0 errors**. Log: `artifacts/backend-final-20261118/build-Debug.log`.
- No Backend code was changed, so no new suite or repeated full-project test run was necessary. Existing offline payment evidence: **27/27 PASS** in `artifacts/local-demo-runtime-20261117/Debug-unit.trx` (`DemoPaymentTests` + `DemoCustomerPaymentTests`, run on 17/11). These use doubles/local test pipes, not a live SQL database.
- Previous current-tree UI evidence remains **8 portal + 6 staff-session + 14 focused unit checks PASS**, documented in [CUSTOMER_ATELIER_UI.md](CUSTOMER_ATELIER_UI.md). It is not a fresh run or SQL acceptance from this pass.
- **Owner-confirmed live evidence:** real customer request → staff approval → official booking saved in SQL; the three-component Local Demo Runtime is in use. Historical P03 APPROVE → READ → REVOKE → DENY evidence is recorded separately in `backend/P03_FINAL_RUNTIME_STABILIZATION.md`; it does not establish payment acceptance in the current demo DB.
- Codex did not connect to SQL or independently query any of these live records. The latest uncommitted customer UI/launcher is source/offline verified; its deployment/live acceptance is not assumed.

## Remaining Owner manual verification

Using only the existing disposable demo DB and separately approved operations:

1. On a suitable official demo booking, record CASH deposit, BANK_TRANSFER_DEMO additional collection, then the actual remaining balance. Confirm stable transaction IDs/codes, method/purpose, ledger-derived totals and survival after GUI/host restart. Do not alter the previously approved booking merely for this checklist.
2. Verify saved receipts reopen after later payments and retain their original snapshots/code; check customer isolation, denied unrelated receipts and actual print/PDF output. Independently inspect ledger/audit rows if required for acceptance.
3. Check duplicate action-key handling and any uncertain outcome by **reloading authoritative history first**, never blindly submitting again. Check Admin refund on a separately approved cancelled booking and its refund/net totals.
4. Verify real account-switch/expiry/navigation behavior and the pending GUI update. These checks do not authorize migrations, service changes or data repair. `MIGRATION_REQUIRED` means stop and review the current schema; do not rerun the empty-database bootstrap over existing data.

**Assessment:** ready for Antigravity to polish the real CustomerClient against the implemented contracts; payment/receipt live SQL and printer acceptance remain open. This is a graduation-demo source/handoff assessment, not production certification. No source blocker currently requires additional infrastructure or Backend redesign.

Frontend contract and boundaries: [ANTIGRAVITY_BACKEND_HANDOFF.md](ANTIGRAVITY_BACKEND_HANDOFF.md).

SQL CONNECTED: **NO** · LIVE DATA MODIFIED: **NO** · SERVICE/CONFIG MODIFIED: **NO** · DEPLOYED: **NO** · COMMIT/PUSH: **NO**.
