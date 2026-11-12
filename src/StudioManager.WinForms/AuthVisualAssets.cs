using System.Drawing.Drawing2D;

namespace StudioManager.WinForms;

public sealed record BackgroundAssetLoadResult(
    string AssetId,
    Image? Image,
    string? ResolvedPath,
    string ExpectedRelativePath,
    bool IsFallbackArtwork,
    string? DiagnosticMessage);

public static class AuthVisualAssets
{
    public const string LoginBackgroundAssetId = "BG-AUTH-01";
    public const string CustomerRegistrationBackgroundAssetId = "BG-AUTH-03";

    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".bmp"];

    public static string GetExpectedBaseName(string assetId) => assetId switch
    {
        LoginBackgroundAssetId => "bg_auth_desktop_login",
        CustomerRegistrationBackgroundAssetId => "bg_auth_portal_register",
        _ => assetId.ToLowerInvariant()
    };

    public static string GetExpectedRelativePath(string assetId) =>
        Path.Combine("Assets", "Backgrounds", $"{GetExpectedBaseName(assetId)}.jpg");

    public static BackgroundAssetLoadResult LoadBackground(string assetId)
    {
        var baseName = GetExpectedBaseName(assetId);
        var expectedRelative = GetExpectedRelativePath(assetId);

        foreach (var candidatePath in EnumerateCandidatePaths(baseName))
        {
            if (!File.Exists(candidatePath))
                continue;

            try
            {
                using var stream = new FileStream(candidatePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var loaded = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
                var detachedBitmap = new Bitmap(loaded);
                return new BackgroundAssetLoadResult(
                    assetId,
                    detachedBitmap,
                    candidatePath,
                    expectedRelative,
                    IsFallbackArtwork: false,
                    DiagnosticMessage: null);
            }
            catch (Exception ex)
            {
                return new BackgroundAssetLoadResult(
                    assetId,
                    null,
                    candidatePath,
                    expectedRelative,
                    IsFallbackArtwork: true,
                    DiagnosticMessage: $"Failed to decode '{candidatePath}': {ex.Message}");
            }
        }

        return new BackgroundAssetLoadResult(
            assetId,
            null,
            null,
            expectedRelative,
            IsFallbackArtwork: true,
            DiagnosticMessage: $"Local asset missing ({expectedRelative}); rendering built-in studio artwork fallback.");
    }

    public static void DrawImageCover(Graphics g, Image image, RectangleF targetBounds)
    {
        if (image.Width <= 0 || image.Height <= 0 || targetBounds.Width <= 0 || targetBounds.Height <= 0)
            return;

        var scale = Math.Max(targetBounds.Width / image.Width, targetBounds.Height / image.Height);
        var drawWidth = image.Width * scale;
        var drawHeight = image.Height * scale;
        var drawX = targetBounds.Left + (targetBounds.Width - drawWidth) / 2f;
        var drawY = targetBounds.Top + (targetBounds.Height - drawHeight) / 2f;

        var state = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(image, new RectangleF(drawX, drawY, drawWidth, drawHeight));
        g.Restore(state);
    }

    public static void DrawScrimOverlay(
        Graphics g,
        RectangleF targetBounds,
        Color topColor,
        Color bottomColor,
        Color sideTintColor)
    {
        if (targetBounds.Width <= 1 || targetBounds.Height <= 1)
            return;

        using var verticalScrim = new LinearGradientBrush(
            targetBounds,
            topColor,
            bottomColor,
            LinearGradientMode.Vertical);
        g.FillRectangle(verticalScrim, targetBounds);

        using var horizontalScrim = new LinearGradientBrush(
            targetBounds,
            sideTintColor,
            Color.FromArgb(0, sideTintColor),
            LinearGradientMode.Horizontal);
        g.FillRectangle(horizontalScrim, targetBounds);
    }

    private static IEnumerable<string> EnumerateCandidatePaths(string baseName)
    {
        var roots = new List<string>();
        AddIfDistinct(roots, Path.Combine(AppContext.BaseDirectory, "Assets", "Backgrounds"));
        AddIfDistinct(roots, Path.Combine(System.Windows.Forms.Application.StartupPath, "Assets", "Backgrounds"));

        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 6 && cursor is not null; depth++)
        {
            AddIfDistinct(roots, Path.Combine(cursor.FullName, "Assets", "Backgrounds"));
            AddIfDistinct(roots, Path.Combine(cursor.FullName, "src", "StudioManager.WinForms", "Assets", "Backgrounds"));
            AddIfDistinct(roots, Path.Combine(cursor.FullName, "assets-source", "visual-system", "backgrounds"));
            cursor = cursor.Parent;
        }

        foreach (var root in roots)
        {
            foreach (var ext in SupportedExtensions)
                yield return Path.Combine(root, baseName + ext);
        }
    }

    private static void AddIfDistinct(List<string> list, string path)
    {
        if (!list.Contains(path, StringComparer.OrdinalIgnoreCase))
            list.Add(path);
    }
}

