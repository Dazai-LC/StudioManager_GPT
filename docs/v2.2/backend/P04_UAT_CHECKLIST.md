# P04 — Owner-controlled SQL and desktop acceptance checklist

Prepared **15/11/2026, Asia/Saigon**. **NOT EXECUTED.** This is a review/acceptance procedure, not approval to provision, migrate, change configuration, operate a service or perform SQL writes.

Final blocker remediation: **B-01/PF-01 and B-02/PF-02 fixed in source; B-03/PF-03 DESIGN COMPLETE / OWNER DECISION REQUIRED**. [Remediation report](P04_BLOCKER_REMEDIATION_REPORT.md) contains the current offline evidence and exact proposed topology. The [original preflight](P04_SQL_DEPLOYMENT_PREFLIGHT.md) is historical. No live checklist item below has been completed by remediation.

## Gate 1 — Name and approve an isolated target

- [ ] Owner records exact SQL machine/instance/database and run identifier. Database must be a specifically approved disposable P04 target, distinct from production and the existing P02/P03 test databases.
- [ ] Migration target is a reviewed standalone named SQL Server 2022+ instance, compatibility >=160, with database `StudioManager_P04_Test_<run GUID as 32 lowercase hex characters>`. Record exact MachineName, ComputerNamePhysicalNetBIOS, InstanceName, ServerName and separately configured @@SERVERNAME, database ID/creation time/collation. A client alias alone is not identity proof; default/cluster/LocalDB/AG targets are unsupported by this migration.
- [ ] Independently review all 16 baseline tables/164 columns, 27 required trusted FKs, 48 required indexes and complete CHECK/default/column/index/FK inventory. Derive/approve its SHA256 using migration 07's canonical inventory from a trusted reviewed reference baseline, not an unreviewed candidate. If a trustworthy fingerprint is unavailable, STOP; do not substitute the source-file hash or invent a hash.
- [ ] External preflight verifies endpoint/TLS/certificate policy, effective rights, exclusive provisioning and approved topology. Both migration approval bits remain zero and expected values NULL in source; enabling a hashed execution copy requires separate approval after these checks, on the same verified connection with no ambient transaction.
- [ ] Owner reviews the baseline business/P03 schema and migration `07_P04_BookingRequests_REVIEW_ONLY.sql`, which is still disabled in the repository. Review object existence, FK/indexes, exact-cent constraints and no-destructive-rerun behavior before enabling a reviewed copy.
- [ ] Owner explicitly approves provisioning/migration and synthetic data setup using a provisioning identity. No production data, real customer contact or credentials are copied into source/evidence.
- [ ] A distinct restricted runtime identity receives only reviewed object-level permissions from `P04_SECURITY_AND_SQL_REVIEW.md`; no `sysadmin`, `db_owner`, DDL, server-wide writes or cross-database access.
- [ ] Restricted-runtime verification independently confirms exact target/schema/rights. Do not use the developer/provisioning identity for live application acceptance or enable the P02 mutation-test selectors.

Stop if any identity/schema/permission observation is unexpected. Do not repair by broadening privileges or switching to a protected database.

## Gate 2 — Approve the real service/client acceptance environment

- [ ] Review full matching P04 service/client/staff packages and hashes against the final source/evidence. The offline desktop helper is not a deployable app.
- [ ] Owner selects and verifies an available **separate Windows VM/host**, preferably with SQL on that same guest/host. Register the real `StudioManagerIdentity` service there; retain fixed pipe/SCM verification and protected configuration. Guest-local client group/SID, ordinary customers, restricted Service SQL principal and separate trusted staff identity must be verified there; do not copy host MSI SIDs or assume a local service SID authenticates to remote SQL.
- [ ] If no separate OS is available, STOP for explicit Owner approval of an alternative maintenance/rollback plan. Merely renaming a same-host service cannot satisfy the fixed pipe/SCM/configuration contract. No alternative is implemented/authorized by this checklist.
- [ ] Review a maintenance/rollback plan before touching the registered service, installed files or protected configuration. Take a fresh complete backup, including ACLs and protected configuration, without publishing secrets.
- [ ] Explicitly approve any target-configuration change and deployment. The source expects the existing registered `StudioManagerIdentity` service and verified local pipe; it is not an interactive/unregistered-host test application. Never bypass SCM verification or collide with the P03 pipe.
- [ ] Confirm the runtime process uses the reviewed service identity; client Windows identities are ordinary non-administrators in the permitted local client group, without SQL/configuration access.
- [ ] Preserve P03 environment/rollback evidence. Any return to its prior configuration/binaries must be a separately controlled action; never erase request/official/audit business data as a binary rollback step.

These operations were not performed by the recovery. If this gate has not been approved, continue source review only.

## Synthetic fixture record sheet

Record actual generated codes/IDs after approved setup; do not assume seed IDs or earlier P03 UAT request IDs.

