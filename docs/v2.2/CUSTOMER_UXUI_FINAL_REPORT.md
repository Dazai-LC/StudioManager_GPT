# StudioManager v2.2 — CustomerClient Atelier UX/UI Final Report

- **Workspace:** `G:\AnhStudio_Modern_v2\StudioManager-v2.2-CustomerUI`
- **Branch:** `feature/customer-atelier-ui`
- **Baseline Commit:** `4c131d3`
- **Target Project:** `src/StudioManager.CustomerClient/`

---

## 1. Critical WinForms UI Rendering, Performance & Visual Polish Fixes

### A. Severe Black Rendering & Transparency Artifacts Resolved
- **Root Cause 1 (`Color.Transparent` in WinForms AutoScroll):** Standard WinForms controls (`Label`, `FlowLayoutPanel`, `Card.Body`, `CustomerUi.Row`) configured with `BackColor = Color.Transparent` nested inside scrollable containers (`PageFlow : FlowLayoutPanel` with `AutoScroll = true`) fail WinForms GDI+ background erase invalidation. During scrolling, window resizing, or redraw passes, Windows Forms does not repaint intermediate transparent layers, painting raw uninitialized black rectangles (`0x000000`) over text and controls.
- **Root Cause 2 (Pitch-Black `Espresso900` Banner):** `Card(true)` set `BackColor = Theme.Primitives.Espresso900` (#161412), creating a solid 1200×235px pitch-black block across the top of "Trang chủ" and "Hồ sơ cá nhân" that visually appeared like a broken rendering defect.
- **Fixes Applied:**
  1. Replaced `Color.Transparent` with `Color.Empty` across `CustomerUi.Text()`, `CustomerUi.Row()`, `Card.Body`, `Chip`, and `PaymentUi`. When `BackColor = Color.Empty`, child controls inherit the parent's solid background directly without invoking the buggy `SupportsTransparentBackColor` GDI+ paint mechanism.
  2. Redesigned dark cards (`Card(true)`): Configured with `BackColor = Theme.Primitives.Espresso800` (#24211E), `BorderColor = Theme.Primitives.Stone500`, and a brand `AccentStripeColor = Theme.Primitives.Terracotta500` (3px left accent stripe). The hero banner is now an intentional, framed boutique editorial card with high contrast (WCAG AAA) text (`Terracotta200`, `Alabaster50`, `Alabaster200`).
  3. Added composite double buffering (`WS_EX_COMPOSITED = 0x02000000`) to `PageFlow` and `CustomerWorkspace` `CreateParams`. Windows composites child window paint in bottom-to-top order, eradicating scroll tearing and clipping artifacts.
  4. Added explicit background clearing in `CoverPanel.OnPaint` and `StepBar.OnPaint` (`e.Graphics.Clear(Parent?.BackColor)`) prior to clipped drawing, preventing dirty pixel buffer remnants.

---

### B. View Overlap & Content Bleeding on "Trang chủ" Eliminated
- **Root Cause 1 (Multiple Overlapping `DockStyle.Fill` Containers):** All 5 `PageFlow` view instances were added simultaneously to `CustomerWorkspace.Controls` with `Dock = DockStyle.Fill`. WinForms z-order management and `AutoScroll` window regions caused controls from inactive hidden pages to clip and bleed through the active page.
- **Root Cause 2 (Un-guarded Asynchronous Navigation):** When navigating from "Packages" to "Home", the asynchronous catalog query (`EnsureCatalog`) was still awaiting IPC pipe responses. Upon completing after the active tab switched to Home, it unconditionally called `Render(Packages)`, painting room selection and booking controls over the visible Home page.
- **Fixes Applied:**
  1. **Single Active View Host:** In `CustomerWorkspace.Show(CustomerPage page)`, only the active page's `PageFlow` is attached to `CustomerWorkspace.Controls`. Inactive flows are detached (`Controls.Remove`), completely eliminating dock collisions, background paint events, and scroll clipping punch-through.
  2. **Navigation Generation Guard (`_navigationTicket`):** Introduced an atomic ticket counter incremented on every `NavigateAsync`, `ResetSession`, and `Dispose`. In `LoadAsync`, if `ticket != _navigationTicket` upon resumption, the obsolete task immediately aborts without calling `Render`.
  3. **Strict Thread Safety (`InvokeRequired`):** Added thread-marshaling guards (`InvokeRequired` -> `BeginInvoke`) to `CustomerWorkspace.Show`, `CustomerWorkspace.Render`, `IdentityForm.Show`, `IdentityForm.SignedOut`, `IdentityForm.SetPages`, and `IdentityForm.Execute`, preventing cross-thread WinForms exceptions and GDI corruption.

---

### C. Navigation Lag & Layout Thrashing Resolved
- **Root Cause 1 (Redundant Forced Catalog Loads):** Visiting "Gói chụp & dịch vụ" previously called `EnsureCatalog(true)` on every click, performing sequential multi-page pipe round-trips for Packages, Services, and Rooms before rendering.
- **Root Cause 2 (Recursive Re-layout in `FitContent`):** `PageFlow.OnLayout` invoked `card.FitContent()`, which mutated `Card.Height`, triggering cascading re-layout cycles on the UI thread.
- **Fixes Applied:**
  1. **Session Catalog Caching:** `EnsureCatalog(false)` checks `_catalogLoaded` and caches catalog items in memory. Tab switching to "Packages" or "Booking" is now instantaneous (0ms).
  2. **Width & Child-Count Caching:** Added `_lastFitWidth` and `_lastFitChildCount` guards to `Card.FitContent()`, skipping redundant calculations unless container width or children actually changed.
  3. **Immediate Navigation Feedback:** Updated `CustomerPortalPresentation.OpenBookingSection` to update header titles and sidebar highlights immediately before awaiting navigation tasks.

---

### D. Visual Balance & Proportions Refined
- **Login & Registration Screens:** Replaced the stretched 800px full-width card with an auto-centered `CardPanel` (460px width for login, 500px for registration) with 24px padding and rounded corners. Centered vertically and horizontally within its pane beside `AuthHero`.
- **Authenticated Customer Workspace:** Implemented optimal editorial max width (1080px) and dynamic centering in `PageFlow.OnLayout`. Wide monitors (1320px–1920px) now render balanced, magazine-style cards with consistent margins rather than stretched horizontal bars.

---

## 2. Preferred Room Selection Restored

Preferred studio room selection is fully functional across catalog discovery and the booking wizard using existing `CustomerApi` contracts:
- **Catalog Loading (`CustomerWorkspace.EnsureCatalog`):** Queries `BookingCatalogType.Rooms` alongside `Packages` and `Services`.
- **Catalog Discovery (`CustomerWorkspace.RenderPackages`):** Displays active rooms with clear selection/unselection toggles.
- **Booking Wizard Step 3 (`CustomerWorkspace.StepSchedule`):** `ComboBox` with default "Để nhân viên studio chọn khi duyệt (mặc định)", active rooms, and "Bỏ phòng ưu tiên" action. Resets quote cache to ensure fresh server quote calculation.
- **Review & Detail Displays (`StepReview`, `SubmitAsync`, `RenderResult`, `RenderDetail`):** Shows authoritative `PreferredRoomName` returned by Backend.

---

## 3. Customer Profile Page Redesign Completed

`IdentityForm.BuildProfile()` in [Program.cs](file:///g:/AnhStudio_Modern_v2/StudioManager-v2.2-CustomerUI/src/StudioManager.CustomerClient/Program.cs) visually matches the Atelier Lumière design system:
1. **Identity Overview Card (`identityCard`):** Framed Atelier dark card with terracotta accent stripe, displaying customer name and read-only login email.
2. **Contact Information Card (`contactCard`):** Vector-icon input fields (`_name`, `_phone`, `_email`, `_address`) with validation and instant session refresh.
3. **Studio Record Link Card (`linkCard`):** Live link status summary, reference/evidence fields, and link query actions.
4. **Account Security Card (`securityCard`):** Password rotation fields with reveal toggles wired to `IdentityCommand.ChangePassword`.

---

## 4. Asset Packaging & Verification

- **Output Assets (`src\StudioManager.CustomerClient\bin\Debug\net8.0-windows\Assets\MediaDefaults\packages\`):**
  - `pkg_GC000001_cover_ai.jpg` (709,283 bytes)
  - `pkg_GC000002_cover_ai.jpg` (821,568 bytes)
  - `pkg_GC000003_cover_ai.jpg` (762,547 bytes)
  - `pkg_GC000004_cover_ai.jpg` (854,639 bytes)
  - `pkg_GC000005_cover_ai.jpg` (977,327 bytes)
- **Path Resolution:** Relative resolution via `AppContext.BaseDirectory`. Zero hardcoded developer paths. Graceful vector `Aperture` fallback when unmapped.

---

## 5. UTF-8 & Source Integrity Verification

All 8 C# source files in `src/StudioManager.CustomerClient/` decoded with strict UTF-8 (`new UTF8Encoding(false, true)`):
- `AtelierUi.cs`, `BookingWorkspaceControl.cs`, `CustomerComponents.cs`, `CustomerPortalPresentation.cs`, `CustomerWorkspace.cs`, `PaymentUi.cs`, `PersistedReceiptForm.cs`, `Program.cs`: **0 `U+FFFD` replacement characters, 0 mojibake sequences**.

---

## 6. Build & Test Results

1. **Solution Build:**
   - Command: `dotnet build StudioManager.sln -c Debug --no-restore -v q`
   - Result: **PASS — 0 Warning(s), 0 Error(s)**.
2. **CustomerClient Project Build:**
   - Command: `dotnet build src/StudioManager.CustomerClient/StudioManager.CustomerClient.csproj -c Debug --no-restore -v q`
   - Result: **PASS — 0 Warning(s), 0 Error(s)**.
3. **Focused Offline Unit & Contract Tests:**
   - Command: `dotnet test tests\StudioManager.Tests\StudioManager.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~FrontendV22Tests|FullyQualifiedName~LocalDemoRuntimeTests|FullyQualifiedName~DemoCustomerPaymentTests|FullyQualifiedName~CustomerIdentityTests|FullyQualifiedName~BookingRequestTests"`
   - Result: **PASS — 121/121 passed, 0 failed, 0 skipped**.
4. **Automated Visual UI Inspection (`scratch/UiInspector`):**
   - Captured real form renderings: `01_login.png`, `02_home_async.png`, `03_Home.png`, `03_Packages.png`, `03_Booking.png`, `03_Requests.png`, `03_Payments.png`, `04_profile.png`.
   - Verified: Zero black boxes, zero overlapping controls, centered 1080px cards, centered login form.
5. **Architectural Boundaries Preserved (`git status`):**
   - Modified: `CustomerComponents.cs`, `CustomerWorkspace.cs`, `CustomerPortalPresentation.cs`, `Program.cs`, `PaymentUi.cs`, `StudioManager.CustomerClient.csproj`.
   - Strictly untouched: `StudioManager.WinForms` (Staff app), `CustomerApi`, `Application`, `Domain`, `Infrastructure`, `IdentityService`, and SQL scripts.

---

## 7. Customer Authentication UI Polish (Atelier Lumière Cinematic Landing)

### A. Full-Window Photographic Background (`AuthBackdrop`)
- **Cover Cropping & High-Quality Bicubic Scaling:** Replaced the narrow split-hero panel with a full-window photographic backdrop control (`AuthBackdrop`) anchored directly in `_login`. Uses the approved Atelier package artwork (`pkg_GC000001_cover_ai.jpg`) dynamically scaled via high-quality bicubic interpolation with center cropping so that no image distortion or stretching occurs regardless of window dimensions.
- **Cinematic Scrim & Vignette:** Painted with a 3-layer composited dark scrim and dual-axis gradients (horizontal and vertical linear gradient brushes in warm espresso tones: `#161412`), delivering depth, warmth, and WCAG AAA legibility.
- **Left Editorial Typography:** When viewport width >= 880px, renders boutique typography directly on the photographic surface:
  - Eyebrow: `ATELIER LUMIÈRE` in `Terracotta200`.
  - Headline: `Khoảnh khắc của bạn, được kể bằng ánh sáng.` in 22pt bold `Alabaster50`.
  - Subtitle: `Không gian khách hàng · Chọn gói chụp, gửi yêu cầu đặt lịch và theo dõi hành trình cùng studio.` in 10.5pt `Alabaster200`.

### B. Floating Authentication Cards
- **Refined Letterpress Card Styling:** Both Sign-In (`_signInCard`, 460px width) and Registration (`_registrationCard`, 490px width) cards are built as floating `CardPanel` controls over the photograph.
  - Surface: `Theme.Primitives.Alabaster50` (#FAF8F5).
  - Border: `Theme.Primitives.Stone300` (#D6D3CD) with 14px rounded corners.
  - Accent: 3px left stripe in `Theme.Primitives.Terracotta500` (#C04E26).
  - Drop Shadow: Multi-layer soft ambient drop shadow rendered by `AuthBackdrop` behind the active card.
- **Card Positioning & Responsiveness:**
  - Viewports >= 1060px: Thoughtfully positioned right-of-center (50%–55% horizontal offset), allowing the studio photography and editorial brand copy on the left to breathe while keeping inputs within natural scanning reach.
  - Viewports < 1060px: Dynamically centered horizontally.
  - `AutoScroll`: Enabled on `AuthBackdrop` with `AutoScrollMinSize` tracking card bottom so fields are never clipped on compact screens or low-resolution displays.

### C. Dynamic Shell Layout Management (`UpdateShellLayout`)
- **Full Viewport Backdrop during Auth:** During authentication (`isAuth == true`), `UpdateShellLayout(true)` collapses `_shellRoot` Row 0 (Header) and Row 2 (Status bar) to height 0. This completely eliminates awkward blank beige bars above or below the login/registration screen, allowing the photograph to extend seamlessly from the title bar to the bottom edge.
- **Restoration of Authenticated Shell:** When logging in or switching to authenticated sections (Home, Packages, Booking, Requests, Payments, Profile, or Staff Links), `UpdateShellLayout(false)` restores Row 0 to 82px (showing the `PageHeader`) and Row 2 to 54px (showing the bottom status bar), preserving the exact approved authenticated workspace shell.
- **Inline Notice Banner (`_authNotice`):** Status updates, session expiration, and error notices are mirrored cleanly inside the floating card via `_authNotice`, with appropriate status colors (Danger red or Primary) without drawing an external beige box across the bottom of the screen.

### D. Full Functionality Preserved
- All existing login, customer registration, field validation, password show/hide toggles, Enter-key submission handlers, and staff link review navigation remain intact.
- Staff authentication mode correctly adapts the floating card copy to "Tên đăng nhập nội bộ" and displays staff purpose text while hiding the customer registration option.

---

## 8. Staff Identity-Link Approval UI Rebuild (`StaffLinkReviewWorkspace`)

### A. Rebuilt Master-Detail Architecture
The operational **"Duyệt liên kết hồ sơ"** screen (`CustomerClient → Nhân sự → Duyệt liên kết hồ sơ`) has been completely rebuilt from the ground up, resolving every defect reported by the Owner:
- **Master-Detail Layout (58% / 42%):** Replaced the narrow top DataGridView and sprawling blank canvas with an ergonomic split workspace:
  - **Left Section (58%):** Master request list card (`_listCard`) hosting the styled DataGridView, custom state pill painting, keyset pagination controls, and an intelligent empty state overlay.
  - **Right Section (42%):** Scrollable review and decision card (`_detailScroll`), displaying full request details, studio verification reminders, numeric `KhachHangId` input, reason documentation, and hierarchical decision action buttons.
- **Header Redundancy Elimination:** The main window shell already provides the title "Duyệt liên kết hồ sơ" in Row 0 (`PageHeader`). Replaced the repeated inner header with a slim 42px operational toolbar containing section guidance, a live count chip (`_countChip`, e.g. "3 yêu cầu"), and a compact reload button.

### B. Request List & Empty State Presentation
- **Balanced DataGridView Columns:** Configured with fixed proportional widths for metadata columns (`Mã YC` 60px, `Trạng thái` 96px, `Khách hàng` 120px, `Email đăng nhập` 135px, `Mã t.chiếu` 95px, `Mã KH` 68px) and `Fill` for `Ghi chú đối chiếu` (minimum 110px). All 7 columns fit comfortably on standard (1320×820) and compact (1040×660) viewports without awkward horizontal scrolling.
- **Custom Status Pill Rendering:** Implemented dedicated `ReviewStateStyle` in `CellPainting` using rounded pill shapes (8px radius) with distinct color semantics:
  - `PENDING` → "Chờ duyệt" (Terracotta warning chip).
  - `APPROVED` → "Đã duyệt" (Success green chip).
  - `REJECTED` → "Từ chối" (Danger red chip).
  - `REVOKED` → "Thu hồi" (Muted subtle chip).
- **Dedicated Empty State (`_emptyPanel`):** When no link requests exist (or on session reset), the grid seamlessly hides in favor of a centered empty-state panel with vector `UserCheck` icon, bold typography ("Không có yêu cầu cần duyệt"), clear explanatory copy, and a primary reload action.

### C. Review Details, Studio Verification & Strict Business Rules
- **Customer Identity Context:** Displays customer account name, login email, internal account ID, customer-supplied reference code, evidence text inside a framed callout box, and existing studio customer status (`✓ Đã gán KhachHangId: {id}` or "Chưa gán KhachHangId studio").
- **Verification Policy Reminder:** Prominently displays the warning callout: *"⚠️ Nguyên tắc đối chiếu: Nhân viên phải tra cứu hồ sơ trực tiếp tại phần mềm quản trị studio nội bộ. Tuyệt đối không tự động duyệt dựa trên trùng khớp email hoặc số điện thoại."*
- **Vector Input Fields & Strict Validation:**
  - `MÃ KHÁCH HÀNG STUDIO (KHACHHANGID) *`: Vector input with database icon; strictly validated as a positive integer (`long > 0`) when approving.
  - `LÝ DO / GHI CHÚ ĐỐI CHIẾU *`: Vector input with info icon; mandatory for all decisions (up to 500 characters).
- **Hierarchical Decision Actions & State Matrix:**
  - **Duyệt liên kết:** Primary terracotta button (`#C04E26`, `IconKind.Check`); enabled only when `State == "PENDING"`.
  - **Từ chối:** Warning outline button (`Theme.Danger`, `IconKind.Close`); enabled only when `State == "PENDING"`.
  - **Thu hồi liên kết:** Danger outline button (`Theme.Danger`, `IconKind.Trash`); enabled only when `State == "APPROVED"`.
- **Confirmation Prompts:** Every action invokes `AtelierUi.Confirm` before executing the backend IPC call, requiring explicit staff confirmation with full customer summary and reason.

### D. Automated Visual Inspection & Screenshots
- `06_staff_reviews_populated.png`: Shows the populated master-detail workspace with row #201 selected, 7 clean grid columns, custom status chips, and primary approval action.
- `07_staff_reviews_approved.png`: Shows approved request #203 selected with pre-filled KhachHangId 1042, disabled approve/reject buttons, and enabled red revoke action.
- `08_staff_reviews_empty.png`: Shows the dedicated empty-state presentation when no requests are pending.

---

## 9. CustomerClient Modified Files Summary

| File | Purpose of Modifications |
|---|---|
| `StaffLinkReviewWorkspace.cs` | **(New)** Full code-first master-detail staff review workspace with DataGridView, custom state pill painting, keyset pagination, dedicated empty state, customer verification panel, policy reminder, strict validation, confirmation dialogs, and state-machine action buttons. |
| `CustomerComponents.cs` | Added mutable `UpdateStyle` to `Chip` control. Implemented `AuthBackdrop` with proportional cover bicubic scaling and scrim gradients. Removed `Color.Transparent` on `Text`, `Row`, `Card.Body`, `Chip`. Redesigned `Card(true)` to `Espresso800` with `Terracotta500` accent stripe. |
| `CustomerPortalPresentation.cs` | Mounted `_staffReviews` in `_reviews` tab. Implemented `UpdateShellLayout(bool isAuth)`. Rebuilt `BuildLogin()` with floating cards inside `_authBackdrop`. |
| `CustomerWorkspace.cs` | Added `WS_EX_COMPOSITED`. Implemented single active view host in `Show(page)`. Added atomic `_navigationTicket` guard. Cached catalog. Restored preferred-room selection in booking wizard. |
| `Program.cs` | Wired `_staffReviews` into `IdentityForm`, delegating `LoadReviews()` and `SignedOut()` reset while preserving backwards-compatible fields (`_customerId`, `_reason`, `_grid`). Added `InvokeRequired` guards. |
| `PaymentUi.cs` | Removed `Color.Transparent` on root `TableLayoutPanel`, `Summary` label, and `Actions` panel. |
| `StudioManager.CustomerClient.csproj` | Included `Assets\**\*` with `PreserveNewest` to output directory. |

---

## 10. Safe Instructions to Refresh & Launch Local Demo (Owner-Only — Not Executed)

Close both GUI applications (`StudioManager.WinForms.exe` and `StudioManager.CustomerClient.exe`) while leaving the existing Local Demo console host running. Run the following PowerShell commands after review to back up the existing binaries, copy the executables/configs per GUI, **and copy the new `Assets` directory for `CustomerClient`** without touching `local-demo-host.json`, `local-demo.profile`, `appsettings.json`, `service.json`, or SQL Server:

```powershell
$ErrorActionPreference = 'Stop'
$worktreePath = 'G:\AnhStudio_Modern_v2\StudioManager-v2.2-CustomerUI'
Set-Location -LiteralPath $worktreePath

dotnet build StudioManager.sln -c Debug --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$demoRoot = Read-Host 'Paste the existing local demo root containing Staff, CustomerClient and local-demo-host.json'
if (-not (Test-Path -LiteralPath "$demoRoot\local-demo-host.json") -or
    (Get-Content -LiteralPath "$demoRoot\local-demo.profile" -Raw).Trim() -ne 'StudioManager.LocalDemo.20261116A.v1') {
    throw 'The matching existing local demo root is required.'
}

$backup = Join-Path $worktreePath ('artifacts\customer-atelier-before-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup | Out-Null

$guiProjects = [ordered]@{ CustomerClient = 'StudioManager.CustomerClient'; Staff = 'StudioManager.WinForms' }
foreach ($entry in $guiProjects.GetEnumerator()) {
    $source = Join-Path $worktreePath "src\$($entry.Value)\bin\Debug\net8.0-windows"
    $target = Join-Path $demoRoot $entry.Key
    $saved = Join-Path $backup $entry.Key
    if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw "Existing GUI output folder missing: $target" }
    New-Item -ItemType Directory -Path $saved | Out-Null
    foreach ($suffix in 'exe', 'dll', 'deps.json', 'runtimeconfig.json') {
        $name = "$($entry.Value).$suffix"
        Copy-Item -LiteralPath "$target\$name" -Destination $saved
        Copy-Item -LiteralPath "$source\$name" -Destination $target -Force
    }
}

# Copy the approved package cover assets to the CustomerClient demo folder:
$customerAssetsSource = Join-Path $worktreePath 'src\StudioManager.CustomerClient\bin\Debug\net8.0-windows\Assets'
$customerAssetsTarget = Join-Path $demoRoot 'CustomerClient\Assets'
if (Test-Path -LiteralPath $customerAssetsTarget) {
    Copy-Item -Path $customerAssetsTarget -Destination (Join-Path $backup 'CustomerClient\Assets') -Recurse -Force
}
Copy-Item -Path $customerAssetsSource -Destination (Join-Path $demoRoot 'CustomerClient') -Recurse -Force

# Launch CustomerClient directly against the existing demo lease:
& "$demoRoot\CustomerClient\StudioManager.CustomerClient.exe" --local-demo --runtime "$demoRoot\local-demo-host.json"
```