internal enum StudioBackdropMood
{
    WorkstationOptics,
    WarmEditorial
}

/// <summary>
/// Shared photographic showcase surface for authentication screens (BG-AUTH-01 &amp; BG-AUTH-03).
/// Safely loads local raster backgrounds with proportional aspect-fill cropping, atmospheric scrims,
/// deterministic Bitmap disposal, and GDI+ studio artwork fallback when local assets are absent.
/// </summary>
internal sealed class AuthShowcasePanel : Panel
{
    private readonly StudioBackdropMood _mood;
    private readonly BackgroundAssetLoadResult _asset;

    public BackgroundAssetLoadResult AssetStatus => _asset;

    public AuthShowcasePanel(string assetId, StudioBackdropMood mood)
    {
        _mood = mood;
        _asset = AuthVisualAssets.LoadBackground(assetId);

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 12f);

        var gradEnd = _mood == StudioBackdropMood.WarmEditorial
            ? Color.FromArgb(44, 34, 28)
            : Theme.Primitives.Espresso800;

        using (var bg = new LinearGradientBrush(ClientRectangle,
            Theme.Primitives.Espresso900,
            gradEnd,
            _mood == StudioBackdropMood.WarmEditorial ? 50f : 55f))
        {
            g.FillPath(bg, path);
        }

        var state = g.Save();
        g.SetClip(path);

        if (_asset.Image is { } photo)
        {
            AuthVisualAssets.DrawImageCover(g, photo, bounds);

            // Directional dark scrim preserves AA/AAA typography legibility over arbitrary studio photos.
            var topScrim = Color.FromArgb(185, 22, 20, 18);
            var bottomScrim = _mood == StudioBackdropMood.WarmEditorial
                ? Color.FromArgb(224, 34, 26, 21)
                : Color.FromArgb(232, 20, 18, 16);
            var sideScrim = Color.FromArgb(150, 22, 20, 18);

            AuthVisualAssets.DrawScrimOverlay(g, bounds, topScrim, bottomScrim, sideScrim);
            DrawOpticalRings(g, subtleOverlay: true);
        }
        else
        {
            DrawFallbackStudioArtwork(g);
        }

        g.Restore(state);

