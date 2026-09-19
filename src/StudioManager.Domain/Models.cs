namespace StudioManager.Domain;

public enum VaiTro { QuanTriVien, NhanVien }
public enum TrangThaiLich { DaDatLich, DaChup, DangChinhSuaAnh, ChoGiaoAnh, DaGiaoAnh, HoanThanh, DaHuy }
public enum ChucVu { TiepNhan, NhiepAnhGia, Khac }
public enum LoaiTaiNguyen { ThietBi, TrangPhuc }
public enum TrangThaiPhanCong { DaPhanCong, DaTra, DaHuy }

public sealed record UserSession(int TaiKhoanId, string TenDangNhap, string HoTen, VaiTro VaiTro);
public sealed record TaiKhoan(int Id, int? NhanVienId, string TenDangNhap, string MatKhauHash, VaiTro VaiTro, bool HoatDong, bool PhaiDoiMatKhau, string HoTen);
public sealed record KhachHang(long Id, string Ma, string HoTen, string SoDienThoai, string? Email, string? DiaChi, string? GhiChu);
public sealed record NhanVien(int Id, string Ma, string HoTen, string SoDienThoai, ChucVu ChucVu, bool DangLam, string? GhiChu);
public sealed record GoiChup(int Id, string Ma, string Ten, decimal Gia, int ThoiLuongPhut, string? MoTa, bool HoatDong);
public sealed record DichVu(int Id, string Ma, string Ten, string DonViTinh, decimal DonGia, string? MoTa, bool HoatDong);
public sealed record PhongChup(int Id, string Ma, string Ten, string? MoTa, bool HoatDong);
public sealed record TaiNguyen(int Id, string Ma, string Ten, LoaiTaiNguyen Loai, int TongSoLuong, int? DichVuThueId, bool HoatDong, string? GhiChu);
public sealed record LichChup(long Id, string Ma, long KhachHangId, string KhachHang, int GoiChupId, string TenGoiChot, decimal GiaGoiChot,
    DateTime BatDau, DateTime KetThuc, int NhiepAnhGiaId, string NhiepAnhGia, int PhongChupId, string Phong, TrangThaiLich TrangThai,
    decimal TienGiam, string? LyDoGiam, string? GhiChu, decimal TienDichVu = 0, decimal DaThu = 0, decimal DaHoan = 0)
{
    public decimal TamTinh => GiaGoiChot + TienDichVu;
    public decimal TongThanhToan => TamTinh - TienGiam;
    public decimal ConLai => TrangThai == TrangThaiLich.DaHuy ? 0 : Math.Max(0, TongThanhToan - DaThu);
}

public static class TrangThaiLichExtensions
{
    public static TrangThaiLich? BuocTiepTheo(this TrangThaiLich value) => value switch
    {
        TrangThaiLich.DaDatLich => TrangThaiLich.DaChup,
        TrangThaiLich.DaChup => TrangThaiLich.DangChinhSuaAnh,
        TrangThaiLich.DangChinhSuaAnh => TrangThaiLich.ChoGiaoAnh,
        TrangThaiLich.ChoGiaoAnh => TrangThaiLich.DaGiaoAnh,
        TrangThaiLich.DaGiaoAnh => TrangThaiLich.HoanThanh,
        _ => null
    };

    public static string HienThi(this TrangThaiLich value) => value switch
    {
        TrangThaiLich.DaDatLich => "Đã đặt lịch", TrangThaiLich.DaChup => "Đã chụp",
        TrangThaiLich.DangChinhSuaAnh => "Đang chỉnh sửa ảnh", TrangThaiLich.ChoGiaoAnh => "Chờ giao ảnh",
        TrangThaiLich.DaGiaoAnh => "Đã giao ảnh", TrangThaiLich.HoanThanh => "Hoàn thành", _ => "Đã hủy"
    };
}
