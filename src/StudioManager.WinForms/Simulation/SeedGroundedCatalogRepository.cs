using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Simulation;

/// <summary>
/// Provides verified catalog items grounded in SQL seed definitions (03_SeedData.sql).
/// Optionally performs read-only queries against AppFacade when a live SQL Staff/Admin session exists;
/// never mutates SQL Server or invents ungrounded commercial packages, rooms, or photographers.
/// </summary>
public static class SeedGroundedCatalogRepository
{
    public sealed record PhotographerRefDto(string MaNhanVien, string HoTen, string TrangThaiLabel, bool IsActive);

    public static IReadOnlyList<CatalogPackageViewDto> GetPackages()
    {
        VisualAssetService.EnsureInitialized();
        return
        [
            new("GC000001", "Chân dung cá nhân", "01 concept, 10 ảnh chỉnh sửa", 1_500_000m, 120, "P0001",
                "Phù hợp chụp chân dung nghệ thuật, profile cá nhân hoặc lookbook gọn nhẹ.", false,
                "DEF-GC000001", VisualAssetService.GetGallery("GC000001"), false),
            new("GC000002", "Gia đình", "Tối đa 6 thành viên", 2_800_000m, 180, "P0002",
                "Ghi lại khoảnh khắc gia đình ấm áp với bố cục nhiều thế hệ trong 180 phút.", true,
                "DEF-GC000002", VisualAssetService.GetGallery("GC000002"), false),
            new("GC000003", "Em bé", "Phông nền và phụ kiện cơ bản", 2_200_000m, 150, "P0003",
                "Không gian an toàn, ánh sáng dịu và phụ kiện mềm dành riêng cho trẻ nhỏ.", false,
                "DEF-GC000003", VisualAssetService.GetGallery("GC000003"), false),
            new("GC000004", "Cặp đôi", "02 concept studio", 2_500_000m, 180, "P0002",
                "Thực hiện 02 concept trong studio với thời lượng rộng rãi cho thay trang phục.", true,
                "DEF-GC000004", VisualAssetService.GetGallery("GC000004"), false),
            new("GC000005", "Kỷ yếu", "Gói nhóm ngoại cảnh", 6_500_000m, 300, "P0001",
                "Thời lượng 300 phút phục vụ chụp tập thể theo lịch điều phối ngoại cảnh.", false,
                "DEF-GC000005", VisualAssetService.GetGallery("GC000005"), false)
        ];
    }

    public static IReadOnlyList<CatalogRoomViewDto> GetRooms()
    {
        VisualAssetService.EnsureInitialized();
        return
        [
            new("P0001", "Phòng Ánh Sáng", "Phong cách tối giản",
                "Không gian sáng sủa, tập trung đường nét tinh giản và ánh sáng sạch.",
                "Việc phân bổ phòng chính thức phụ thuộc lịch điều phối thực tế của nhân sự studio.",
                "DEF-P0001", VisualAssetService.GetGallery("P0001"), false),
            new("P0002", "Phòng Cổ Điển", "Nội thất vintage",
                "Bối cảnh tông trầm ấm, nội thất cổ điển phù hợp concept cặp đôi và gia đình.",
                "Chỉ ghi nhận phòng ưu tiên ở bước yêu cầu; studio sẽ kiểm tra trùng lịch trước khi chốt.",
                "DEF-P0002", VisualAssetService.GetGallery("P0002"), false),
            new("P0003", "Phòng Em Bé", "An toàn, nhiều đạo cụ",
                "Không gian dịu mắt với nhiều đạo cụ an toàn thân thiện cho buổi chụp em bé.",
                "Các phụ kiện cụ thể được nhân sự chuẩn bị theo ghi chú yêu cầu đặt lịch.",
                "DEF-P0003", VisualAssetService.GetGallery("P0003"), false)
        ];
    }

    public static IReadOnlyList<CatalogResourceViewDto> GetResources()
    {
        VisualAssetService.EnsureInitialized();
        return
        [
            BuildResource("TN000001", "Canon R6 Mark II", "THIET_BI", 2, null, "Thiết bị chụp chính theo phân công của nhiếp ảnh gia."),
            BuildResource("TN000002", "Sony A7 IV", "THIET_BI", 2, null, "Thiết bị chụp full-frame dùng cho các concept studio và sự kiện."),
            BuildResource("TN000003", "Đèn Godox AD600", "THIET_BI", 4, null, "Đèn chiếu sáng studio do kỹ thuật và nhiếp ảnh gia bố trí."),
            BuildResource("TN000004", "Đèn LED RGB", "THIET_BI", 3, null, "Ánh sáng màu hỗ trợ các concept chân dung và sáng tạo."),
            BuildResource("TN000005", "Phông giấy trắng", "THIET_BI", 2, null, "Phông nền trắng tiêu chuẩn sạch gọn cho ảnh hồ sơ / chân dung."),
            BuildResource("TN000006", "Váy cưới trắng", "TRANG_PHUC", 3, "DV000002", "Trang phục cho thuê kèm dịch vụ DV000002 (Thuê trang phục)."),
            BuildResource("TN000007", "Áo dài đỏ", "TRANG_PHUC", 4, "DV000002", "Trang phục truyền thống cho thuê kèm dịch vụ DV000002."),
            BuildResource("TN000008", "Vest nam", "TRANG_PHUC", 4, "DV000002", "Vest nam trưng bày minh họa; nhân sự kiểm tra size và tình trạng thực tế."),
            BuildResource("TN000009", "Váy công chúa bé", "TRANG_PHUC", 5, "DV000002", "Trang phục cho bé; ghi chú độ tuổi/chiều cao khi gửi yêu cầu đặt lịch."),
            BuildResource("TN000010", "Đạo cụ sinh nhật", "THIET_BI", 3, null, "Đạo cụ trang trí concept sinh nhật tại studio.")
        ];
    }

