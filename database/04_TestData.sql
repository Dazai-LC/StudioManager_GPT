USE StudioManager;
GO
-- Kiểm tra điều kiện giáp ranh: lịch 11:00 bắt đầu ngay sau LCDEMO-0001 kết thúc phải hợp lệ.
SELECT CASE WHEN EXISTS(SELECT 1 FROM LichChup WHERE TrangThai<>'DA_HUY' AND BatDau<'2099-01-01T12:00:00' AND KetThuc>'2099-01-01T11:00:00') THEN N'Cần kiểm tra dữ liệu' ELSE N'Khung giờ trống' END AS KiemTra;
-- Công thức xung đột chuẩn: Cu.BatDau < Moi.KetThuc AND Cu.KetThuc > Moi.BatDau.
-- Tệp này không tự chèn dữ liệu để có thể chạy nhiều lần an toàn.
GO
