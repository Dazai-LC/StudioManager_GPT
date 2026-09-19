USE StudioManager;
GO
SET NOCOUNT ON;

INSERT NhanVien(MaNhanVien,HoTen,SoDienThoai,ChucVu,TrangThai) VALUES
('NV000001',N'Nguyễn Minh Anh','0901000001','TIEP_NHAN','DANG_LAM'),('NV000002',N'Trần Thu Hà','0901000002','TIEP_NHAN','DANG_LAM'),
('NV000003',N'Lê Hoàng Nam','0901000003','TIEP_NHAN','DANG_LAM'),('NV000004',N'Phạm Gia Huy','0901000004','NHIEP_ANH_GIA','DANG_LAM'),
('NV000005',N'Võ Khánh Linh','0901000005','NHIEP_ANH_GIA','DANG_LAM'),('NV000006',N'Đỗ Quang Minh','0901000006','NHIEP_ANH_GIA','DANG_LAM'),
('NV000007',N'Bùi Ngọc Mai','0901000007','NHIEP_ANH_GIA','DANG_LAM'),('NV000008',N'Ngô Đức Long','0901000008','NHIEP_ANH_GIA','NGUNG_LAM');

-- Mật khẩu demo cho cả ba tài khoản: Admin@123 (bắt buộc đổi khi dùng thật)
DECLARE @Hash nvarchar(255)=N'PBKDF2-SHA256$210000$384+g6Fb4W74qN4ljb5MzA==$jM2+11okSJfLHLnG27IkEdW8bM/kJ6+PWoAggBn7Mr8=';
INSERT TaiKhoan(NhanVienId,TenDangNhap,MatKhauHash,VaiTro,TrangThai,PhaiDoiMatKhau) VALUES
(1,'admin',@Hash,'QUAN_TRI_VIEN','HOAT_DONG',1),(2,'nhanvien1',@Hash,'NHAN_VIEN','HOAT_DONG',1),(3,'nhanvien2',@Hash,'NHAN_VIEN','HOAT_DONG',1);

INSERT GoiChup(MaGoiChup,TenGoiChup,MoTa,GiaGoi,ThoiLuongPhut) VALUES
('GC000001',N'Chân dung cá nhân',N'01 concept, 10 ảnh chỉnh sửa',1500000,120),('GC000002',N'Gia đình',N'Tối đa 6 thành viên',2800000,180),
('GC000003',N'Em bé',N'Phông nền và phụ kiện cơ bản',2200000,150),('GC000004',N'Cặp đôi',N'02 concept studio',2500000,180),('GC000005',N'Kỷ yếu',N'Gói nhóm ngoại cảnh',6500000,300);
INSERT DichVu(MaDichVu,TenDichVu,DonViTinh,DonGia,MoTa) VALUES
('DV000001',N'Trang điểm',N'Người',450000,N'Trang điểm studio'),('DV000002',N'Thuê trang phục',N'Bộ',300000,N'Trang phục có sẵn'),
('DV000003',N'In ảnh 15x21',N'Tấm',30000,NULL),('DV000004',N'In album',N'Cuốn',850000,NULL),('DV000005',N'Chụp thêm giờ',N'Giờ',500000,NULL),
('DV000006',N'Chỉnh sửa ảnh bổ sung',N'Ảnh',50000,NULL),('DV000007',N'Phông nền cao cấp',N'Bộ',250000,NULL),('DV000008',N'Giao ảnh nhanh',N'Gói',400000,NULL);
INSERT PhongChup(MaPhong,TenPhong,MoTa) VALUES ('P0001',N'Phòng Ánh Sáng',N'Phong cách tối giản'),('P0002',N'Phòng Cổ Điển',N'Nội thất vintage'),('P0003',N'Phòng Em Bé',N'An toàn, nhiều đạo cụ');
INSERT TaiNguyen(MaTaiNguyen,TenTaiNguyen,LoaiTaiNguyen,TongSoLuong,DichVuThueId) VALUES
('TN000001',N'Canon R6 Mark II','THIET_BI',2,NULL),('TN000002',N'Sony A7 IV','THIET_BI',2,NULL),('TN000003',N'Đèn Godox AD600','THIET_BI',4,NULL),
('TN000004',N'Đèn LED RGB','THIET_BI',3,NULL),('TN000005',N'Phông giấy trắng','THIET_BI',2,NULL),('TN000006',N'Váy cưới trắng','TRANG_PHUC',3,2),
('TN000007',N'Áo dài đỏ','TRANG_PHUC',4,2),('TN000008',N'Vest nam','TRANG_PHUC',4,2),('TN000009',N'Váy công chúa bé','TRANG_PHUC',5,2),('TN000010',N'Đạo cụ sinh nhật','THIET_BI',3,NULL);

