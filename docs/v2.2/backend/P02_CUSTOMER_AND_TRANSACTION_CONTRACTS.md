# P02 — Hợp đồng ownership và transaction cho các phase sau

Ngày: 12/11/2026. Trạng thái: **DESIGN ONLY — KHÔNG PHẢI CUSTOMER IMPLEMENTATION**.

Tài liệu xác định điều kiện bảo mật để review P03–P08. Owner đã chọn topology cùng máy studio với Windows Service tách quyền (ADR DECISION-01, option B); service/transport chưa triển khai trong P02. Không tạo account khách, bảng mới, migration hoặc transport. WinForms contracts hiện tại chỉ là dữ liệu trình bày; giá/role/owner/link state/payment status/path do client gửi không có authority.

## Customer authority và ownership

Boundary đã được duyệt theo ADR phải nhận session credential và tự lấy actor. Không dùng `DemoCustomerId`, `AuthenticatedPrincipal` từ client, record `UserSession`, SĐT/email hay trạng thái dropdown làm quyền đọc dữ liệu.

Account đăng nhập, contact profile, `KhachHang` tại quầy và ownership relationship là bốn khái niệm khác nhau. Registration không được tự link hồ sơ cũ chỉ vì trùng điện thoại/email. Customer chỉ gửi yêu cầu xác minh. Nhân sự có quyền duyệt/từ chối/thu hồi, ghi bằng chứng/lý do/actor/thời điểm. Năm trạng thái frontend được bảo toàn; verified/manual-link chỉ do backend cấp.

| Tài nguyên/command đề xuất | Predicate tại backend | Điều cấm và kiểm thử |
|---|---|---|
| Profile của tôi | Account từ session khớp profile owner | Không nhận target account tùy ý; A không đọc/sửa B |
| Request list/detail/revise/cancel | Request.AccountId khớp session; command thỏa trạng thái/version | Không load tất cả rồi lọc WinForms; trả kết quả không phân biệt ID không tồn tại/trái quyền |
| Lịch mới chuyển từ request | Request owner khớp account và liên kết request–booking còn hợp lệ | Account chưa link legacy vẫn xem lịch mới của chính mình; không mở lịch sử cũ |
| Lịch tại quầy | Ownership được phê duyệt, còn hiệu lực, khớp `KhachHangId` | Trùng liên hệ không đủ; thu hồi quyền phải có hiệu lực ở lần truy cập sau |
| Payment declaration | Reference request/booking được phép truy cập; backend xác định source | Không khai báo cho B; không nhận confirmed amount/status từ customer |
| Receipt/history | Nguồn nghiệp vụ của receipt thuộc quyền đọc hiện tại | Không dùng mã phiếu thu đoán được làm authorization |
| Album/photo stream | Ownership hợp lệ; album đúng lịch/customer và đã published/delivered | Kiểm tra từng download, không chỉ album list; A đoán MediaId B vẫn bị từ chối |
| Notifications | RecipientAccountId khớp actor | Không có endpoint customer lấy toàn bộ notification |
| Staff review/approve/post/upload | Role/permission hiện hành do backend xác minh | Customer giả STAFF bị chặn trước truy vấn/ghi; ghi chú nội bộ không trả cho khách |

Tất cả query projection customer phải loại ghi chú nội bộ, account hashes, candidate customer PII, private full path và ngân hàng chưa được công bố. Tên query/command trong bảng là hợp đồng thiết kế, chưa phải APIs đã triển khai.

Authorization của album/request phải nằm trong query/join hoặc transaction backend, không phải lọc collection ở UI. Đổi customer của một lịch đã có request phải là ownership transfer được kiểm soát, không để thao tác edit thông thường vô tình chuyển quyền.

## Booking transaction contract

Conversion P06 phải dùng một connection/transaction bao gồm:

