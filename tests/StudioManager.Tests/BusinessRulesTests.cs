using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

public sealed class BusinessRulesTests
{
    [Fact] public void HaiKhoangGiapRanh_KhongTrung()
    {
        var a=DateTime.Today.AddHours(8); Assert.False(BusinessRules.BiTrung(a,a.AddHours(2),a.AddHours(2),a.AddHours(3)));
    }
    [Fact] public void HaiKhoangGiaoNhau_BiTrung()
    {
        var a=DateTime.Today.AddHours(8); Assert.True(BusinessRules.BiTrung(a,a.AddHours(2),a.AddHours(1),a.AddHours(3)));
    }
    [Fact] public void KetThucBangBatDau_KhongHopLe()=>Assert.False(BusinessRules.KhoangThoiGianHopLe(DateTime.Today,DateTime.Today));
    [Fact] public void TinhTien_DungCongThuc()
    {
        var tam=BusinessRules.TamTinh(1_500_000,[(2,100_000),(1,250_000)]);Assert.Equal(1_950_000,tam);Assert.Equal(1_750_000,BusinessRules.TongThanhToan(tam,200_000));
    }
    [Fact] public void GiamGiaVuotTamTinh_BiTuChoi()=>Assert.Throws<ArgumentOutOfRangeException>(()=>BusinessRules.TongThanhToan(100,101));
    [Theory]
    [InlineData(TrangThaiLich.DaDatLich,TrangThaiLich.DaChup,true)]
    [InlineData(TrangThaiLich.DaDatLich,TrangThaiLich.DangChinhSuaAnh,false)]
    [InlineData(TrangThaiLich.HoanThanh,TrangThaiLich.DaGiaoAnh,false)]
    public void ChuyenTrangThai_DungThuTu(TrangThaiLich from,TrangThaiLich to,bool expected)=>Assert.Equal(expected,BusinessRules.DuocChuyen(from,to));
    [Fact] public void NhanVien_KhongHuyLichDaChup()=>Assert.False(BusinessRules.DuocHuy(TrangThaiLich.DaChup,VaiTro.NhanVien,DateTime.Now.AddDays(1),DateTime.Now));
    [Fact] public void QuanTriVien_HuyDuocLichChuaHoanThanh()=>Assert.True(BusinessRules.DuocHuy(TrangThaiLich.DaChup,VaiTro.QuanTriVien,DateTime.Now.AddDays(-1),DateTime.Now));
}
