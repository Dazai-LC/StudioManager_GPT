# Phase 2 — Dữ liệu nền (FR02–FR04)

## Baseline và branch

- Baseline đã nghiệm thu: Phase 1 tại remote commit `5c9724de`.
- Branch Phase 2: `feature/spec-v2.1-master-data`.
- Phạm vi: khách hàng, nhân viên, gói chụp, dịch vụ, phòng chụp và tài nguyên; mục tiêu gate AT07–AT11.

## Audit trước thay đổi

| Yêu cầu | Trước Phase 2 | Gap |
|---|---|---|
| FR02 khách hàng | CRUD, tìm kiếm, xóa có điều kiện | Chưa có lịch sử lịch chụp, xóa không audit transaction |
| FR03 nhân viên | CRUD/lookup trạng thái có sẵn | Chưa có màn lịch được phân công |
| FR04 danh mục | CRUD và CHECK SQL cơ bản | Nhân viên vẫn thấy menu danh mục; ngừng dùng không audit; tài nguyên thiếu selector dịch vụ thuê |
| BR19 / FR10 | Soft deactivate và audit một phần | Không lưu before/after cho update danh mục hoặc thao tác ngừng dùng |

## Thay đổi Phase 2

```text
CrudPage
  -> AdministrationService
  -> IStudioRepository
  -> SqlStudioRepository transaction + NhatKyHeThong
```

- Chỉ Admin thấy các menu Nhân viên, Gói chụp, Dịch vụ, Phòng chụp và Tài nguyên; phân quyền Service vẫn giữ là lớp bảo vệ bắt buộc.
- Thêm dialog lịch sử lịch chụp cho Khách hàng, Nhân viên (được phân công) và Phòng chụp.
- Update danh mục lấy dữ liệu trước khi sửa; xóa khách hàng và ngừng dùng danh mục chạy transaction, tạo audit `XOA_KHACH_HANG` hoặc `NGUNG_SU_DUNG_DANH_MUC`.
- Tài nguyên có combobox dịch vụ thuê liên kết, có lựa chọn rõ ràng `Không liên kết`; form chỉ nạp dịch vụ còn hoạt động khi tạo mới.
- Validation tại Application chặn thiếu trường bắt buộc, email sai, giá âm, thời lượng không dương và số lượng tài nguyên không dương trước persistence.

## Không thay đổi

- Không có migration/schema mới.
- Không có asset hoặc image placeholder mới.
- Không thay đổi dữ liệu booking lịch sử; danh mục ngừng dùng vẫn giữ để tra cứu lịch cũ.

## Acceptance cần chạy trên Windows/SQL Server

1. AT07: thêm khách hàng hợp lệ, kiểm tra mã tự sinh và tìm được theo mã/tên/SĐT.
2. AT08–AT09: xóa khách chưa có lịch được sau xác nhận; xóa khách đã có lịch bị từ chối và lịch sử còn nguyên.
3. AT10: ngừng nhân viên nhiếp ảnh gia, xác nhận không còn trong combobox tạo lịch mới nhưng lịch cũ/lịch sử vẫn xem được.
4. AT11: thử giá gói/dịch vụ âm, thời lượng 0, số lượng tài nguyên 0; tất cả phải không lưu.
5. Kiểm tra `NhatKyHeThong` sau cập nhật/xóa/ngừng dùng; dữ liệu trước/sau và actor phải hiện diện.
6. Kiểm tra menu bằng tài khoản Nhân viên: chỉ còn Khách hàng và các nghiệp vụ được phép; không có các menu danh mục/quản trị.

Không đánh dấu AT07–AT11 Pass trước khi người dùng chạy checklist trên commit Phase 2.