| Fixture | Required value / recorded identifier |
|---|---|
| Run / target | Owner-recorded exact instance, disposable database and run ID |
| Customers A and B | Two new P03 accounts with distinct synthetic login emails/contact; phone at most 15 characters; no approved legacy link initially |
| Staff S1 / S2 | Two different active internal account IDs with completed required password changes; authorized roles STAFF/ADMIN |
| Package P | Active package, duration 60 minutes, price **100.00** |
| Add-on D | Active standalone non-rental service, price **123.44** |
| Rooms R1 / R2 | Two active rooms |
| Photographers N1 / N2 | Two active employees with `NHIEP_ANH_GIA` role |
| Business customer K-B | A distinct synthetic `KhachHang` for the later staff reassignment test |
| Times T / T+60 / U | Future Vietnam-local slots at least five minutes ahead; one adjacent slot and one separate slot; record matching UTC values |

No real payment is transferred. Any ledger writes below are synthetic, only after approval on the disposable target. Passwords/configuration strings are not recorded in this sheet.

## Customer request → staff approval → official booking

1. [ ] Run real CustomerClient as ordinary customer Windows user; register/log in as A. Confirm P03 own profile works and there is no automatic legacy profile link from matching contacts.
2. [ ] Browse real active package/service/room records. Confirm inactive records are absent and customers cannot request the staff-only photographer catalog. Room listing means active/preferred, not reserved/free.
3. [ ] Select P + D × **1.25**, future Vietnam time T and preferred R1; enter a synthetic note. Preview must show **254.30**, the current authenticated contact, duration and room preference.
4. [ ] Confirm submit. Record request Q1. Independently verify PENDING + snapshot line + first UTC history event. Verify no new official booking, no new business customer and no P03 ownership grant were created by submission.
5. [ ] Log in as B in the same form without first explicitly logging out A. Confirm A's rows/detail/contact disappear **before** B's authentication completes. B cannot list/read/withdraw Q1 or query its official details by a manipulated ID.
6. [ ] In the staff app request page, explicitly log in through the service as S1. Inspect Q1, its original quote/contact/time and history. Select R1/N1; run availability. It must say advisory/not held, then be rechecked on approval.
7. [ ] Approve with a meaningful 5–500-character public reason, explicitly accepting **254.30**. Independently verify exactly one `LichChup`/add-on snapshot, Q1 APPROVED/reference/resolved customer/reviewer/time, terminal history and matching `NhatKyHeThong` rows. Official state starts `DA_DAT_LICH`; request approval is not payment or completion.
8. [ ] For this unlinked A, verify a distinct business customer was created/reused only by P04 provenance, with no email/phone match and no new P03 ownership grant.
9. [ ] Log in fresh as A. Own request/history and current official summary are visible. In staff desktop, open the resulting existing official detail workflow; do not create a second booking engine/record.
10. [ ] Repeat approval/rejection/withdrawal attempts on processed Q1. They must fail state guards with exactly one official booking/history terminal event.

Independently inspect persisted state with an Owner-approved read-only identity. A success toast alone does not verify the transaction/audit rows.

## Rejection, withdrawal, stale quotes and amount precision

- [ ] Create Q2 at separate time U. Reject without a reason: fail. Reject with a meaningful reason: REJECTED + reviewer/time/history, no official/customer creation; A sees the reason and B cannot see Q2.
- [ ] Create Q3 and withdraw as A while pending: CANCELLED + reason/time/history, no official record. Staff cannot approve it; B cannot withdraw A's request.
- [ ] Change catalog price/contact between preview and submit through an approved synthetic-data action: submission refuses stale quote and requires fresh preview. An already persisted valid quote is not silently repriced at approval.
- [ ] Test exact-cent boundary with a reviewed synthetic standalone service price **123.45**, quantity **1.25**: reject `154.3125` at preview rather than storing `154.31`/`154.32`. Test `0.01 × 0.49/0.50/0.51` too: all reject, never round.
- [ ] Valid `123.44 × 1.25 = 154.30` and `0.10 × 0.10 = 0.01` remain allowed. Zero/negative/excess/third-decimal quantities and duplicate service IDs are rejected.
- [ ] Test invalid/past/too-soon date, inactive room/package/service and inactive/non-photographer assignment. Failure leaves PENDING/no orphan official/customer/audit result.
- [ ] Verify phone lengths 16–20 accepted by P03 contact do not silently truncate into the 15-character booking/business-customer schema; booking instructs profile correction.

## Continuity with existing official booking/finance operations

- [ ] Through real package/service create/edit, preserve 100.00/123.45 including edit of existing cent prices; reject original 123.456/0.001/out-of-range input without UI rounding/clamping or persistence. Test both the UI and the shared backend entry point using approved synthetic setup; verify 123.44 × 1.25 passes and 123.45 × 1.25 fails. Integer duration/stock controls remain integer. No historical price/snapshot is rewritten.

