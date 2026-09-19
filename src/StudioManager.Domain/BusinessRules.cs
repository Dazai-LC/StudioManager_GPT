namespace StudioManager.Domain;

public static class BusinessRules
{
    public static bool KhoangThoiGianHopLe(DateTime batDau, DateTime ketThuc) => ketThuc > batDau;
    public static bool BiTrung(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) => aStart < bEnd && aEnd > bStart;
    public static decimal TamTinh(decimal giaGoi, IEnumerable<(decimal SoLuong, decimal DonGia)> services)
        => giaGoi + services.Sum(x => x.SoLuong * x.DonGia);
    public static decimal TongThanhToan(decimal tamTinh, decimal giamGia)
        => giamGia < 0 || giamGia > tamTinh ? throw new ArgumentOutOfRangeException(nameof(giamGia)) : tamTinh - giamGia;
    public static bool DuocChuyen(TrangThaiLich from, TrangThaiLich to) => from.BuocTiepTheo() == to;
    public static bool DuocHuy(TrangThaiLich status, VaiTro role, DateTime batDau, DateTime now)
        => role == VaiTro.QuanTriVien ? status is not TrangThaiLich.HoanThanh and not TrangThaiLich.DaHuy
           : status == TrangThaiLich.DaDatLich && now < batDau;
}
