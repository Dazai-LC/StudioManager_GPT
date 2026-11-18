# Antigravity Backend handoff — 18/11/2026

Work from the **current working tree** (`c72b4f3` plus uncommitted Atelier integration), not a reset to the pushed baseline. Codex owns Domain/Application/repositories/CustomerApi/IdentityService and functional authority. Antigravity owns final CustomerClient presentation, layout, typography, responsive behavior and photography-oriented visual assets. Preserve approved Staff UI and all pending work. No Backend extension is currently required for the agreed workflow.

## Architecture and entrypoints

- **CustomerClient:** native .NET 8 Windows Forms, references CustomerApi plus shared rendering sources only. Use `CustomerPipeClient.SendAsync(IdentityRequest, CancellationToken)` and `IdentityResponse`; this is a local named-pipe API, **not HTTP/REST/JWT**. Never add SQL credentials, repositories or AppFacade to the customer executable.
- **Authority:** IdentityService → `IdentityDispatcher`/`IdentityDispatcher.Booking` → `CustomerIdentityService` and `BookingRequestService` → `SqlCustomerIdentityRepository`, `SqlBookingRequestRepository` and the existing official-booking writer.
- **Staff:** trusted internal WinForms → existing `AppFacade` services/SQL repositories for business/finance. Booking-request review uses the same verified service API through the MainForm-owned `StaffServiceSession`; keep that connection alive across page navigation.
- **Roles:** customer accounts are `TaiKhoanKhachHang`, not internal `TaiKhoan` ADMIN/STAFF. Customers access only their own profile/requests and authorized official booking/payment/receipt. ADMIN and STAFF may review requests/links and collect payments; only ADMIN may refund, and only under the existing cancellation policy. Hiding controls is not authorization.

Keep customer startup's existing explicit runtime selection. Demo uses `--local-demo --runtime <absolute local-demo-host.json>` and the matching output layout `IdentityService/`, `CustomerClient/`, `Staff/`. `--register` is an implemented CustomerClient-only initial-view option. The staff launcher passes the same demo arguments. Normal launch supports the verified sibling Staff/CustomerClient layout and uses the installed P03 service; missing files/profile must show an error, never a mock or alternate endpoint.

The approved demo target is `MSI\SQLEXPRESS` / `StudioManager_P04_Test_20261116A`; customer receives no connection string. Local integrated SQL encryption/certificate settings belong to the existing reviewed demo configuration, not UI code. Do not touch the installed `StudioManagerIdentity` service, protected configuration or production/business databases.

## Session contract

Create **one CustomerPipeClient per customer portal lifetime**, before constructing dependent pages. Preserve normal SCM PID verification, demo host PID/start/path/SID/session verification and pipe ACLs. Do not replace either verifier with a flag that skips authentication.

`CustomerLogin` authenticates a customer; `StaffLogin` authenticates internal staff separately. Successful customer login returns `Profile`; staff login returns no fabricated customer profile. Registration does not log in automatically. There is no bearer token to save or pass as a customer ID. `Logout` closes the connection; dispose does the same. A new connection must authenticate again. Password change revokes the old credential/session; clear UI and require login.

Existing limits are **8-hour authority sessions**, **110-second client idle lease**, **2-minute server idle timeout**, **30-second request deadline**, **256 KiB bounded messages**. Real expiry/revocation/disconnect must be visible; preserve `IdentityProtocol.RequiresSignIn(response)` handling. No background password storage, automatic re-login, timeout extension or automatic mutation replay.

On logout, login attempt, account/role change or expiry, clear old profile, request rows, previews, selected IDs and payment/receipt views **before awaiting new data**. Use the existing generation checks to ignore delayed callbacks. Page navigation must not dispose the shared client or cancel its in-flight mutation; view disposal only discards that view's result. Close/cancel the actual connection at portal/application shutdown. Preserve `StaffServiceSession`, `MainForm` and `BookingRequestsPage` ownership behavior.

## Actual pipe commands

All requests are `new IdentityRequest(IdentityCommand.X) { ... }`. Responses share `Success`, `Code`, sanitized Vietnamese `Message`; consume payloads only after success. Use the existing enum members without renumbering and retain strict serialization. Never send claimed actor/role or fabricated ownership IDs.

Staff Application calls return `Result<T>` with `Data`; pipe calls return the named payloads on `IdentityResponse` below, not a generic `Data` property.

