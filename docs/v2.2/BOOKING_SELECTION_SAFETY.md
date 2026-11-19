# Booking selection and consequential-action safety

Implemented on `feature/v2.2-integration`, starting at `42ce609`, on 2026-11-19.

## Result

The Staff booking list no longer chooses row 0 after loading. A booking action requires an explicit mouse/keyboard selection, retained as a **long Booking ID**, and a selected grid row with that same ID. `CurrentRow` alone cannot authorize an action.

- Initial loads have no selection and all booking-dependent actions are disabled.
- Refreshing the same query restores the deliberately selected Booking ID, even when its row position changes.
- Changing filters clears selection. If a selected ID disappears from a refreshed result, selection stays empty; no replacement row is chosen.
- Binding/focus changes cannot adopt a different customer automatically. Mouse clicks and deliberate keyboard row navigation can choose a new target.
- Loading disables the grid and booking actions. Failed reloads clear stale rows/selection. An older asynchronous search response cannot replace newer results.

The existing Atelier grid, inspector, layout, theme and dialogs remain in use.

## Cancellation and other booking actions

Cancellation keeps the existing required reason prompt. It then uses the existing destructive Atelier confirmation, identifying:

- Customer name.
- Booking code.
- Booking start/end date and time.
- Current status.
- Entered cancellation reason.

The final actions are **Hủy lịch này** and **Giữ nguyên**, using the existing safe destructive-dialog defaults.

Cancellation, refunds, schedule edits and progress changes capture an immutable booking snapshot, session and selection generation. The UI reloads the booking by that captured ID before opening the workflow and again immediately before submitting a confirmed change. A changed/missing booking, changed customer/schedule/status/financial snapshot, changed selection, list reload or changed session aborts the pending action and requires a fresh choice. A common busy guard prevents duplicate or competing booking actions while a dialog/read/submit is pending.

Refunds retain the existing amount limit, monetary precision, role requirement and required reason. Their final destructive confirmation now names the booking/customer/date/status and includes the refund amount and reason. Edits and progress confirmations identify the same target. No handler resolves its mutation target from a row index or substitutes the newly current row.

The collection workspace already captures a fixed Booking ID, including when opened from the booking-detail dialog. Its existing summary and final manual confirmation now also identify the customer, booking date/time and status. It rereads the booking and authoritative balance after confirmation and declines submission if either changed. Existing payment input, method/purpose handling, action key, persisted-receipt handling and unknown-outcome behavior are retained.

Booking-detail support operations already capture the dialog's Booking ID; receipt/history operations use persisted transaction IDs within that booking. They cannot retarget a financial operation to another booking merely because the outer list changes. Approval logic and all Backend implementations are unchanged.

These checks are UI preflight safeguards. They add no database concurrency token or transaction lock; the existing Backend validation/transactions remain responsible for concurrent changes after the final client read.

## Verification

- `dotnet build StudioManager.sln -c Debug --nologo`: **PASS**, SDK 8.0.425, **0 errors**, **12 NU1900 warnings** from the unreachable NuGet vulnerability feed.
- `dotnet test tests/StudioManager.WinForms.Tests/StudioManager.WinForms.Tests.csproj -c Debug --no-restore --nologo`: **29 passed, 0 failed, 0 skipped**.
- `dotnet test tests/StudioManager.Tests/StudioManager.Tests.csproj -c Debug --no-build --no-restore --nologo --filter "FullyQualifiedName~BusinessRulesTests.NhanVien_KhongHuyLichDaChup|FullyQualifiedName~FinanceAndBackupValidationTests.Receive_|FullyQualifiedName~FinanceAndBackupValidationTests.Refund_"`: **4 passed, 0 failed, 0 skipped**.
- `git diff --check`: **PASS**.

The new suite drives actual WinForms controls and reason/money/collection dialogs on STA threads, with synthetic in-memory repository responses. It checks initial/focus behavior, explicit mouse/keyboard selection, bigint IDs, refresh/reordering, changed filters, removed results, delayed/failed/out-of-order loads, action disablement, required reasons, positive/declined confirmations, stale customer/date/status/balance, changed sessions, duplicate actions and fixed payment targets. Synthetic mutation calls only record the requested ID/input and return an offline result; they do not persist anything.

The first run found two assertions expecting an Error notification where the existing Atelier helper emits Warning. Those test expectations were corrected; the final UI suite passes. No production notification styling was changed for the tests.

No application startup bootstrap or Local Demo host was run for verification. No real SQL cancellation, collection, refund, receipt update or other SQL transaction was executed. The accidentally cancelled booking was not restored or edited. Schema, migrations, seed/UAT files, Local Demo configuration, existing artifacts and the production Windows Service were left untouched. No commit, push or deployment was performed.

For an optional Owner review in F5, select a known booking, refresh and filter, then open cancellation/refund confirmation and choose **Giữ nguyên** / **Quay lại**. Live SQL interactive acceptance was not run automatically.

## Files changed

| File | Change |
|---|---|
| `src/StudioManager.WinForms/Pages/BookingsPage.cs` | Explicit stable-ID selection, guarded reloads/actions, identified confirmations and stale-target rejection |
| `src/StudioManager.WinForms/Pages/PaymentWorkspacePage.cs` | Identified fixed booking target and post-confirmation booking/balance checks |
| `StudioManager.sln` | Include the focused Windows UI test project |
| `tests/StudioManager.WinForms.Tests/StudioManager.WinForms.Tests.csproj` | WinForms test target using the existing test package versions |
| `tests/StudioManager.WinForms.Tests/BookingUiFixture.cs` | STA controls, dialog automation and synthetic repository; no live persistence |
| `tests/StudioManager.WinForms.Tests/BookingSelectionTests.cs` | Refresh/filter/focus/selection and disabled-action checks |
| `tests/StudioManager.WinForms.Tests/BookingActionTests.cs` | Cancellation/refund/collection/progress target and confirmation checks |
| `docs/v2.2/BOOKING_SELECTION_SAFETY.md` | Behavior, actual verification, scope and remaining manual review |
