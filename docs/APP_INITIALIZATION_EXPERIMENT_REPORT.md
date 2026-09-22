# StudioManager — Post-login initialization experiment

## A. Branch

```text
Branch: feature/app-initialization-screen
Implementation commit: 765f9fa056893bff7f57751c67d248f3f18182c1
Report commit: this document's branch-head commit
Status: Experiment — not merged
```

## B. Existing login flow

Before this experiment:

```text
LoginForm
→ AuthService.LoginAsync
→ SqlStudioRepository.FindAccountAsync
→ AppFacade.Session
→ ChangePasswordForm (only when PhaiDoiMatKhau = 1)
→ LoginForm hides
→ MainForm
→ DashboardPage.Load
→ SkeletonLoader
→ SqlStudioRepository.GetDashboardAsync
→ Dashboard data binding
```

Authentication errors are displayed by `LoginForm` and do not create `MainForm`. The active session is stored in `AppFacade.Session`. Role-based menu visibility is applied by `MainForm`. Dashboard loading errors use the existing `Ui.Error` handling.

## C. New initialization flow

```text
Login
→ Authentication
→ Required password change (when applicable)
→ ApplicationInitializationForm
→ Real dashboard data initialization
→ MainForm
→ Dashboard (using preloaded data)
```

The insertion point is after authentication/session creation and any mandatory password change, but before `MainForm`. Failed authentication never displays the initialization screen.

## D. Initialization steps

1. `Xác thực tài khoản` — completed because the screen is created only after `AuthService.LoginAsync` succeeds.
2. `Tải thông tin người dùng` — validates that `AppFacade.Session` exists.
3. `Kiểm tra quyền truy cập` — reflects the existing `VaiTro` value; no permission system was added.
4. `Tải dữ liệu studio` — awaits the real `IStudioRepository.GetDashboardAsync` operation.
5. `Workspace sẵn sàng` — completed only after valid `DashboardData` is available.

Progress is milestone-based (`20 / 40 / 55 / 85 / 100`) and advances only when the corresponding state is reached. No timer-based fake progress or artificial display delay is used.

## E. UI changes

- Centered StudioManager branding area.
- Lightweight, disposable spinner driven by a WinForms timer.
- Milestone progress bar.
- Five status rows supporting pending, active, completed and error states.
- Error headline and readable error detail.
- `Thử lại` action retries only the real initialization operation.
- `Quay lại đăng nhập` clears the session through the existing Login flow.
- Responsive `TableLayoutPanel` layout; no absolute screen coordinates.

## F. Logic changes

- `MainForm` accepts optional preloaded `DashboardData`.
- `DashboardPage` accepts optional initial data and binds it without issuing a duplicate query.
- Dashboard data binding was extracted into a private `Bind` method; calculations and display content are unchanged.
- Authentication, password validation, SQL authentication, database schema, role rules and permissions were not rewritten.

## G. IMAGE / ASSET PLACEHOLDERS

1. Initialization Screen
   - Location: Center branding area
   - Type: StudioManager logo
   - Status: Placeholder (Unicode brand glyph inside a fixed branding row)
   - Expected asset: Transparent StudioManager logo/icon

No random external image or background was added.

## H. Verification

| Check | Result |
|---|---|
| Git branch isolated from source branch | Passed |
| Unexpected working-tree changes before experiment | None |
| `git diff --check` | Passed |
| Authentication failure bypasses initialization | Verified by control flow |
| Authentication success opens initialization | Verified by control flow |
| Mandatory password change remains before initialization | Verified by control flow |
| Initialization uses real Dashboard query | Verified by code inspection |
| Initialization error exposes Retry/Logout | Implemented |
| Retry starts a new cancellable operation | Implemented |
| Dashboard receives preloaded data | Implemented |
| Timer disposed with spinner control | Implemented |
| Cancellation source cancelled on form close | Implemented |
| Windows `.NET 8` build | Pending external Windows environment |
| Visual test at supported DPI/window sizes | Pending external Windows environment |

The Codex runtime does not contain the .NET SDK or Windows Forms renderer, so it cannot truthfully report a Windows build or visual smoke test as passed.

## I. Remaining work

- Build and run on Windows with .NET 8.
- Test successful login, invalid login, mandatory password change, SQL unavailable, Retry and Logout.
- Inspect layout at 100%, 125% and 150% DPI.
- Replace the logo placeholder when the official transparent asset is available.
- If the experiment is accepted, consider adding cancellation telemetry or startup diagnostics without changing the authentication flow.