| Caller / command | Request fields | Response payload / effect |
|---|---|---|
| Anonymous `Register` | `Identifier` email, `Password`, `Confirmation`, `Contact` | Result only; navigate to real login on success |
| Customer `CustomerLogin` | `Identifier`, `Password` | `Profile` |
| Authenticated `Logout` | None | Close session; clear UI |
| Customer `GetMyProfile` | None | `Profile` |
| Customer `UpdateMyProfile` | `Contact` | Contact update result; login email is not changed |
| Customer `ChangePassword` | `Password` current, `NewPassword`, `Confirmation` | Result; sign out on successful rotation |
| Customer `RequestLink` | `Reference`, `Evidence` | Submit controlled historical-profile link request |
| Customer `GetMyLink` / `GetMyLinkedCustomer` | None | `Link` / `LinkedCustomer`; no guessed email/phone matching |
| Staff `StaffLogin` | Internal `Identifier`, `Password` | Staff result; no customer `Profile` |
| Staff `ListLinkReviews` | Optional `BeforeLinkId` | `Reviews`, newest first, up to 25 |
| Staff `ApproveLink` | `LinkId`, positive business `CustomerId`, `Reason` | Authorized link decision |
| Staff `RejectLink` / `RevokeLink` | `LinkId`, `Reason` | Authorized decision/revocation |
| Customer `GetBookingCatalog` | `BookingCatalogQuery(Kind, AfterId?)` | `BookingCatalog` |
| Staff `GetBookingStaffCatalog` | Same query | `BookingCatalog`, including staff allocation choices |
| Customer `PreviewBookingRequest` | `BookingDraft` | `BookingQuote`; server stores this preview on the same connection |
| Customer `SubmitBookingRequest` | `BookingSubmissionKey` only | `CreatedRequestId`; PENDING request, not an official booking |
| Customer `GetMyBookingRequests` | Optional `BookingFilter(State, BeforeId)` | `BookingRequests` |
| Customer `GetMyBookingRequest` | `BookingRequestId` | `BookingDetail` with saved quote/history |
| Customer `CancelBookingRequest` | `BookingRequestId`, `Reason` | Only own PENDING request may be cancelled |
| Staff `GetBookingReviewQueue` / `GetBookingReview` | Filter / `BookingRequestId` | Review list / `BookingDetail` |
| Staff `CheckBookingAvailability` | `BookingRequestId`, `BookingApproval` | Availability result; no reservation |
| Staff `ApproveBookingRequest` | `BookingRequestId`, `BookingApproval`, `Reason` | Transactionally creates one official booking/history/audit |
| Staff `RejectBookingRequest` | `BookingRequestId`, `Reason` | Rejection/history; no booking creation |
| Customer `GetMyRequestBooking` / Staff `GetReviewedRequestBooking` | `BookingRequestId` | `OfficialBooking` after authorized approval |
| Customer `GetMyBookingPayments` | `BookingRequestId`, optional `BeforePaymentId` | `Payments`; read-only, approved owned request and current booking continuity required |
| Customer `GetMyBookingReceipt` | `BookingRequestId`, `PaymentId` | `Receipt`; same ownership plus saved-receipt continuity |

Catalog: Packages/Services/Rooms for customers; Photographers is staff-only. Catalog pages contain up to 25 items and explicit `NextAfterId`; stop on null. Request/review lists contain up to 25 rows descending by ID; use the last row's ID for the older-page cursor. Payment pages contain up to 50 rows and explicit `NextBeforeId`. Keep cursor/data state scoped to the current principal/filter.

## DTOs and validation

Authoritative definitions: [IdentityProtocol.cs](../../src/StudioManager.CustomerApi/IdentityProtocol.cs), [BookingProtocol.cs](../../src/StudioManager.CustomerApi/BookingProtocol.cs), [PaymentProtocol.cs](../../src/StudioManager.CustomerApi/PaymentProtocol.cs). Reuse these types; do not invent parallel contracts or assume additional response fields.

| Type | Fields the UI consumes |
|---|---|
| `ContactDto` / `ProfileDto` | `FullName, Phone, Email, Address` / `LoginEmail, Contact` |
| `BookingCatalogItemDto` | `Id, Code, Name, Description, Price, Unit, DurationMinutes` |
| `BookingDraftDto` | `SubmissionKey, PackageId, StartUtc, PreferredRoomId?, Notes, Addons` (`ServiceId, Quantity`) |
| `BookingQuoteDto` | Saved draft key, package identifiers/name/price/duration, time/room/contact, add-on unit prices/quantities and authoritative `Estimate` |
| `BookingApprovalDto` | `PhotographerId, RoomId, ExpectedEstimate` |
| `BookingRequestRowDto` / `BookingDetailDto` | ID/state, customer/package/time/estimate/reference; detail adds `Quote, Feedback, ReviewedAtUtc, CancelledAtUtc, OfficialBookingId, OfficialBookingCode, ConfirmedTotal, History` |
| `BookingEventDto` | `State, AtUtc, Actor, Reason` |
| `OfficialBookingDto` | `Id, Code, Status, StartUtc, EndUtc, PackageName, PackagePrice, ServicesTotal, Total, Room, Photographer` |
| `BookingPaymentsDto` | `BookingCode, Status, Total, Collected, Refunded, Remaining, Items, NextBeforeId` |
| `PaymentRowDto` | `Id, Code, Purpose, Amount, Method?, PaidAt, CanViewReceipt` |
| `PaymentReceiptDto` | Stable ID/code/booking identity, customer/phone/package, optional package price/discount/booking total/balance snapshot, service lines, `Amount, Method?, Purpose, PaidAt, StaffName, HasSnapshot, Note` |