    public static IReadOnlyList<CatalogServiceViewDto> GetServices() =>
    [
        new("DV000001", "Trang điểm", "Người", 450_000m, "Trang điểm studio", false),
        new("DV000002", "Thuê trang phục", "Bộ", 300_000m, "Trang phục có sẵn", false),
        new("DV000003", "In ảnh 15x21", "Tấm", 30_000m, "Ảnh lụa cao cấp", false),
        new("DV000004", "In album", "Cuốn", 850_000m, "Album 20 trang", false),
        new("DV000005", "Chụp thêm giờ", "Giờ", 500_000m, "Phát sinh ngoài gói", false),
        new("DV000006", "Chỉnh sửa ảnh bổ sung", "Ảnh", 50_000m, "Retouch nâng cao", false),
        new("DV000007", "Phông nền cao cấp", "Bộ", 250_000m, "Bối cảnh thiết kế", false),
        new("DV000008", "Giao ảnh nhanh", "Gói", 400_000m, "Trả trong 24h", false)
    ];

    public static IReadOnlyList<PhotographerRefDto> GetPhotographers() =>
    [
        new("NV000004", "Phạm Gia Huy", "Đang làm việc", true),
        new("NV000005", "Võ Khánh Linh", "Đang làm việc", true),
        new("NV000006", "Đỗ Quang Minh", "Đang làm việc", true),
        new("NV000007", "Bùi Ngọc Mai", "Đang làm việc", true),
        new("NV000008", "Ngô Đức Long", "Ngừng làm việc (chỉ lưu lịch sử)", false)
    ];

    public static async Task<IReadOnlyList<CatalogPackageViewDto>> TryLoadLivePackagesReadOnlyAsync(AppFacade? app, CancellationToken ct = default)
    {
        var baseline = GetPackages().ToList();
        if (app?.Session is null) return baseline;

        try
        {
            var rows = await app.Administration.QueryGridAsync("GoiChup", null, app.Session, ct);
            if (rows.Count == 0) return baseline;

            var byCode = baseline.ToDictionary(x => x.MaGoiChup, StringComparer.OrdinalIgnoreCase);
            var merged = new List<CatalogPackageViewDto>();
            foreach (var r in rows)
            {
                var code = GetCell(r, "Mã", "MaGoiChup");
                var name = GetCell(r, "Tên gói", "TenGoiChup", "TenGoi");
                var desc = GetCell(r, "Mô tả", "MoTa");
                var price = decimal.TryParse(GetCell(r, "Giá", "GiaGoi", "GiaCoBan"), out var dp) ? dp : 0m;
                var dur = int.TryParse(GetCell(r, "Thời lượng", "ThoiLuongPhut"), out var di) ? di : 120;
                if (string.IsNullOrWhiteSpace(code)) continue;

                byCode.TryGetValue(code, out var seedItem);
                merged.Add(new CatalogPackageViewDto(
                    MaGoiChup: code,
                    TenGoi: string.IsNullOrWhiteSpace(name) ? seedItem?.TenGoi ?? code : name,
                    MoTa: string.IsNullOrWhiteSpace(desc) ? seedItem?.MoTa ?? "Đang cập nhật mô tả trong SQL Server." : desc,
                    GiaCoBan: price > 0 ? price : seedItem?.GiaCoBan ?? 0m,
                    ThoiLuongPhut: dur > 0 ? dur : seedItem?.ThoiLuongPhut ?? 120,
                    SuggestedRoomCode: seedItem?.SuggestedRoomCode ?? "P0001",
                    Highlights: seedItem?.Highlights ?? "Dữ liệu đọc chỉ xem từ SQL Server.",
                    IsPopular: seedItem?.IsPopular ?? false,
                    CoverAssetId: $"DEF-{code}",
                    Gallery: VisualAssetService.GetGallery(code),
                    FromLiveSql: true));
            }
            return merged.Count > 0 ? [.. merged.OrderBy(x => x.MaGoiChup, StringComparer.OrdinalIgnoreCase)] : baseline;
        }
        catch
        {
            return baseline;
        }
    }