        using var borderPen = new Pen(Color.FromArgb(60, Theme.Primitives.Stone400), 1f);
        g.DrawPath(borderPen, path);
    }

    private void DrawFallbackStudioArtwork(Graphics g)
    {
        if (_mood == StudioBackdropMood.WorkstationOptics)
        {
            // Key rim light in warm terracotta + dark floor vignette
            using (var glow = new SolidBrush(Color.FromArgb(28, Theme.Primary)))
                g.FillEllipse(glow, Width - 220, -80, 320, 320);

            using (var rimGlow = new SolidBrush(Color.FromArgb(14, Theme.Primitives.Ochre600)))
                g.FillEllipse(rimGlow, -70, Height - 240, 260, 260);

            DrawOpticalRings(g, subtleOverlay: false);
            DrawViewfinderFrame(g);
        }
        else
        {
            // Warm terracotta softbox top-right + botanical sage & ochre bounce bottom-left
            using (var glow = new SolidBrush(Color.FromArgb(24, Theme.Primary)))
                g.FillEllipse(glow, Width - 240, -60, 340, 340);

            using (var sageGlow = new SolidBrush(Color.FromArgb(16, Theme.Primitives.Sage600)))
                g.FillEllipse(sageGlow, -60, Height - 220, 270, 270);

            using (var ochreGlow = new SolidBrush(Color.FromArgb(12, Theme.Primitives.Ochre600)))
                g.FillEllipse(ochreGlow, Width / 3f, Height - 190, 220, 220);

            DrawOpticalRings(g, subtleOverlay: false);
        }
    }

    private void DrawOpticalRings(Graphics g, bool subtleOverlay)
    {
        var cx = _mood == StudioBackdropMood.WarmEditorial ? Width - 70f : Width - 60f;
        var cy = _mood == StudioBackdropMood.WarmEditorial ? 110f : 90f;
        var ringAlpha = subtleOverlay ? 14 : 22;
        var dashAlpha = subtleOverlay ? 18 : 30;

        using var ringPen = new Pen(Color.FromArgb(ringAlpha, Theme.Primitives.Alabaster200), 1f);
        using var dashedPen = new Pen(Color.FromArgb(dashAlpha, Theme.Primary), 1f) { DashStyle = DashStyle.Dot };

        if (_mood == StudioBackdropMood.WarmEditorial)
        {
            g.DrawEllipse(ringPen, cx - 60, cy - 60, 120, 120);
            g.DrawEllipse(dashedPen, cx - 100, cy - 100, 200, 200);
            g.DrawEllipse(ringPen, cx - 150, cy - 150, 300, 300);
            g.DrawLine(ringPen, cx - 170, cy, cx + 170, cy);
            g.DrawLine(ringPen, cx, cy - 170, cx, cy + 170);
        }
        else
        {
            g.DrawEllipse(ringPen, cx - 70, cy - 70, 140, 140);
            g.DrawEllipse(dashedPen, cx - 115, cy - 115, 230, 230);
            g.DrawEllipse(ringPen, cx - 165, cy - 165, 330, 330);
            g.DrawLine(ringPen, cx - 190, cy, cx + 190, cy);
            g.DrawLine(ringPen, cx, cy - 190, cx, cy + 190);
        }
    }

    private void DrawViewfinderFrame(Graphics g)
    {
        const float inset = 18f;
        const float arm = 18f;
        var right = Width - inset;
        var bottom = Height - inset;
        using var framePen = new Pen(Color.FromArgb(26, Theme.Primitives.Stone400), 1.2f);

        // Top-right & bottom-right camera viewfinder corner brackets
        g.DrawLines(framePen, [new PointF(right - arm, inset), new PointF(right, inset), new PointF(right, inset + arm)]);
        g.DrawLines(framePen, [new PointF(right - arm, bottom), new PointF(right, bottom), new PointF(right, bottom - arm)]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _asset.Image?.Dispose();

        base.Dispose(disposing);
    }
}

/// <summary>
/// Full-canvas photographic surface painted behind elevated authentication cards
/// across the entire window client area (BG-AUTH-01 &amp; BG-AUTH-03).
/// </summary>
internal sealed class FullCanvasAuthSurface : Panel
{
    private readonly StudioBackdropMood _mood;
    private readonly BackgroundAssetLoadResult _asset;

    public BackgroundAssetLoadResult AssetStatus => _asset;