1. Xác thực actor hiện hành; khóa request; kiểm tra expected state/version và idempotency key.
2. Nếu đã convert cùng request, trả chính lịch cũ; unique request–booking bảo vệ lần gọi đồng thời.
3. Xác minh customer association; không chọn customer bằng so trùng contact.
4. Kiểm tra phòng đang hoạt động, thợ đúng chức vụ/đang làm, gói/dịch vụ/snapshot theo policy được duyệt.
5. Khóa theo cùng thứ tự với tạo/sửa lịch nội bộ; kiểm tra overlap `[start,end)` và resource capacity.
6. Tạo `LichChup`, services, assignments; giữ snapshot giá đã được duyệt.
7. Gắn request–booking, chuyển trạng thái; ghi audit/timeline và notification event trong cùng transaction.
8. Commit; retry sau uncertain outcome phải tra kết quả idempotency, không tạo lịch lần hai.

Không compose bằng các repository methods tự mở các transaction độc lập. Read-only availability không giữ chỗ. `rowversion` ngăn lost update nhưng tự nó không ngăn hai request khác nhau cùng đặt phòng. Tất cả writer phải cùng tuân thủ lock/invariant, kể cả module nội bộ cũ.

Test SQL bắt buộc trước acceptance conversion: double-click, hai reviewer/cùng request, hai request/cùng slot, competition với booking nội bộ, thợ nghỉ giữa preview và commit, vượt capacity, lỗi sau từng bước, audit rollback và mất kết nối sau commit. Mock/SQLite không chứng minh locking SQL Server.

## Financial transaction contract

QR generation chỉ đưa hướng dẫn; không đổi ledger hoặc trạng thái xác nhận. Customer declaration lưu pending và evidence ID do backend cấp. Reviewer đối chiếu thực, backend ghi quyết định và bank transaction reference khi có.

Posting P07 phải khóa submission và source booking; revalidate reviewer, state, confirmed amount và remaining balance; tạo duy nhất một `ThanhToan`, posting link, receipt snapshot và audit trong cùng transaction. Submission ID/idempotency key/posting link có unique constraint; mã phiếu thu mới ngẫu nhiên mỗi retry không phải idempotency.

Theo đề xuất chờ DECISION-02, request chưa có `LichChup` chỉ nhận khai báo. Không làm nullable FK `ThanhToan.LichChupId` hoặc giả tạo lịch chỉ để posting. Nếu thu trước được duyệt, thiết kế advance ledger, allocation/refund và báo cáo riêng.

Receipt đã phát hành giữ snapshot và số phiếu qua mọi định dạng; in lại không tính theo catalog mới. Không sửa/xóa tiền cũ để đảo đối soát: dùng nghiệp vụ điều chỉnh/hoàn có quyền. Một ảnh chứng từ trùng hash không đủ chứng minh một bank transaction trùng, và ảnh khác hash không đủ chứng minh tiền mới.

Test bắt buộc: cùng submission hai reviewer, retry sau commit, hai submission cùng bank reference, vượt công nợ đồng thời, rollback ledger/receipt/audit, scan QR không posting, sai nội dung/số tiền, refund đúng quyền và receipt bất biến sau đổi catalog.

## Media contract

Storage key là định danh nội bộ, không phải đường dẫn client. Upload nhận bytes/stream có giới hạn, validate signature/decoder/dimensions, sinh tên, staging rồi ready; metadata và file có state recovery vì filesystem không nằm trong SQL transaction. Không lộ private full path cho UI hoặc cache công khai. Kiểm tra root traversal/reparse points; quyền OS của customer không được đọc private root nếu topology cần cô lập.

Catalog concept/fallback không phải ảnh riêng khách. Album chỉ published/delivered và đúng owner. SQL backup cùng manifest/hash media có generation thống nhất; `.bak` không chứa kho file ngoài SQL. Test crash staging, file thiếu/hash sai sau restore và truy cập chéo account.

## Diagnostics và phạm vi kiểm chứng P02

`SafeDiagnostics` chỉ ghi JSON với event/code/correlation/time/stage/type/provider integer. Không exception message/stack/Data/query/path. Stages là literal kỹ thuật do developer sở hữu, không interpolate contact/secret/path. UI hiển thị thông báo Việt an toàn và mã tra cứu; correlation cùng failure giữ nguyên qua repository/UI.

P02 chưa chứng minh ownership production vì chưa triển khai P03 hoặc service boundary của topology đã được chọn. Tất cả acceptance tests ownership phải chạy tại authority boundary thực, không chỉ chạy router hoặc đọc source string.