    public static async Task<IReadOnlyList<CatalogRoomViewDto>> TryLoadLiveRoomsReadOnlyAsync(AppFacade? app, CancellationToken ct = default)
    {
        var baseline = GetRooms().ToList();
        if (app?.Session is null) return baseline;

        try
        {
            var rows = await app.Administration.QueryGridAsync("PhongChup", null, app.Session, ct);
            if (rows.Count == 0) return baseline;

            var byCode = baseline.ToDictionary(x => x.MaPhong, StringComparer.OrdinalIgnoreCase);
            var merged = new List<CatalogRoomViewDto>();
            foreach (var r in rows)
            {
                var code = GetCell(r, "Mã", "MaPhong");
                var name = GetCell(r, "Tên phòng", "TenPhong");
                var desc = GetCell(r, "Mô tả", "MoTa");
                if (string.IsNullOrWhiteSpace(code)) continue;

                byCode.TryGetValue(code, out var seedItem);
                merged.Add(new CatalogRoomViewDto(
                    MaPhong: code,
                    TenPhong: string.IsNullOrWhiteSpace(name) ? seedItem?.TenPhong ?? code : name,
                    MoTa: string.IsNullOrWhiteSpace(desc) ? seedItem?.MoTa ?? "Không gian studio đọc từ SQL Server" : desc,
                    StyleSummary: seedItem?.StyleSummary ?? "Không gian studio đọc trực tiếp từ SQL Server (chưa có tóm tắt thiết kế riêng).",
                    Notes: seedItem?.Notes ?? "Phân bổ phòng chính thức phụ thuộc lịch điều phối thực tế của nhân sự studio.",
                    CoverAssetId: $"DEF-{code}",
                    Gallery: VisualAssetService.GetGallery(code),
                    FromLiveSql: true));
            }
            return merged.Count > 0 ? [.. merged.OrderBy(x => x.MaPhong, StringComparer.OrdinalIgnoreCase)] : baseline;
        }
        catch
        {
            return baseline;
        }
    }

    public static async Task<IReadOnlyList<CatalogResourceViewDto>> TryLoadLiveResourcesReadOnlyAsync(AppFacade? app, CancellationToken ct = default)
    {
        var baseline = GetResources().ToList();
        if (app?.Session is null) return baseline;

        try
        {
            var rows = await app.Administration.QueryGridAsync("TaiNguyen", null, app.Session, ct);
            if (rows.Count == 0) return baseline;

            var byCode = baseline.ToDictionary(x => x.MaTaiNguyen, StringComparer.OrdinalIgnoreCase);
            var merged = new List<CatalogResourceViewDto>();
            foreach (var r in rows)
            {
                var code = GetCell(r, "Mã", "MaTaiNguyen");
                var name = GetCell(r, "Tên tài nguyên", "TenTaiNguyen");
                var rawType = GetCell(r, "Loại", "LoaiTaiNguyen");
                var qty = int.TryParse(GetCell(r, "Số lượng", "TongSoLuong", "SoLuong"), out var q) ? q : 1;
                var svc = GetCell(r, "Dịch vụ thuê", "MaDichVuThue");
                var note = GetCell(r, "Ghi chú", "GhiChu");
                if (string.IsNullOrWhiteSpace(code)) continue;

                byCode.TryGetValue(code, out var seedItem);
                var normalizedType = string.Equals(rawType, "TRANG_PHUC", StringComparison.OrdinalIgnoreCase) || rawType.Contains("phục", StringComparison.OrdinalIgnoreCase)
                    ? "TRANG_PHUC"
                    : "THIET_BI";
                var resolvedSvc = !string.IsNullOrWhiteSpace(svc) ? svc : seedItem?.MaDichVuThue;
                var resolvedNote = !string.IsNullOrWhiteSpace(note)
                    ? note
                    : seedItem?.IllustrativeNote ?? "Tài nguyên đọc từ SQL Server; tình trạng khả dụng thực tế do nhân sự kiểm tra theo khung giờ.";

                merged.Add(new CatalogResourceViewDto(
                    MaTaiNguyen: code,
                    TenTaiNguyen: string.IsNullOrWhiteSpace(name) ? seedItem?.TenTaiNguyen ?? code : name,
                    LoaiTaiNguyen: normalizedType,
                    SoLuong: Math.Max(0, qty),
                    MaDichVuThue: resolvedSvc,
                    IllustrativeNote: resolvedNote,
                    CoverAssetId: $"DEF-{code}",
                    Gallery: VisualAssetService.GetGallery(code),
                    FromLiveSql: true));
            }
            return merged.Count > 0 ? [.. merged.OrderBy(x => x.MaTaiNguyen, StringComparer.OrdinalIgnoreCase)] : baseline;
        }
        catch
        {
            return baseline;
        }
    }

    private static string GetCell(IDictionary<string, object?> row, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (row.TryGetValue(k, out var val) && val is not null)
            {
                var s = Convert.ToString(val)?.Trim();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return "";
    }

    private static CatalogResourceViewDto BuildResource(
        string code, string name, string type, int qty, string? rentService, string note) =>
        new(code, name, type, qty, rentService, note, $"DEF-{code}", VisualAssetService.GetGallery(code), false);
}
