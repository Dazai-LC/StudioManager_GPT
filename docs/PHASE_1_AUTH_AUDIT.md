# Phase 1 — Audit tài khoản và bảo mật

## Baseline và phạm vi

- Branch nguồn đã kiểm tra: `feature/spec-v2.1-compliance` tại `6e84fc0`.
- Branch thực hiện Phase 1: `feature/spec-v2.1-accounts-security`.
- Phạm vi: FR01 và các gate AT01–AT06. Không thay đổi schema SQL, không rewrite authentication.

## Luồng trước khi thay đổi

```text
LoginForm
  -> AuthService.LoginAsync
  -> IStudioRepository.FindAccountAsync + IPasswordHasher.Verify
  -> UpdateLastLoginAsync
  -> UserSession trong AppFacade
  -> ChangePasswordForm (nếu PhaiDoiMatKhau = 1)
  -> ApplicationInitializationForm
  -> MainForm/Dashboard
```

- `Pbkdf2PasswordHasher` dùng PBKDF2-SHA256, salt ngẫu nhiên 16 byte, 210,000 iterations và so sánh hash constant-time.
- `TaiKhoan.TenDangNhap` là `UNIQUE`; không có email trong schema và LoginForm chỉ gợi ý tên đăng nhập.
- `PhaiDoiMatKhau` được bật khi tạo/reset và chỉ được tắt khi đổi mật khẩu thành công.
- UI đã có chặn tự khóa, nhưng trước Phase 1 repository chưa ghi audit lock/unlock trong transaction; service chưa revalidate quyền admin từ database.

## Điểm chèn và thay đổi Phase 1

```text
AccountsPage
  -> AdministrationService.ToggleAccountLockAsync
  -> revalidate actor admin/active bằng GetAccountByIdAsync
  -> SqlStudioRepository.ToggleAccountLockAsync
  -> transaction + NhatKyHeThong
```

- Login thành công ghi `DANG_NHAP` vào `NhatKyHeThong` cùng transaction cập nhật `LanDangNhapCuoi`.
- Khóa/mở khóa tài khoản có guard ở cả Service và Repository, chặn tự khóa, khóa hàng mục tiêu (`UPDLOCK, HOLDLOCK`) và ghi `KHOA_TAI_KHOAN`/`MO_KHOA_TAI_KHOAN` kèm before/after.
- Create/reset account revalidate tài khoản thao tác vẫn active và còn quyền `QuanTriVien`; reset chính tài khoản bị từ chối để dùng luồng đổi mật khẩu riêng.
- Grid tài khoản chỉ trả dữ liệu qua `AdministrationService` cho session Admin; nút khóa/mở khóa đổi nhãn/màu theo trạng thái thực tế và luôn có confirm dialog.

## Không thuộc phạm vi

- Không có đăng nhập email/SSO/Windows sign-in; đây là quyết định giữ database và nghiệp vụ tối giản.
- Không có asset mới hoặc image placeholder trong Phase 1.
- Không thay đổi SQL schema hay dữ liệu seed.

## Evidence cần nghiệm thu trên Windows

1. Build và test ở commit Phase 1.
2. Login sai, login tài khoản bị khóa và login thành công.
3. Tạo/reset tài khoản, xác nhận lần đăng nhập tiếp theo bắt buộc đổi mật khẩu.
4. Khóa/mở khóa một tài khoản khác; xác nhận tự khóa và self-reset bị chặn.
5. Kiểm tra `NhatKyHeThong` có `DANG_NHAP`, `KHOA_TAI_KHOAN`/`MO_KHOA_TAI_KHOAN`, `TAO_TAI_KHOAN`, `RESET_MAT_KHAU`, `DOI_MAT_KHAU`.

`Implemented` chỉ được đánh dấu Pass cho AT01–AT06 sau khi các evidence này được chạy trên Windows/SQL Server.

## Kết quả nghiệm thu

- Ngày xác nhận: 23/09/2026.
- Kết quả: **Pass** theo xác nhận nghiệm thu của người dùng trên Windows/SQL Server.
- Commit nghiệm thu trên remote: `3b74009277f7e529dfbff725e2ecc6c41d12653c`.
