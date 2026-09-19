USE StudioManager;
GO

CREATE TABLE NhanVien(
 NhanVienId int IDENTITY PRIMARY KEY, MaNhanVien varchar(20) NOT NULL UNIQUE, HoTen nvarchar(100) NOT NULL,
 SoDienThoai varchar(15) NOT NULL, ChucVu varchar(30) NOT NULL CHECK(ChucVu IN('TIEP_NHAN','NHIEP_ANH_GIA','KHAC')),
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_LAM' CHECK(TrangThai IN('DANG_LAM','NGUNG_LAM')), GhiChu nvarchar(500) NULL,
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_NhanVien_HoTen ON NhanVien(HoTen);

CREATE TABLE TaiKhoan(
 TaiKhoanId int IDENTITY PRIMARY KEY, NhanVienId int NULL REFERENCES NhanVien(NhanVienId), TenDangNhap varchar(50) NOT NULL UNIQUE,
 MatKhauHash nvarchar(255) NOT NULL, VaiTro varchar(20) NOT NULL CHECK(VaiTro IN('QUAN_TRI_VIEN','NHAN_VIEN')),
 TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','BI_KHOA')), PhaiDoiMatKhau bit NOT NULL DEFAULT 1,
 LanDangNhapCuoi datetime2(0) NULL, CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE UNIQUE INDEX UX_TaiKhoan_NhanVien ON TaiKhoan(NhanVienId) WHERE NhanVienId IS NOT NULL;

CREATE TABLE KhachHang(
 KhachHangId bigint IDENTITY PRIMARY KEY, MaKhachHang varchar(20) NOT NULL UNIQUE, HoTen nvarchar(100) NOT NULL,
 SoDienThoai varchar(15) NOT NULL, DiaChi nvarchar(255) NULL, Email varchar(254) NULL, GhiChu nvarchar(500) NULL,
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_KhachHang_HoTen ON KhachHang(HoTen); CREATE INDEX IX_KhachHang_SDT ON KhachHang(SoDienThoai);

CREATE TABLE GoiChup(
 GoiChupId int IDENTITY PRIMARY KEY, MaGoiChup varchar(20) NOT NULL UNIQUE, TenGoiChup nvarchar(150) NOT NULL,
 MoTa nvarchar(1000) NULL, GiaGoi decimal(18,2) NOT NULL CHECK(GiaGoi>=0), ThoiLuongPhut int NOT NULL CHECK(ThoiLuongPhut>0),
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_AP_DUNG' CHECK(TrangThai IN('DANG_AP_DUNG','NGUNG_AP_DUNG')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE DichVu(
 DichVuId int IDENTITY PRIMARY KEY, MaDichVu varchar(20) NOT NULL UNIQUE, TenDichVu nvarchar(150) NOT NULL,
 DonViTinh nvarchar(30) NOT NULL, DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0), MoTa nvarchar(1000) NULL,
 TrangThai varchar(20) NOT NULL DEFAULT 'DANG_CUNG_CAP' CHECK(TrangThai IN('DANG_CUNG_CAP','NGUNG_CUNG_CAP')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE PhongChup(
 PhongChupId int IDENTITY PRIMARY KEY, MaPhong varchar(20) NOT NULL UNIQUE, TenPhong nvarchar(100) NOT NULL, MoTa nvarchar(500) NULL,
 TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','NGUNG_SU_DUNG')),
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE TABLE TaiNguyen(
 TaiNguyenId int IDENTITY PRIMARY KEY, MaTaiNguyen varchar(20) NOT NULL UNIQUE, TenTaiNguyen nvarchar(150) NOT NULL,
 LoaiTaiNguyen varchar(20) NOT NULL CHECK(LoaiTaiNguyen IN('THIET_BI','TRANG_PHUC')), TongSoLuong int NOT NULL CHECK(TongSoLuong>0),
 DichVuThueId int NULL REFERENCES DichVu(DichVuId), TrangThai varchar(20) NOT NULL DEFAULT 'HOAT_DONG' CHECK(TrangThai IN('HOAT_DONG','NGUNG_SU_DUNG')),
 GhiChu nvarchar(500) NULL, CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());

CREATE TABLE LichChup(
 LichChupId bigint IDENTITY PRIMARY KEY, MaLichChup varchar(25) NOT NULL UNIQUE, KhachHangId bigint NOT NULL REFERENCES KhachHang(KhachHangId),
 GoiChupId int NOT NULL REFERENCES GoiChup(GoiChupId), TenGoiChot nvarchar(150) NOT NULL, GiaGoiChot decimal(18,2) NOT NULL CHECK(GiaGoiChot>=0),
 BatDau datetime2(0) NOT NULL, KetThuc datetime2(0) NOT NULL, NhiepAnhGiaId int NOT NULL REFERENCES NhanVien(NhanVienId),
 PhongChupId int NOT NULL REFERENCES PhongChup(PhongChupId), TrangThai varchar(30) NOT NULL CHECK(TrangThai IN('DA_DAT_LICH','DA_CHUP','DANG_CHINH_SUA','CHO_GIAO_ANH','DA_GIAO_ANH','HOAN_THANH','DA_HUY')),
 TienGiam decimal(18,2) NOT NULL DEFAULT 0 CHECK(TienGiam>=0), LyDoGiam nvarchar(500) NULL, GhiChu nvarchar(1000) NULL,
 LyDoHuy nvarchar(500) NULL, HuyBoiTaiKhoanId int NULL REFERENCES TaiKhoan(TaiKhoanId), HuyLuc datetime2(0) NULL, HoanThanhLuc datetime2(0) NULL,
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT CK_LichChup_ThoiGian CHECK(KetThuc>BatDau), CONSTRAINT CK_LichChup_LyDoGiam CHECK(TienGiam=0 OR LyDoGiam IS NOT NULL));
CREATE INDEX IX_LichChup_ThoiGian ON LichChup(BatDau,KetThuc); CREATE INDEX IX_LichChup_AnhGia ON LichChup(NhiepAnhGiaId,BatDau);
CREATE INDEX IX_LichChup_Phong ON LichChup(PhongChupId,BatDau); CREATE INDEX IX_LichChup_TrangThai ON LichChup(TrangThai,BatDau);

CREATE TABLE LichChupDichVu(
 LichChupDichVuId bigint IDENTITY PRIMARY KEY, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId), DichVuId int NOT NULL REFERENCES DichVu(DichVuId),
 TenDichVuChot nvarchar(150) NOT NULL, DonViTinhChot nvarchar(30) NOT NULL, DonGiaChot decimal(18,2) NOT NULL CHECK(DonGiaChot>=0), SoLuong decimal(10,2) NOT NULL CHECK(SoLuong>0),
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
 CONSTRAINT UX_LichChupDichVu UNIQUE(LichChupId,DichVuId));
CREATE TABLE PhanCongTaiNguyen(
 PhanCongId bigint IDENTITY PRIMARY KEY, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId), TaiNguyenId int NOT NULL REFERENCES TaiNguyen(TaiNguyenId), SoLuong int NOT NULL CHECK(SoLuong>0),
 BatDauSuDung datetime2(0) NOT NULL, KetThucSuDung datetime2(0) NOT NULL, TrangThai varchar(20) NOT NULL CHECK(TrangThai IN('DA_PHAN_CONG','DA_TRA','DA_HUY')),
 CreatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), CreatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), UpdatedBy int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), UpdatedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME(), CHECK(KetThucSuDung>BatDauSuDung));
