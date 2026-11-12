namespace StudioManager.WinForms.Contracts;

public enum BookingRequestStatus
{
    CHO_XU_LY = 0,
    CAN_BO_SUNG = 1,
    DA_XAC_NHAN = 2,
    DA_TU_CHOI = 3,
    KHACH_HUY = 4
}

public sealed record BookingRequestServiceLineDto(
    string MaDichVu,
    string TenDichVu,
    string DonViTinh,
    decimal DonGia,
    int SoLuong)
{
    public decimal ThanhTien => DonGia * SoLuong;
}

public sealed record BookingRequestTimelineEventDto(
    string TimestampLabel,
    string ActorLabel,
    string ActionLabel,
    string Detail,
    BookingRequestStatus ResultingStatus);

public sealed class CustomerBookingRequestDto
{
    public required string MaYeuCau { get; init; }
    public required string DemoCustomerId { get; init; }
    public required string TenKhachHang { get; set; }
    public required string SoDienThoai { get; set; }
    public required string Email { get; set; }
    public required string MaGoiChup { get; set; }
    public required string TenGoi { get; set; }
    public required DateTime NgayChupMongMuon { get; set; }
    public required string KhungGioMongMuon { get; set; }
    public required int ThoiLuongPhut { get; set; }
    public string? MaPhongUuTien { get; set; }
    public string? TenPhongUuTien { get; set; }
    public int SoNguoiDuKien { get; set; } = 1;
    public List<BookingRequestServiceLineDto> ExtraServices { get; init; } = [];
    public decimal TamTinhGoi { get; set; }
    public decimal TamTinhDichVu => ExtraServices.Sum(x => x.ThanhTien);
    public decimal TongTamTinh => TamTinhGoi + TamTinhDichVu;
    public string GhiChuKhachHang { get; set; } = "";
    public BookingRequestStatus TrangThai { get; set; } = BookingRequestStatus.CHO_XU_LY;
    public string PhanHoiChoKhach { get; set; } = "";
    public string GhiChuNoiBo { get; set; } = "";
    public string? SuggestedPhotographerCode { get; set; }
    public string? SuggestedPhotographerName { get; set; }
    public string? AssignedRoomCode { get; set; }
    public string? AssignedRoomName { get; set; }
    public string ConflictCheckSummary { get; set; } = "Chưa chạy kiểm tra lịch (Demo)";
    public bool HasConflictWarning { get; set; }
    public string? LinkedBookingCode { get; set; }
    public List<BookingRequestTimelineEventDto> Timeline { get; init; } = [];
    public string CreatedAtLabel { get; init; } = "";
}

public sealed record StudioNotificationDto(
    string NotificationId,
    string Title,
    string Message,
    string Category,
    string CreatedAtLabel,
    bool IsRead,
    string TargetSectionKey,
    string? DemoCustomerId = null);

public sealed class CustomerMockBookingDto
{
    public required string MaLichChup { get; init; }
    public required string DemoCustomerId { get; init; }
    public string? MaYeuCauGoc { get; init; }
    public required string TenKhachHang { get; init; }
    public required string MaGoiChup { get; init; }
    public required string TenGoi { get; init; }
    public required string MaPhong { get; init; }
    public required string TenPhong { get; init; }
    public required string MaNhiepAnhGia { get; init; }
    public required string TenNhiepAnhGia { get; init; }
    public required DateTime NgayChup { get; init; }
    public required string KhungGio { get; init; }
    public required string TrangThaiLich { get; set; }
    public required string PipelineStageLabel { get; set; }
    public int PipelineStepIndex { get; set; }
    public decimal TongGiaTri { get; set; }
    public decimal DaThanhToan { get; set; }
    public decimal ConLai => Math.Max(0, TongGiaTri - DaThanhToan);
    public List<BookingRequestServiceLineDto> SelectedServices { get; init; } = [];
    public string GhiChu { get; init; } = "";
    public string DeliveryStageNote { get; set; } = "";
}