1. [ ] Use the established staff `BookingService`/edit dialog to move Q1's official schedule without changing its business customer. A still sees the updated authorized official schedule; stored request time/quote/history remain historical snapshots.
2. [ ] Reassign the still-editable official booking to K-B through the existing authorized staff path. A's current official-detail command must now return no authorized booking. A keeps their original request/history/reference; B cannot claim Q1 by contact or ID. Staff can still inspect the current booking.
3. [ ] Confirm package snapshot preservation on ordinary time edit; package change still requires explicit reviewed current quote and existing role/reason rules. Complete/cancel terminal guards remain.
4. [ ] Confirm resource assignment/return/cancellation and synchronized usage times on a schedule edit. Rental-linked services must not have been sold as automatically allocated P04 add-ons.
5. [ ] On a separate valid synthetic booking totaling 254.30, apply a 0.11 discount through the established workflow: total 254.19. Post 254.18, verify remainder 0.01, then post 0.01 and verify zero remainder. Attempt 0.02 at remainder 0.01: reject with no ledger/audit write.
6. [ ] New payment/refund/discount amounts with third fractional digits reject before persistence. New/updated add-on extensions must be exact cents. Historical stored sub-cent lines must not be rewritten or silently rounded; unexpected legacy balances require explicit review.
7. [ ] Basic collection/refund receipt/history display retains the exact cent amount. This does not accept the older demo multi-format/PDF or reconciliation workflows.
8. [ ] Staff/admin lifecycle and cancellation rules remain; only an authorized administrator may post a bounded synthetic refund on a cancelled booking. Verify corresponding ledger/audit atomically and no real bank operation.

## Actual SQL concurrency and failure checks

Use two independent sessions with **different staff account IDs**; same-account SQL actor locks can otherwise conceal races. Any repeated attempt must follow explicit reload and a new deliberate decision, never blind replay.

- [ ] S1/S2 approve the same pending request together: exactly one official booking/approval; the other sees processed state or a safe transaction failure. Independently count rows/audit.
- [ ] Staff approval races the customer's withdrawal: one terminal result only; either no official row with CANCELLED or exactly one official row with APPROVED. No orphan business customer/audit/partial approval.
- [ ] Competing requests at the same time sharing R1 or N1: at most one approved allocation. Adjacent half-open intervals may both succeed.
- [ ] P04 approval races a legacy direct create/edit for the same room/photographer: same scheduling invariant holds. Also test independent resources to assess unnecessarily broad blocking.
- [ ] P04 customer resolution races approved P03 linking/revocation; no implicit takeover, wrong customer or auto-grant. Keep the old P03 ownership sequence working.
- [ ] Using an explicitly approved disposable-only failure technique, fail after staged creation but before the final commit/audit. Request, customer, official rows and history/audit must all roll back together. Do not weaken constraints or inject failures in installed production code.
- [ ] Observe timeouts/deadlock victims/safe diagnostics under these races. Reload authoritative state before another action; record actual events rather than infer deadlock freedom from source/fake tests.

## Session, navigation and P03 regression acceptance

- [ ] A→B and customer↔staff switching never displays the previous principal's booking/contact data. A delayed old response cannot restore it.
- [ ] Staff page successful login followed by failed re-login is empty/disabled, with no misleading authenticated state.
- [ ] Navigate repeatedly away from/back to the staff request page, including during an in-flight action. Removed pages release timers/pipe clients; new pages start signed out. Other staff navigation remains usable.
- [ ] Leave the real client idle past the finite lease: explicit login required; no approval is sent by an expired view. Do not extend timeouts to bypass this check.
- [ ] Restart/service loss only within a separately approved maintenance action: old actors do not resume; explicit fresh login and SCM verification remain required.
- [ ] A committed write with a lost response is reported uncertain; fresh login/reload reveals its actual state, without automatic duplicate approval/submission.
- [ ] Unexpected P04 mutation failures also return `OUTCOME_UNKNOWN`, even if a pre-commit failure is suspected; do not infer rollback from that code. After SQL commit acknowledgement failure, reauthenticate/reload before any deliberate new decision. Independently verify whether the write actually committed; raw SQL errors must remain redacted. Known rule refusals retain their specific codes.
- [ ] Reconfirm P03 registration/profile/password change and security-version invalidation, STAFF/ADMIN isolation, and legacy ownership **APPROVE → READ → REVOKE → DENY**. Independently verify audit rows; the earlier P03 deployment record is historical evidence only.

## Sign-off record

Record exact target/run, source/package hashes, approved identities, actual request/booking IDs, UTC decision times, observed results and independent row-count/audit evidence. Mark each unexecuted scenario **NOT EXECUTED**, each failure with sanitized category and whether rollback completed, and each unresolved dependency explicitly.

Acceptance requires Owner sign-off after these real checks. Offline **404 xUnit passes + 9 smoke passes per configuration** are supporting evidence, not this sign-off. This checklist currently has no live PASS markings and authorizes no execution by itself.

The preceding **404 + 9** figures describe the earlier recovery snapshot. Current blocker-remediation evidence is **452 xUnit PASS / 11 live SQL SKIP plus 11 separate smoke PASS per configuration**, with P04/P03 focused subsets and no SQL/runtime acceptance. These newer offline results do not mark any live checkbox complete.