    public FullCanvasAuthSurface(string assetId, StudioBackdropMood mood)
    {
        _mood = mood;
        _asset = AuthVisualAssets.LoadBackground(assetId);

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Primitives.Espresso900;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height));

        using (var baseBrush = new LinearGradientBrush(
            bounds,
            Theme.Primitives.Espresso900,
            _mood == StudioBackdropMood.WarmEditorial ? Color.FromArgb(46, 36, 30) : Theme.Primitives.Espresso800,
            35f))
        {
            g.FillRectangle(baseBrush, bounds);
        }

        if (_asset.Image is { } photo)
        {
            AuthVisualAssets.DrawImageCover(g, photo, bounds);

            // Calibrated full-canvas horizontal luminance scrim:
            // Stronger protective dusk/espresso scrim on left (editorial headline over lamps/curtains)
            // tapering to gentle warm glaze on right so the photography frames the elevated frosted auth card.
            using var hScrim = new LinearGradientBrush(bounds, Color.Black, Color.Black, LinearGradientMode.Horizontal);
            var hBlend = new ColorBlend(3);
            if (_mood == StudioBackdropMood.WorkstationOptics)
            {
                hBlend.Colors =
                [
                    Color.FromArgb(196, 18, 16, 14),
                    Color.FromArgb(148, 22, 19, 17),
                    Color.FromArgb(98, 22, 19, 17)
                ];
            }
            else
            {
                hBlend.Colors =
                [
                    Color.FromArgb(198, 34, 26, 20),
                    Color.FromArgb(136, 36, 28, 22),
                    Color.FromArgb(88, 36, 28, 22)
                ];
            }
            hBlend.Positions = [0f, 0.48f, 1f];
            hScrim.InterpolationColors = hBlend;
            g.FillRectangle(hScrim, bounds);

            using var vVignette = new LinearGradientBrush(bounds,
                Color.FromArgb(95, 16, 14, 12),
                Color.FromArgb(145, 16, 14, 12),
                LinearGradientMode.Vertical);
            g.FillRectangle(vVignette, bounds);
        }
        else
        {
            using var rim = new SolidBrush(Color.FromArgb(30, Theme.Primary));
            g.FillEllipse(rim, Width - 380, -110, 480, 480);
            using var warm = new SolidBrush(Color.FromArgb(20, Theme.Primitives.Ochre600));
            g.FillEllipse(warm, -120, Height - 320, 420, 420);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _asset.Image?.Dispose();

        base.Dispose(disposing);
    }
}

/// <summary>
/// Semi-translucent elevated warm-ivory frosted card floating over full-canvas photography.
/// Renders multi-layer soft ambient drop shadow and crisp border while keeping child controls sharp.
/// </summary>
internal sealed class ElevatedFrostedAuthCard : Panel
{
    public Color AccentStripeColor { get; set; } = Theme.Primary;
    public int CornerRadius { get; set; } = 14;
    public int CardOpacityAlpha { get; set; } = 240;

    public ElevatedFrostedAuthCard()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Padding = new Padding(38, 26, 38, 22);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var margin = 6f;
        var cardBounds = new RectangleF(margin, margin, Math.Max(10, Width - margin * 2), Math.Max(10, Height - margin * 2));

        // Ambient elevation shadows
        for (var layer = 4; layer >= 1; layer--)
        {
            var spread = layer * 1.4f;
            var sBounds = new RectangleF(cardBounds.X - spread * 0.4f, cardBounds.Y + spread * 0.8f,
                cardBounds.Width + spread * 0.8f, cardBounds.Height + spread * 0.8f);
            using var sPath = Theme.RoundedRectangle(sBounds, CornerRadius + spread * 0.4f);
            using var sBrush = new SolidBrush(Color.FromArgb(14 + (5 - layer) * 6, 12, 10, 8));
            g.FillPath(sBrush, sPath);
        }

        using var cardPath = Theme.RoundedRectangle(cardBounds, CornerRadius);
        using (var frostedFill = new SolidBrush(Color.FromArgb(Math.Clamp(CardOpacityAlpha, 215, 252), Theme.Primitives.Alabaster50)))
        {
            g.FillPath(frostedFill, cardPath);
        }

        // Top architectural accent bar clipped inside card
        var clipState = g.Save();
        g.SetClip(cardPath);
        using (var stripe = new SolidBrush(AccentStripeColor))
        {
            g.FillRectangle(stripe, cardBounds.X, cardBounds.Y, cardBounds.Width, 4f);
        }
        g.Restore(clipState);

        using var outerBorder = new Pen(Color.FromArgb(165, Theme.Primitives.Stone300), 1.1f);
        g.DrawPath(outerBorder, cardPath);
    }
}

