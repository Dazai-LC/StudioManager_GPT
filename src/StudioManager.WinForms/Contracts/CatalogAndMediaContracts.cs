namespace StudioManager.WinForms.Contracts;

public enum MediaSourceKind
{
    LocalAiDefault = 0,
    AdminUploadPreview = 1,
    SystemFallback = 2
}

public enum MediaTargetEntityKind
{
    Package = 0,
    Room = 1,
    Resource = 2,
    StudioAlbum = 3
}

public sealed record VisualAssetEntryDto(
    string AssetId,
    MediaTargetEntityKind TargetEntityKind,
    string TargetCode,
    string Title,
    string Caption,
    string AltText,
    string ExpectedRelativePath,
    string? ResolvedFullPath,
    bool IsPrimaryCover,
    bool IsIllustrative,
    MediaSourceKind SourceKind,
    int SortOrder,
    long FileSizeBytes,
    int PixelWidth,
    int PixelHeight,
    string UploadedByDisplay,
    string UpdatedAtLabel)
{
    public string SourceBadgeLabel => SourceKind switch
    {
        MediaSourceKind.AdminUploadPreview => "Ảnh quản trị (Demo)",
        MediaSourceKind.LocalAiDefault => "Ảnh concept minh họa",
        _ => "Hình thay thế"
    };
}

public sealed record CatalogPackageViewDto(
    string MaGoiChup,
    string TenGoi,
    string MoTa,
    decimal GiaCoBan,
    int ThoiLuongPhut,
    string SuggestedRoomCode,
    string Highlights,
    bool IsPopular,
    string CoverAssetId,
    IReadOnlyList<VisualAssetEntryDto> Gallery,
    bool FromLiveSql);

public sealed record CatalogRoomViewDto(
    string MaPhong,
    string TenPhong,
    string MoTa,
    string StyleSummary,
    string Notes,
    string CoverAssetId,
    IReadOnlyList<VisualAssetEntryDto> Gallery,
    bool FromLiveSql);

public sealed record CatalogResourceViewDto(
    string MaTaiNguyen,
    string TenTaiNguyen,
    string LoaiTaiNguyen,
    int SoLuong,
    string? MaDichVuThue,
    string IllustrativeNote,
    string CoverAssetId,
    IReadOnlyList<VisualAssetEntryDto> Gallery,
    bool FromLiveSql)
{
    public string LoaiHienThi => LoaiTaiNguyen == "TRANG_PHUC" ? "Trang phục & đạo cụ" : "Thiết bị chuyên dụng";
}

public sealed record CatalogServiceViewDto(
    string MaDichVu,
    string TenDichVu,
    string DonViTinh,
    decimal DonGia,
    string MoTa,
    bool FromLiveSql);

