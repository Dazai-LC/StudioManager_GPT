using System.Drawing.Drawing2D;
using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Simulation;

/// <summary>
/// Centralized offline-first asset loader, thumbnail cache, and Admin Media Manager sandbox store.
/// Distinguishes honestly between real generated photography and built-in graphic fallback artwork.
/// Never writes back to SQL Server or overwrites repository baseline files.
/// </summary>
public static class VisualAssetService
{
    private static readonly Dictionary<string, List<VisualAssetEntryDto>> GalleryByTarget = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Bitmap> CachedOriginals = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Bitmap> CachedThumbnails = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object SyncLock = new();
    private static bool _initialized;

    // All 18 baseline seed catalog targets + 3 named live SQL records (P5A94A4, TNA1DC12, TN501515) plus 2 Auth backgrounds are verified high-resolution photography on disk.
    private static readonly HashSet<string> RealAiPhotographyTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "GC000001", "GC000002", "GC000003", "GC000004", "GC000005",
        "P0001", "P0002", "P0003", "P5A94A4",
        "TN000001", "TN000002", "TN000003", "TN000004", "TN000005",
        "TN000006", "TN000007", "TN000008", "TN000009", "TN000010",
        "TNA1DC12", "TN501515"
    };

    public static void EnsureInitialized()
    {
        lock (SyncLock)
        {
            if (_initialized) return;
            InitializeDefaultsLocked();
            _initialized = true;
        }
    }

    public static void ResetToBaseline()
    {
        lock (SyncLock)
        {
            DisposeCachedBitmapsLocked();
            GalleryByTarget.Clear();
            InitializeDefaultsLocked();
            _initialized = true;
        }
    }

    public static IReadOnlyList<VisualAssetEntryDto> GetGallery(string targetCode)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            if (GalleryByTarget.TryGetValue(targetCode, out var list))
                return list.OrderByDescending(x => x.IsPrimaryCover).ThenBy(x => x.SortOrder).ToList();
        }
        return [CreateFallbackEntry(targetCode)];
    }

    public static VisualAssetEntryDto GetCoverEntry(string targetCode)
    {
        var list = GetGallery(targetCode);
        return list.FirstOrDefault(x => x.IsPrimaryCover) ?? list[0];
    }

    public static Image GetOrLoadImage(VisualAssetEntryDto entry, int maxWidth = 720)
    {
        EnsureInitialized();
        var cacheKey = $"{entry.AssetId}::{maxWidth}";
        lock (SyncLock)
        {
            if (CachedThumbnails.TryGetValue(cacheKey, out var cached))
                return cached;

            var sourcePath = ResolveAssetFilePath(entry.ExpectedRelativePath, entry.ResolvedFullPath);
            if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
            {
                try
                {
                    using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var raw = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
                    var thumb = ResizeProportional(raw, maxWidth);
                    CachedThumbnails[cacheKey] = thumb;
                    return thumb;
                }
                catch
                {
                    // Fall through to synthesized safe studio plate
                }
            }

            var fallbackBmp = CreateRuntimeFallbackBitmap(entry.TargetCode, entry.Title, maxWidth, (int)(maxWidth * 0.67f));
            CachedThumbnails[cacheKey] = fallbackBmp;
            return fallbackBmp;
        }
    }

    public static Bitmap CreateDetachedFullResolutionBitmap(VisualAssetEntryDto entry)
    {
        var sourcePath = ResolveAssetFilePath(entry.ExpectedRelativePath, entry.ResolvedFullPath);
        if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            try
            {
                using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var raw = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
                return new Bitmap(raw);
            }
            catch
            {
                // Fall through to fallback bitmap
            }
        }
        return CreateRuntimeFallbackBitmap(entry.TargetCode, entry.Title, 1200, 800);
    }

    public static VisualAssetEntryDto AddAdminDemoUpload(
        MediaTargetEntityKind entityKind,
        string targetCode,
        string sourceFilePath,
        string title,
        string caption,
        string altText,
        bool setAsPrimaryCover,
        string adminDisplayName)
    {
        EnsureInitialized();
        var copiedFileInfo = new FileInfo(sourceFilePath);
        var (w, h) = ReadImageDimensions(sourceFilePath);

        lock (SyncLock)
        {
            if (!GalleryByTarget.TryGetValue(targetCode, out var list))
            {
                list = [];
                GalleryByTarget[targetCode] = list;
            }

            if (setAsPrimaryCover)
            {
                for (var i = 0; i < list.Count; i++)
                    list[i] = list[i] with { IsPrimaryCover = false };
            }

            var newEntry = new VisualAssetEntryDto(
                AssetId: $"ADM-{targetCode}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                TargetEntityKind: entityKind,
                TargetCode: targetCode,
                Title: string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(sourceFilePath) : title.Trim(),
                Caption: string.IsNullOrWhiteSpace(caption) ? "Ảnh quản trị tải lên trong phiên (Demo cục bộ)" : caption.Trim(),
                AltText: string.IsNullOrWhiteSpace(altText) ? title.Trim() : altText.Trim(),
                ExpectedRelativePath: sourceFilePath,
                ResolvedFullPath: sourceFilePath,
                IsPrimaryCover: setAsPrimaryCover || list.Count == 0,
                IsIllustrative: false,
                SourceKind: MediaSourceKind.AdminUploadPreview,
                SortOrder: list.Count == 0 ? 1 : list.Max(x => x.SortOrder) + 1,
                FileSizeBytes: copiedFileInfo.Exists ? copiedFileInfo.Length : 0,
                PixelWidth: w,
                PixelHeight: h,
                UploadedByDisplay: adminDisplayName,
                UpdatedAtLabel: DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            list.Add(newEntry);
            return newEntry;
        }
    }

    public static void SetPrimaryCover(string targetCode, string assetId)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            if (!GalleryByTarget.TryGetValue(targetCode, out var list)) return;
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                list[i] = item with { IsPrimaryCover = string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase) };
            }
        }
    }

    public static void UpdateMetadata(string targetCode, string assetId, string title, string caption, string altText, bool isIllustrative)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            if (!GalleryByTarget.TryGetValue(targetCode, out var list)) return;
            for (var i = 0; i < list.Count; i++)
            {
                if (!string.Equals(list[i].AssetId, assetId, StringComparison.OrdinalIgnoreCase)) continue;
                list[i] = list[i] with
                {
                    Title = title.Trim(),
                    Caption = caption.Trim(),
                    AltText = altText.Trim(),
                    IsIllustrative = isIllustrative,
                    UpdatedAtLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };
            }
        }
    }

    public static void MoveOrder(string targetCode, string assetId, int delta)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            if (!GalleryByTarget.TryGetValue(targetCode, out var list) || list.Count < 2) return;
            var ordered = list.OrderBy(x => x.SortOrder).ToList();
            var idx = ordered.FindIndex(x => string.Equals(x.AssetId, assetId, StringComparison.OrdinalIgnoreCase));
            var targetIdx = idx + delta;
            if (idx < 0 || targetIdx < 0 || targetIdx >= ordered.Count) return;

            (ordered[idx], ordered[targetIdx]) = (ordered[targetIdx], ordered[idx]);
            for (var i = 0; i < ordered.Count; i++)
                ordered[i] = ordered[i] with { SortOrder = i + 1 };

            GalleryByTarget[targetCode] = ordered;
        }
    }

    public static void RemoveAsset(string targetCode, string assetId)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            if (!GalleryByTarget.TryGetValue(targetCode, out var list) || list.Count <= 1) return;
            list.RemoveAll(x => string.Equals(x.AssetId, assetId, StringComparison.OrdinalIgnoreCase));
            if (list.Count > 0 && !list.Any(x => x.IsPrimaryCover))
                list[0] = list[0] with { IsPrimaryCover = true };
        }
    }

    public static void ResetTargetToDefault(string targetCode)
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            var allDefaults = BuildBaselineEntries().Where(x => string.Equals(x.TargetCode, targetCode, StringComparison.OrdinalIgnoreCase)).ToList();
            if (allDefaults.Count > 0)
                GalleryByTarget[targetCode] = allDefaults;
        }
    }

    public static IReadOnlyList<VisualAssetEntryDto> GetInventorySnapshot()
    {
        EnsureInitialized();
        lock (SyncLock)
        {
            return GalleryByTarget.Values.SelectMany(x => x).ToList();
        }
    }

    private static void InitializeDefaultsLocked()
    {
        foreach (var entry in BuildBaselineEntries())
        {
            if (!GalleryByTarget.TryGetValue(entry.TargetCode, out var list))
            {
                list = [];
                GalleryByTarget[entry.TargetCode] = list;
            }
            list.Add(entry);
        }
    }

    private static List<VisualAssetEntryDto> BuildBaselineEntries()
    {
        var entries = new List<VisualAssetEntryDto>();

        void AddDefault(MediaTargetEntityKind kind, string code, string subFolder, string fileName, string title, string caption, string altText)
        {
            var rel = Path.Combine("Assets", "MediaDefaults", subFolder, fileName);
            var full = ResolveAssetFilePath(rel, null);
            var isRealPhoto = RealAiPhotographyTargets.Contains(code);
            var (w, h) = !string.IsNullOrWhiteSpace(full) ? ReadImageDimensions(full) : (1200, 800);
            var size = !string.IsNullOrWhiteSpace(full) && File.Exists(full) ? new FileInfo(full).Length : 0L;

            entries.Add(new VisualAssetEntryDto(
                AssetId: $"DEF-{code}",
                TargetEntityKind: kind,
                TargetCode: code,
                Title: title,
                Caption: caption,
                AltText: altText,
                ExpectedRelativePath: rel,
                ResolvedFullPath: full,
                IsPrimaryCover: true,
                IsIllustrative: true,
                SourceKind: isRealPhoto ? MediaSourceKind.LocalAiDefault : MediaSourceKind.SystemFallback,
                SortOrder: 1,
                FileSizeBytes: size,
                PixelWidth: w,
                PixelHeight: h,
                UploadedByDisplay: isRealPhoto ? "Hệ thống IMG-02 (Ảnh AI cục bộ)" : "Hệ thống (Đồ họa minh họa dự phòng)",
                UpdatedAtLabel: "Baseline v2.2"));
        }

        // Packages GC000001..05
        AddDefault(MediaTargetEntityKind.Package, "GC000001", "packages", "pkg_GC000001_cover_ai.jpg",
            "Gói Chân dung cá nhân", "Concept chân dung studio chuyên sâu với ánh sáng nghệ thuật", "Ảnh minh họa gói Chân dung cá nhân");
        AddDefault(MediaTargetEntityKind.Package, "GC000002", "packages", "pkg_GC000002_cover_ai.jpg",
            "Gói Gia đình", "Không gian gắn kết gia đình tối đa 6 thành viên", "Ảnh minh họa gói Gia đình");
        AddDefault(MediaTargetEntityKind.Package, "GC000003", "packages", "pkg_GC000003_cover_ai.jpg",
            "Gói Em bé", "Bối cảnh dịu êm và đạo cụ an toàn cho bé", "Ảnh minh họa gói Em bé");
        AddDefault(MediaTargetEntityKind.Package, "GC000004", "packages", "pkg_GC000004_cover_ai.jpg",
            "Gói Cặp đôi", "Hai concept lãng mạn trong không gian studio ấm áp", "Ảnh minh họa gói Cặp đôi");
        AddDefault(MediaTargetEntityKind.Package, "GC000005", "packages", "pkg_GC000005_cover_ai.jpg",
            "Gói Kỷ yếu", "Gói chụp nhóm ngoại cảnh 300 phút ánh sáng tự nhiên", "Ảnh minh họa gói Kỷ yếu");

        // Rooms P0001..03 + Live SQL Room P5A94A4
        AddDefault(MediaTargetEntityKind.Room, "P0001", "rooms", "room_P0001_cover_ai.jpg",
            "Phòng Ánh Sáng", "Phong cách tối giản, tận dụng nguồn sáng tự nhiên cửa vòm", "Ảnh minh họa Phòng Ánh Sáng");
        AddDefault(MediaTargetEntityKind.Room, "P0002", "rooms", "room_P0002_cover_ai.jpg",
            "Phòng Cổ Điển", "Nội thất vintage gỗ óc chó và gam màu ấm cổ điển", "Ảnh minh họa Phòng Cổ Điển");
        AddDefault(MediaTargetEntityKind.Room, "P0003", "rooms", "room_P0003_cover_ai.jpg",
            "Phòng Em Bé", "An toàn, nhiều đạo cụ nhẹ nhàng cho trẻ nhỏ", "Ảnh minh họa Phòng Em Bé");
        AddDefault(MediaTargetEntityKind.Room, "P5A94A4", "rooms", "room_P5A94A4_cover_ai.jpg",
            "Phòng 4D", "Không gian ánh sáng đa chiều & hiệu ứng trình chiếu nghệ thuật (Live SQL)", "Ảnh minh họa Phòng 4D");

        // Resources TN000001..10 + Live SQL Equipment TNA1DC12, TN501515
        AddDefault(MediaTargetEntityKind.Resource, "TN000001", "resources", "res_TN000001_cover_ai.jpg",
            "Canon R6 Mark II", "Thân máy mirrorless full-frame phục vụ tác nghiệp studio", "Ảnh minh họa Canon R6 Mark II");
        AddDefault(MediaTargetEntityKind.Resource, "TN000002", "resources", "res_TN000002_cover_ai.jpg",
            "Sony A7 IV", "Thân máy full-frame đa dụng tại studio", "Ảnh minh họa Sony A7 IV");
        AddDefault(MediaTargetEntityKind.Resource, "TN000003", "resources", "res_TN000003_cover_ai.jpg",
            "Đèn Godox AD600", "Đèn flash công suất cao kèm softbox tản sáng", "Ảnh minh họa đèn Godox AD600");
        AddDefault(MediaTargetEntityKind.Resource, "TN000004", "resources", "res_TN000004_cover_ai.jpg",
            "Đèn LED RGB", "Hệ thống đèn LED bảng và tuýp RGB tạo không khí studio", "Ảnh minh họa đèn LED RGB");
        AddDefault(MediaTargetEntityKind.Resource, "TN000005", "resources", "res_TN000005_cover_ai.jpg",
            "Phông giấy trắng", "Phông nền giấy trắng vô cực cho concept tối giản", "Ảnh minh họa phông giấy trắng");
        AddDefault(MediaTargetEntityKind.Resource, "TN000006", "resources", "res_TN000006_cover_ai.jpg",
            "Váy cưới trắng", "Trang phục váy cưới trưng bày tại phòng thử đồ studio", "Ảnh minh họa Váy cưới trắng");
        AddDefault(MediaTargetEntityKind.Resource, "TN000007", "resources", "res_TN000007_cover_ai.jpg",
            "Áo dài đỏ", "Trang phục áo dài truyền thống chất liệu lụa đỏ", "Ảnh minh họa Áo dài đỏ");
        AddDefault(MediaTargetEntityKind.Resource, "TN000008", "resources", "res_TN000008_cover_ai.jpg",
            "Vest nam", "Trang phục vest lịch lãm cho concept cặp đôi và gia đình", "Ảnh minh họa Vest nam");
        AddDefault(MediaTargetEntityKind.Resource, "TN000009", "resources", "res_TN000009_cover_ai.jpg",
            "Váy công chúa bé", "Trang phục bé gái phục vụ concept sinh nhật và em bé", "Ảnh minh họa Váy công chúa bé");
        AddDefault(MediaTargetEntityKind.Resource, "TN000010", "resources", "res_TN000010_cover_ai.jpg",
            "Đạo cụ sinh nhật", "Bộ đạo cụ trang trí buổi chụp sinh nhật tông ấm tối giản", "Ảnh minh họa Đạo cụ sinh nhật");
        AddDefault(MediaTargetEntityKind.Resource, "TNA1DC12", "resources", "res_TNA1DC12_cover_ai.jpg",
            "Đèn trung thu", "Đạo cụ lồng đèn trung thu thủ công phục vụ concept lễ hội (Live SQL)", "Ảnh minh họa Đèn trung thu");
        AddDefault(MediaTargetEntityKind.Resource, "TN501515", "resources", "res_TN501515_cover_ai.jpg",
            "Micro", "Micro thu âm studio kèm shockmount và lọc âm cho quay/livestream (Live SQL)", "Ảnh minh họa Micro studio");

        return entries;
    }

    private static VisualAssetEntryDto CreateFallbackEntry(string code)
    {
        var kind = code.StartsWith("P", StringComparison.OrdinalIgnoreCase)
            ? MediaTargetEntityKind.Room
            : code.StartsWith("TN", StringComparison.OrdinalIgnoreCase)
                ? MediaTargetEntityKind.Resource
                : MediaTargetEntityKind.Package;
        var rel = Path.Combine("Assets", "MediaDefaults", "fallbacks", "fallback_catalog_cover.jpg");
        var full = ResolveAssetFilePath(rel, null);
        var (w, h) = !string.IsNullOrWhiteSpace(full) ? ReadImageDimensions(full) : (1200, 800);
        var size = !string.IsNullOrWhiteSpace(full) && File.Exists(full) ? new FileInfo(full).Length : 0L;
        return new VisualAssetEntryDto(
            $"FB-{code}", kind, code, $"Hình thay thế — {code}",
            $"Chưa có ảnh chụp riêng cho mã {code} trong thư viện cục bộ; hệ thống hiển thị ảnh thay thế an toàn thay cho ảnh thật.",
            $"Hình thay thế hệ thống cho mã {code}", rel, full,
            true, true, MediaSourceKind.SystemFallback, 1, size, w, h, "Hệ thống (Ảnh thay thế mặc định)", "Chờ bổ sung ảnh riêng");
    }

    public static string? ResolveAssetFilePath(string? expectedRelativePath, string? existingFullPath)
    {
        if (!string.IsNullOrWhiteSpace(existingFullPath) && File.Exists(existingFullPath))
            return existingFullPath;

        if (string.IsNullOrWhiteSpace(expectedRelativePath))
            return null;

        if (Path.IsPathRooted(expectedRelativePath) && File.Exists(expectedRelativePath))
            return expectedRelativePath;

        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, expectedRelativePath),
            Path.Combine(System.Windows.Forms.Application.StartupPath, expectedRelativePath)
        };

        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && cursor is not null; i++)
        {
            candidates.Add(Path.Combine(cursor.FullName, expectedRelativePath));
            candidates.Add(Path.Combine(cursor.FullName, "src", "StudioManager.WinForms", expectedRelativePath));
            cursor = cursor.Parent;
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Bitmap ResizeProportional(Image source, int maxWidth)
    {
        if (source.Width <= maxWidth || maxWidth <= 0)
            return new Bitmap(source);

        var ratio = (double)maxWidth / source.Width;
        var targetW = maxWidth;
        var targetH = Math.Max(1, (int)Math.Round(source.Height * ratio));

        var bmp = new Bitmap(targetW, targetH);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, new Rectangle(0, 0, targetW, targetH));
        return bmp;
    }

    private static (int Width, int Height) ReadImageDimensions(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            return (img.Width, img.Height);
        }
        catch
        {
            return (1200, 800);
        }
    }

    private static Bitmap CreateRuntimeFallbackBitmap(string code, string title, int width, int height)
    {
        var bmp = new Bitmap(Math.Max(200, width), Math.Max(150, height));
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(
            new RectangleF(0, 0, bmp.Width, bmp.Height),
            Theme.Primitives.Alabaster50,
            Theme.Primitives.Alabaster200,
            40f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);
        using var pen = new Pen(Color.FromArgb(64, Theme.Primary), 2f);
        g.DrawRectangle(pen, 18, 18, bmp.Width - 36, bmp.Height - 36);
        using var fCode = Theme.MonoFont(10f, FontStyle.Bold);
        using var fTitle = Theme.Font(13f, FontStyle.Bold);
        using var brAccent = new SolidBrush(Theme.Primary);
        using var brText = new SolidBrush(Theme.Text);
        g.DrawString(code, fCode, brAccent, 28, bmp.Height - 66);
        g.DrawString(title, fTitle, brText, 28, bmp.Height - 44);
        return bmp;
    }

    private static void DisposeCachedBitmapsLocked()
    {
        foreach (var bmp in CachedOriginals.Values) bmp.Dispose();
        foreach (var bmp in CachedThumbnails.Values) bmp.Dispose();
        CachedOriginals.Clear();
        CachedThumbnails.Clear();
    }
}