DECLARE @i int=1;
WHILE @i<=30 BEGIN
 INSERT KhachHang(MaKhachHang,HoTen,SoDienThoai,Email,DiaChi) VALUES
 (CONCAT('KH',RIGHT('000000'+CAST(@i AS varchar(6)),6)),CONCAT(N'Khách hàng ',RIGHT('00'+CAST(@i AS varchar(2)),2)),CONCAT('0912',RIGHT('000000'+CAST(@i AS varchar(6)),6)),CONCAT('khach',@i,'@example.com'),N'Thành phố Hồ Chí Minh'); SET @i+=1;
END

DECLARE @Admin int=(SELECT TaiKhoanId FROM TaiKhoan WHERE TenDangNhap='admin');
INSERT LichChup(MaLichChup,KhachHangId,GoiChupId,TenGoiChot,GiaGoiChot,BatDau,KetThuc,NhiepAnhGiaId,PhongChupId,TrangThai,TienGiam,LyDoGiam,GhiChu,LyDoHuy,HuyBoiTaiKhoanId,HuyLuc,HoanThanhLuc,CreatedBy,UpdatedBy)
VALUES
('LCDEMO-0001',1,1,N'Chân dung cá nhân',1500000,DATEADD(hour,9,CAST(CAST(GETDATE() AS date) AS datetime2)),DATEADD(hour,11,CAST(CAST(GETDATE() AS date) AS datetime2)),4,1,'DA_DAT_LICH',0,NULL,N'Lịch hôm nay',NULL,NULL,NULL,NULL,@Admin,@Admin),
('LCDEMO-0002',2,2,N'Gia đình',2800000,DATEADD(day,1,DATEADD(hour,8,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,1,DATEADD(hour,11,CAST(CAST(GETDATE() AS date) AS datetime2))),5,2,'DA_CHUP',0,NULL,NULL,NULL,NULL,NULL,NULL,@Admin,@Admin),
('LCDEMO-0003',3,3,N'Em bé',2200000,DATEADD(day,-2,DATEADD(hour,8,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,-2,DATEADD(minute,630,CAST(CAST(GETDATE() AS date) AS datetime2))),6,3,'DANG_CHINH_SUA',200000,N'Khách hàng thân thiết',NULL,NULL,NULL,NULL,NULL,@Admin,@Admin),
('LCDEMO-0004',4,4,N'Cặp đôi',2500000,DATEADD(day,-5,DATEADD(hour,14,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,-5,DATEADD(hour,17,CAST(CAST(GETDATE() AS date) AS datetime2))),7,1,'CHO_GIAO_ANH',0,NULL,NULL,NULL,NULL,NULL,NULL,@Admin,@Admin),
('LCDEMO-0005',5,1,N'Chân dung cá nhân',1500000,DATEADD(day,-8,DATEADD(hour,10,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,-8,DATEADD(hour,12,CAST(CAST(GETDATE() AS date) AS datetime2))),4,2,'DA_GIAO_ANH',0,NULL,NULL,NULL,NULL,NULL,NULL,@Admin,@Admin),
('LCDEMO-0006',6,2,N'Gia đình',2800000,DATEADD(day,-20,DATEADD(hour,8,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,-20,DATEADD(hour,11,CAST(CAST(GETDATE() AS date) AS datetime2))),5,2,'HOAN_THANH',0,NULL,NULL,NULL,NULL,NULL,DATEADD(day,-15,GETDATE()),@Admin,@Admin),
('LCDEMO-0007',7,5,N'Kỷ yếu',6500000,DATEADD(day,-10,DATEADD(hour,7,CAST(CAST(GETDATE() AS date) AS datetime2))),DATEADD(day,-10,DATEADD(hour,12,CAST(CAST(GETDATE() AS date) AS datetime2))),6,3,'DA_HUY',0,NULL,NULL,N'Thời tiết xấu',@Admin,DATEADD(day,-11,GETDATE()),NULL,@Admin,@Admin);
INSERT ThanhToan(MaThanhToan,LichChupId,LoaiThu,SoTien,NgayGiaoDich,TaiKhoanThucHienId,GhiChu) VALUES
('TTDEMO-0001',1,'DAT_COC',500000,GETDATE(),@Admin,N'Đặt cọc'),('TTDEMO-0002',3,'DAT_COC',1000000,DATEADD(day,-3,GETDATE()),@Admin,NULL),
('TTDEMO-0003',6,'CON_LAI',2800000,DATEADD(day,-15,GETDATE()),@Admin,NULL),('TTDEMO-0004',7,'DAT_COC',2000000,DATEADD(day,-12,GETDATE()),@Admin,NULL);
INSERT HoanTien(MaHoanTien,LichChupId,SoTien,NgayGiaoDich,TaiKhoanThucHienId,LyDo) VALUES ('HTDEMO-0001',7,1000000,DATEADD(day,-10,GETDATE()),@Admin,N'Hoàn một phần theo chính sách');
GO