- Login email: normalized ASCII email, maximum 254 characters. Customer login failures deliberately combine incorrect/locked/unavailable accounts. Profile name required, max 100; phone required, max 20 with ASCII digits, `+ - ( )` and spaces; valid contact email; non-null address max 255. **Booking contact requires phone max 15** for the existing business schema; show `BOOKING_CONTACT_REQUIRED` guidance instead of silently truncating.
- Password: 8–128 characters, upper/lower/digit, matching confirmation; profile password change also needs the current password. No OTP/OAuth/email-verification endpoint exists. `Contact.Email` is editable contact data, not a login-email-change API.
- Draft: nonempty GUID, positive package and optional room IDs, non-null notes max 1000. Start must be UTC offset zero, whole seconds, at least 5 minutes and at most 365 days ahead. Package duration is server-defined, 1–1440 minutes. Display Vietnam time (+07:00), convert once on input; do not send culture-formatted date strings.
- Add-ons: at most 20 distinct service IDs; quantity `> 0`, `<= 1000`, at most 2 decimal places. Package price, unit price and every extended line must fit exact cents and ledger bounds. Preserve `decimal`; no floating-point money or silent rounding.
- Cancellation/approval/rejection reason: trimmed 5–500 characters, customer-visible. Identity-link reference max 100, evidence max 500; link-review reason nonblank max 500, with an explicit selected business customer for approval.

Use one GUID per logical draft, preview the actual server quote, confirm, then submit that same key **on the same authenticated connection**. Changing relevant inputs requires a new preview; login/logout/password rotation clears previews. The server consumes its preview on submit. SQL prevents duplicate `(account, submission key)` writes and rejects changed content. Never automatically resubmit after loss of acknowledgement.

Request transitions are `PENDING → APPROVED | REJECTED | CANCELLED`. Staff must use the saved exact estimate and active allocation choices; later catalog prices do not rewrite saved quotes. Request APPROVED and official-booking lifecycle status are different concepts. Room choice is a preference until approval; preview/availability checks do not hold a slot. No customer command creates an official booking directly.

## Staff collections and saved receipts

These are **existing trusted staff Application APIs**, not customer pipe mutation endpoints. Reuse [FinanceService.Payments.cs](../../src/StudioManager.Application/Services/FinanceService.Payments.cs), [Contracts.cs](../../src/StudioManager.Application/Contracts.cs), [Payments.cs](../../src/StudioManager.Domain/Payments.cs), `PaymentPolicy` and `BookingMoneyPolicy`.

```csharp
var input = new PaymentInput(officialBookingId, purpose, amount, note)
{
    PhuongThucThu = PaymentMethods.Cash, // or BankTransferDemo
    YeuCauThuId = Guid.NewGuid()         // once after explicit human confirmation
};
var posted = await app.Finance.CollectAsync(input, app.Session!, ct);
var history = await app.Finance.PaymentsAsync(officialBookingId, beforePaymentId, ct);
var receipt = await app.Finance.ReceiptAsync(officialBookingId, paymentId, ct);
```

Call with the actual issued staff session; never construct a `UserSession` from display data. Method is `CASH` or `BANK_TRANSFER_DEMO`; purpose is independently `DAT_COC`, `BO_SUNG` or `CON_LAI`. For a final-balance action use the **server-returned Remaining**, not a UI estimate. No deposit-percentage rule or automatic bank verification is implemented. New UI must not use compatibility-only `ReceiveAsync` to omit method/action-key/receipt metadata.

Amount must be positive, exact to 0.01 and no greater than authoritative remaining, with `decimal(18,2)` maximum `9_999_999_999_999_999.99`. Existing `PaymentPolicy.TryReadAmount` accepts plain digits and optional `.`/`,` with 1–2 fractional digits; grouping, exponent and subcent input are refused, never rounded. Preserve the staff parser; customers only format received DTOs.

