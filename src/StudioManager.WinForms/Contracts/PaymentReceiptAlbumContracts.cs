namespace StudioManager.WinForms.Contracts;

public enum PaymentReconciliationStatus
{
    CHO_DOI_SOAT = 0,
    DA_KHOP_THU_CONG = 1,
    SAI_LECH_THONG_TIN = 2,
    THANH_TOAN_TIEN_MAT = 3,
    TU_CHOI = 4
}

public enum PaymentMethodKind
{
    VietQrChuyenKhoan = 0,
    ChuyenKhoanThuCong = 1,
    TienMatTaiQuay = 2
}

public enum ReceiptFormatKind
{
    A4Invoice = 0,
    ThermalK80 = 1,
    ThermalK58 = 2,
    PdfLayoutPreview = 3
}

public sealed record VietQrInstructionDto(
    string BankDisplayName,
    string AccountHolder,
    string MaskedAccountNumber,
    string TransferSyntax,
    decimal SuggestedAmount,
    string ReferenceCode,
    string DisclaimerText);

public sealed class PaymentSubmissionDto
{
    public required string MaGiaoDichDemo { get; init; }
    public required string DemoCustomerId { get; init; }
    public required string MaThamChieu { get; init; }
    public required string TargetType { get; init; }
    public required string TenKhachHang { get; init; }
    public required string SoDienThoai { get; init; }
    public PaymentMethodKind PhuongThuc { get; set; }
    public decimal SoTienKhaiBao { get; set; }
    public decimal SoTienXacNhan { get; set; }
    public required string NoiDungChuyenKhoan { get; init; }
    public PaymentReconciliationStatus TrangThai { get; set; }
    public bool CustomerConfirmedTransfer { get; set; }
    public string? EvidenceFileName { get; set; }
    public string? EvidenceFilePath { get; set; }
    public string EvidenceNotes { get; set; } = "";
    public string StaffReviewerNote { get; set; } = "";
    public string CreatedAtLabel { get; init; } = "";
    public string? ReconciledAtLabel { get; set; }
}

public sealed record ReceiptLineItemDto(
    string Description,
    string UnitLabel,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record ReceiptDocumentDto(
    string ReceiptNumber,
    ReceiptFormatKind Format,
    string IssuedAtLabel,
    string ReferenceCode,
    string CustomerName,
    string CustomerPhone,
    string PackageName,
    string RoomName,
    IReadOnlyList<ReceiptLineItemDto> LineItems,
    decimal Subtotal,
    decimal PaidAmount,
    decimal RemainingAmount,
    string PaymentMethodLabel,
    string StaffIssuerName,
    string FooterDisclaimer);

public sealed record CustomerAlbumPhotoDto(
    string PhotoId,
    string Title,
    string Caption,
    string RelativeAssetPath,
    string? ResolvedFullPath,
    string ShotLabel,
    string TakenAtLabel,
    bool IsRetouched);

public sealed class CustomerPhotoAlbumDto
{
    public required string AlbumId { get; init; }
    public required string DemoCustomerId { get; init; }
    public required string MaLichChup { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string DeliveryStatus { get; set; }
    public int DeliveryProgressPercent { get; set; }
    public string OwnerScopeLabel { get; init; } = "";
    public string UpdatedAtLabel { get; init; } = "";
    public required string CoverAssetId { get; init; }
    public List<CustomerAlbumPhotoDto> Photos { get; init; } = [];
    public bool IsDemoSample { get; init; } = true;
}

public sealed record PaymentTargetSnapshot(
    string ReferenceCode,
    string TargetType,
    string CustomerName,
    string CustomerPhone,
    string PackageName,
    string RoomName,
    IReadOnlyList<BookingRequestServiceLineDto> Services,
    decimal PackagePrice,
    decimal TotalOrderAmount,
    decimal AlreadyConfirmedPaid,
    decimal RemainingBeforeThisTx);