CREATE INDEX IX_PhanCong_XungDot ON PhanCongTaiNguyen(TaiNguyenId,BatDauSuDung,KetThucSuDung,TrangThai);
CREATE TABLE ThanhToan(
 ThanhToanId bigint IDENTITY PRIMARY KEY, MaThanhToan varchar(25) NOT NULL UNIQUE, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId),
 LoaiThu varchar(20) NOT NULL CHECK(LoaiThu IN('DAT_COC','BO_SUNG','CON_LAI')), SoTien decimal(18,2) NOT NULL CHECK(SoTien>0), NgayGiaoDich datetime2(0) NOT NULL,
 TaiKhoanThucHienId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), GhiChu nvarchar(500) NULL);
CREATE INDEX IX_ThanhToan_LichNgay ON ThanhToan(LichChupId,NgayGiaoDich);
CREATE TABLE HoanTien(
 HoanTienId bigint IDENTITY PRIMARY KEY, MaHoanTien varchar(25) NOT NULL UNIQUE, LichChupId bigint NOT NULL REFERENCES LichChup(LichChupId),
 SoTien decimal(18,2) NOT NULL CHECK(SoTien>0), NgayGiaoDich datetime2(0) NOT NULL, TaiKhoanThucHienId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), LyDo nvarchar(500) NOT NULL, GhiChu nvarchar(500) NULL);
CREATE INDEX IX_HoanTien_LichNgay ON HoanTien(LichChupId,NgayGiaoDich);
CREATE TABLE NhatKyHeThong(
 NhatKyId bigint IDENTITY PRIMARY KEY, TaiKhoanId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), HanhDong varchar(50) NOT NULL,
 LoaiDoiTuong varchar(50) NOT NULL, DoiTuongId varchar(50) NOT NULL, GiaTriCu nvarchar(max) NULL, GiaTriMoi nvarchar(max) NULL, LyDo nvarchar(500) NULL, ThoiDiem datetime2(0) NOT NULL DEFAULT SYSDATETIME());
CREATE INDEX IX_NhatKy_TaiKhoan ON NhatKyHeThong(TaiKhoanId,ThoiDiem); CREATE INDEX IX_NhatKy_DoiTuong ON NhatKyHeThong(LoaiDoiTuong,DoiTuongId,ThoiDiem);
CREATE TABLE NhatKySaoLuu(
 NhatKySaoLuuId bigint IDENTITY PRIMARY KEY, LoaiThaoTac varchar(20) NOT NULL CHECK(LoaiThaoTac IN('SAO_LUU','PHUC_HOI')),
 DuongDanTep nvarchar(1000) NOT NULL, TrangThai varchar(20) NOT NULL CHECK(TrangThai IN('THANH_CONG','THAT_BAI')), ThongBao nvarchar(1000) NULL,
 TaiKhoanId int NOT NULL REFERENCES TaiKhoan(TaiKhoanId), ThoiDiem datetime2(0) NOT NULL DEFAULT SYSDATETIME());
GO