The SQL writer revalidates actor/booking under serializable locking, stores method/purpose/action key/snapshot in existing `ThanhToan` with audit, and returns the persisted receipt. A repeated identical action key returns that transaction; changed payload/actor under that key fails `POSTING_KEY_REUSED`. Do not generate a replacement key or retry automatically after `OUTCOME_UNKNOWN`. Reload latest summary/history first; `RELOAD_REQUIRED` blocks collection until the existing recovery path completes. `BUSY` is not permission to queue another collection.

Render summary totals as returned: active `Remaining = max(0, Total - Collected)`; cancelled bookings report zero remaining. Refunds are separate and Admin-only through `RefundAsync(RefundInput, issuedSession)`, limited to cancelled bookings and collected minus refunded. Do not change debt policy or treat refunds as a new collection allowance.

Receipts come from `ReceiptAsync`/`GetMyBookingReceipt`, not current catalog calculations. Keep ID/code, transaction amount/method/purpose and original snapshot. `HasSnapshot=false` means optional historical totals/discount/balance were not saved; show absence, not zero or a recomputed substitute. Customer `CanViewReceipt=false` is meaningful and may require contacting staff. Reuse `ReceiptText` and `PersistedReceiptForm` for preview/printing; PDF through Windows printing is sufficient. `PaidAt` is the existing SQL-local `DateTime`; booking/history fields ending `Utc` are UTC. Do not reinterpret all timestamps as UTC.

## Errors, UI integration and boundaries

- Identity: `INVALID_LOGIN`, `REGISTRATION_UNAVAILABLE`, `INVALID_INPUT`, `WEAK_PASSWORD`, `PASSWORD_MISMATCH`, `INVALID_PASSWORD`, plus link conflicts/state changes. Show sanitized `Message`; do not reveal account availability or raw SQL errors.
- Booking: `BOOKING_CONTACT_REQUIRED`, `INVALID_TIME`, `INVALID_QUANTITY`, inactive catalog/allocation codes, `PREVIEW_REQUIRED`, `STALE_QUOTE`, `STALE_REQUEST`, `STATE_CHANGED`, `CONFLICT`, `REASON_REQUIRED`, `SUBMISSION_KEY_REUSED`, `PRICE_PRECISION`, `PRICE_LIMIT`, `NOT_FOUND`. Re-read/re-preview as appropriate; none permits an automatic write retry.
- Payment: `INVALID_AMOUNT`, `INVALID_PRECISION`, `INVALID_METHOD`, `INVALID_TYPE`, `INVALID_NOTE`, `LIMIT`, `CANCELLED`, `HISTORICAL_PRECISION`, `MIGRATION_REQUIRED`, `POSTING_KEY_REUSED`, `OUTCOME_UNKNOWN`, `RELOAD_REQUIRED`, `BUSY`, `SESSION_INVALID`/`FORBIDDEN`. Stop on schema/precision blockers; no UI-triggered migrations/repair.
- Use `IdentityProtocol.RequiresSignIn` for transport/authority failures including expiry, lost connection and uncertain outcomes. Disable duplicate submit while awaiting RPC, keep UI responsive, clear stale state on authority loss, and display empty/error states instead of sample records. Do not log credentials, hashes, connection strings or private payloads.

Reuse current customer `Program.cs` + `CustomerPortalPresentation.cs` handlers, `BookingWorkspaceControl` (shared staff branch must remain functional), `CustomerPaymentsForm`, `PersistedReceiptForm`, `AtelierUi`, Theme, CardPanel/PageHeader/ModernButton/AtelierDialog and the extracted rendering-only input/navigation controls. Preserve the working dialog-result behavior. For runtime update commands/manual checks see [CUSTOMER_ATELIER_UI.md](CUSTOMER_ATELIER_UI.md); they have not been executed by this pass.

Do **not** activate `DesktopDemoSessionStore`, mock personas, `SeedGroundedCatalogRepository` or legacy customer simulation adapters as the real data source. No album/media-delivery/image URL API exists in this release; use approved visual assets as presentation, without inventing owned albums or customer documents. Do not add another ledger, gateway, OTP/OAuth, network service, SQL access, service installation, database reset/migration, automatic endpoint fallback or password-based auto-login. Preserve the current Backend, staff session fix and pending changes; coordinate genuine contract gaps with Codex/Owner instead of rewriting business rules for a layout.

Live database/printer/restart acceptance remains Owner-controlled; [BACKEND_FINAL_STATUS.md](BACKEND_FINAL_STATUS.md) distinguishes confirmed manual evidence from offline/source checks. No SQL, runtime deployment, service modification, commit or push is authorized by this handoff.
