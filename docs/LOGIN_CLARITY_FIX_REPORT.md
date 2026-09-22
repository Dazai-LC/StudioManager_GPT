# Login clarity fix report

Branch: `fix/login-form-clarity`

## Scope completed

- Changed the login account placeholder from `Nhập tên đăng nhập hoặc email` to `Nhập tên đăng nhập` because the `TaiKhoan` schema has no email column and authentication only uses `TenDangNhap`.
- Added visible `Hiện` / `Ẩn` controls for the login password, new password and confirmation password fields.
- Removed the non-functional `Chưa có tài khoản? Liên hệ quản trị viên` copy.
- Removed the non-functional `Đăng nhập bằng Windows` option and its visual divider.
- Retained `Quên mật khẩu?` because the existing administrator Accounts page has a real password-reset operation. Its dialog now explains that workflow instead of implying a self-service reset that does not exist.
- Rebalanced Login table rows after the removals so the footer uses available space and controls do not leave an empty action area.

## Interaction verification

- `Hiện` / `Ẩn`: toggles `UseSystemPasswordChar` for the corresponding input only.
- `Quên mật khẩu?`: gives the user a concrete administrator-reset route; it does not claim to reset the password automatically.
- Removed Windows sign-in: no unsupported external identity provider or database expansion is introduced.
- Existing username/password login and mandatory password-change flow are unchanged.

## UI quality checklist

- [x] No decorative Windows-sign-in button remains.
- [x] No decorative account-registration prompt remains.
- [x] Password reveal controls have separate docking space from their text boxes.
- [x] The selected account identifier matches the actual schema.
- [x] Login uses the existing responsive table layout.
- [ ] Windows visual smoke test at 100%, 125% and 150% DPI — pending local Visual Studio test.

## IMAGE / ASSET PLACEHOLDERS

No new image/asset placeholder was introduced by this fix.

Existing placeholders are tracked in `UI_HOTFIX_REPORT.md` and `APP_INITIALIZATION_EXPERIMENT_REPORT.md`.
