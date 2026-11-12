using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Controls;

/// <summary>
/// Detailed WinForms lightbox / image inspector supporting pan, zoom (Fit / 100% / +/-),
/// gallery strip navigation, keyboard shortcuts (Esc/Left/Right/+/-), and structured metadata sidebar.
/// Disables gallery Previous/Next navigation cleanly when only one image exists.
/// </summary>
public sealed class ImageViewerForm : Form
{
    private readonly IReadOnlyList<VisualAssetEntryDto> _entries;
    private int _currentIndex;
    private Bitmap? _currentBitmap;
    private bool _loadedRealFile;
    private bool _fitToWindow = true;
    private float _zoom = 1f;
    private PointF _panOffset = PointF.Empty;
    private bool _dragging;
    private Point _dragStart;

    private readonly Panel _canvas = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.Primitives.Espresso900,
        Cursor = Cursors.Hand
    };

    private readonly Label _counterLabel = new()
    {
        AutoSize = true,
        ForeColor = Theme.Primitives.Alabaster100,
        Font = Theme.MonoFont(9.0f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Label _zoomLabel = new()
    {
        AutoSize = true,
        ForeColor = Theme.Primitives.Terracotta200,
        Font = Theme.MonoFont(8.8f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly ModernButton _prevBtn = Theme.OutlineButton("Ảnh trước");
    private readonly ModernButton _nextBtn = Theme.OutlineButton("Ảnh sau");

    private readonly FlowLayoutPanel _thumbStrip = new()
    {
        Dock = DockStyle.Bottom,
        Height = 76,
        BackColor = Theme.Primitives.Espresso800,
        Padding = new Padding(12, 8, 12, 8),
        WrapContents = false,
        AutoScroll = true
    };

    private readonly Label _metaTitle = new() { Dock = DockStyle.Top, Height = 46, Font = Theme.Font(11.2f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly Label _metaBadge = new() { Dock = DockStyle.Top, Height = 26, Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly Panel _metaKvHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 8, 0, 8) };

    public bool IsPrevEnabled => _prevBtn.Enabled;
    public bool IsNextEnabled => _nextBtn.Enabled;

    public ImageViewerForm(IReadOnlyList<VisualAssetEntryDto> entries, int initialIndex = 0)
    {
        _entries = entries.Count > 0 ? entries : VisualAssetService.GetGallery("FALLBACK");
        _currentIndex = Math.Clamp(initialIndex, 0, _entries.Count - 1);

        Text = "Trình xem ảnh chi tiết — Atelier Lumière";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1160, 740);
        MinimumSize = new Size(880, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        KeyPreview = true;
        BackColor = Theme.Background;
        Font = Theme.Font();

        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(_canvas, true);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 336));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Top Dark Toolbar
        var toolbar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Primitives.Espresso900,
            Padding = new Padding(16, 8, 16, 8)
        };
        root.SetColumnSpan(toolbar, 2);

        var leftTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 360,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        _counterLabel.Padding = new Padding(0, 7, 14, 0);
        var hasMulti = _entries.Count > 1;
        _prevBtn.Width = 98;
        _prevBtn.Height = 32;
        _prevBtn.Enabled = hasMulti;
        _nextBtn.Width = 92;
        _nextBtn.Height = 32;
        _nextBtn.Enabled = hasMulti;
        _prevBtn.Click += (_, _) => Navigate(-1);
        _nextBtn.Click += (_, _) => Navigate(1);
        leftTools.Controls.Add(_counterLabel);
        leftTools.Controls.Add(_prevBtn);
        leftTools.Controls.Add(_nextBtn);

        var rightTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 620,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        var closeBtn = Theme.Button("Đóng", Theme.Primary, IconKind.Close);
        closeBtn.Width = 88;
        closeBtn.Height = 32;
        closeBtn.Click += (_, _) => Close();

        var actualBtn = Theme.OutlineButton("100%");
        actualBtn.Width = 68;
        actualBtn.Height = 32;
        actualBtn.Click += (_, _) => SetActualSize();

        var fitBtn = Theme.OutlineButton("Vừa khung");
        fitBtn.Width = 96;
        fitBtn.Height = 32;
        fitBtn.Click += (_, _) => ResetFit();

        var zoomInBtn = Theme.OutlineButton("Phóng to +");
        zoomInBtn.Width = 98;
        zoomInBtn.Height = 32;
        zoomInBtn.Click += (_, _) => AdjustZoom(1.25f);

        var zoomOutBtn = Theme.OutlineButton("Thu nhỏ -");
        zoomOutBtn.Width = 94;
        zoomOutBtn.Height = 32;
        zoomOutBtn.Click += (_, _) => AdjustZoom(0.8f);

        _zoomLabel.Padding = new Padding(8, 8, 8, 0);
        rightTools.Controls.Add(closeBtn);
        rightTools.Controls.Add(fitBtn);
        rightTools.Controls.Add(actualBtn);
        rightTools.Controls.Add(zoomInBtn);
        rightTools.Controls.Add(zoomOutBtn);
        rightTools.Controls.Add(_zoomLabel);

        toolbar.Controls.Add(leftTools);
        toolbar.Controls.Add(rightTools);

        // Left Stage (Canvas + Bottom Thumb Strip)
        var stage = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Primitives.Espresso900 };
        stage.Controls.Add(_canvas);
        stage.Controls.Add(_thumbStrip);
        _thumbStrip.Visible = hasMulti;

        // Right Structured Metadata Sidebar
        var inspector = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(18, 16, 18, 14)
        };
        var sep = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.BorderStrong };
        inspector.Controls.Add(sep);

        var metaHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = "THÔNG TIN HÌNH ẢNH",
            Font = Theme.MonoFont(7.8f, FontStyle.Bold),
            ForeColor = Theme.Muted
        };
        var shortcutHint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 46,
            Text = hasMulti
                ? "Phím tắt: ← / → chuyển ảnh • + / - thu phóng • 0 vừa khung • Kéo chuột di chuyển • Esc đóng"
                : "Phím tắt: + / - thu phóng • 0 vừa khung • Kéo chuột khi phóng to • Esc đóng",
            Font = Theme.Font(7.8f),
            ForeColor = Theme.Muted
        };
        inspector.Controls.Add(_metaKvHost);
        inspector.Controls.Add(shortcutHint);
        inspector.Controls.Add(_metaBadge);
        inspector.Controls.Add(_metaTitle);
        inspector.Controls.Add(metaHeader);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(stage, 0, 1);
        root.Controls.Add(inspector, 1, 1);
        Controls.Add(root);

        BindCanvasEvents();
        BuildThumbStrip();
        LoadEntryAt(_currentIndex);
    }

    private void BindCanvasEvents()
    {
        _canvas.Paint += (_, e) => PaintStage(e.Graphics);
        _canvas.Resize += (_, _) => _canvas.Invalidate();
        _canvas.MouseWheel += (_, e) => AdjustZoom(e.Delta > 0 ? 1.15f : 0.87f);
        _canvas.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragStart = e.Location;
            _canvas.Cursor = Cursors.SizeAll;
        };
        _canvas.MouseMove += (_, e) =>
        {
            if (!_dragging || _fitToWindow) return;
            _panOffset = new PointF(_panOffset.X + (e.X - _dragStart.X), _panOffset.Y + (e.Y - _dragStart.Y));
            _dragStart = e.Location;
            _canvas.Invalidate();
        };
        _canvas.MouseUp += (_, _) =>
        {
            _dragging = false;
            _canvas.Cursor = _fitToWindow ? Cursors.Hand : Cursors.SizeAll;
        };
        _canvas.DoubleClick += (_, _) =>
        {
            if (_fitToWindow) SetActualSize();
            else ResetFit();
        };
        KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.Escape: Close(); break;
                case Keys.Left: if (_entries.Count > 1) Navigate(-1); break;
                case Keys.Right: if (_entries.Count > 1) Navigate(1); break;
                case Keys.Oemplus or Keys.Add: AdjustZoom(1.2f); break;
                case Keys.OemMinus or Keys.Subtract: AdjustZoom(0.82f); break;
                case Keys.D0 or Keys.NumPad0: ResetFit(); break;
            }
        };
    }

    private void BuildThumbStrip()
    {
        _thumbStrip.Controls.Clear();
        for (var i = 0; i < _entries.Count; i++)
        {
            var index = i;
            var entry = _entries[i];
            var thumbBox = new Panel
            {
                Size = new Size(88, 56),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Tag = index
            };
            thumbBox.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var img = VisualAssetService.GetOrLoadImage(entry, 180);
                AuthVisualAssets.DrawImageCover(g, img, new RectangleF(1, 1, thumbBox.Width - 2, thumbBox.Height - 2));
                var borderCol = index == _currentIndex ? Theme.Primary : Theme.Primitives.Stone500;
                using var pen = new Pen(borderCol, index == _currentIndex ? 2.5f : 1f);
                g.DrawRectangle(pen, 1, 1, thumbBox.Width - 3, thumbBox.Height - 3);
            };
            thumbBox.Click += (_, _) => LoadEntryAt(index);
            _thumbStrip.Controls.Add(thumbBox);
        }
    }

    private void Navigate(int delta)
    {
        if (_entries.Count <= 1) return;
        var next = (_currentIndex + delta + _entries.Count) % _entries.Count;
        LoadEntryAt(next);
    }

    private void LoadEntryAt(int index)
    {
        _currentIndex = Math.Clamp(index, 0, _entries.Count - 1);
        _currentBitmap?.Dispose();
        var entry = _entries[_currentIndex];
        var resolved = VisualAssetService.ResolveAssetFilePath(entry.ExpectedRelativePath, entry.ResolvedFullPath);
        _loadedRealFile = !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved);
        _currentBitmap = VisualAssetService.CreateDetachedFullResolutionBitmap(entry);
        ResetFit();

        _counterLabel.Text = _entries.Count == 1 ? "Ảnh 1 / 1 (Đơn lẻ)" : $"Ảnh {_currentIndex + 1} / {_entries.Count}";
        _metaTitle.Text = entry.Title;
        var effectiveBadge = !_loadedRealFile && entry.SourceKind != MediaSourceKind.SystemFallback
            ? "Hình thay thế (Không tìm thấy tệp)"
            : entry.SourceBadgeLabel;
        _metaBadge.Text = $"[{entry.TargetCode}]  •  {effectiveBadge}";
        _metaBadge.ForeColor = _loadedRealFile ? Theme.Primary : Theme.WarningHover;

        RenderStructuredMetadata(entry, effectiveBadge);

        foreach (Control c in _thumbStrip.Controls) c.Invalidate();
    }

    private void RenderStructuredMetadata(VisualAssetEntryDto entry, string effectiveBadge)
    {
        _metaKvHost.Controls.Clear();
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Width = 286,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        void AddSectionCard(string caption, string body, Color? bg = null)
        {
            var card = new CardPanel
            {
                Width = 282,
                AutoSize = true,
                MinimumSize = new Size(282, 54),
                BackColor = bg ?? Theme.Primitives.Alabaster50,
                BorderColor = Theme.Border,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 8)
            };
            var lblCap = new Label
            {
                Dock = DockStyle.Top,
                Height = 16,
                Text = caption.ToUpperInvariant(),
                Font = Theme.MonoFont(7.1f, FontStyle.Bold),
                ForeColor = Theme.Muted
            };
            var lblBody = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(260, 0),
                Text = body,
                Font = Theme.Font(8.5f),
                ForeColor = Theme.TextSecondary,
                Padding = new Padding(0, 3, 0, 0)
            };
            card.Controls.Add(lblBody);
            card.Controls.Add(lblCap);
            stack.Controls.Add(card);
        }

        if (!_loadedRealFile)
        {
            AddSectionCard("Trạng thái tệp ảnh", "Tệp ảnh thực tế chưa có trên máy hoặc bị lỗi đọc; hệ thống đang hiển thị hình thay thế an toàn.", Theme.WarningLight);
        }

        AddSectionCard("Chú thích hình ảnh", entry.Caption);
        AddSectionCard("Mô tả thay thế", entry.AltText);

        var fileLabel = string.IsNullOrWhiteSpace(entry.ResolvedFullPath) ? entry.ExpectedRelativePath : Path.GetFileName(entry.ResolvedFullPath);
        AddTechKvCard("Thuộc tính kỹ thuật", [
            ("Độ phân giải", $"{_currentBitmap?.Width ?? 0} × {_currentBitmap?.Height ?? 0} px"),
            ("Phân loại", effectiveBadge),
            ("Nguồn tạo", entry.UploadedByDisplay),
            ("Tệp cục bộ", string.IsNullOrWhiteSpace(fileLabel) ? "-" : fileLabel),
            ("Tính chất", entry.IsIllustrative ? "Ảnh concept minh họa" : "Ảnh do quản trị gắn (Demo)"),
            ("Cập nhật", entry.UpdatedAtLabel)
        ]);

        void AddTechKvCard(string caption, (string Key, string Value)[] pairs)
        {
            var card = new CardPanel
            {
                Width = 282,
                AutoSize = true,
                MinimumSize = new Size(282, 54),
                BackColor = Theme.Primitives.Alabaster50,
                BorderColor = Theme.Border,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 8)
            };
            var lblCap = new Label
            {
                Dock = DockStyle.Top,
                Height = 16,
                Text = caption.ToUpperInvariant(),
                Font = Theme.MonoFont(7.1f, FontStyle.Bold),
                ForeColor = Theme.Muted
            };
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = pairs.Length,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (var i = 0; i < pairs.Length; i++)
            {
                grid.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = pairs[i].Key + ":",
                    Font = Theme.Font(8.1f, FontStyle.Bold),
                    ForeColor = Theme.Muted,
                    Margin = new Padding(0, 2, 4, 2)
                }, 0, i);
                grid.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    MaximumSize = new Size(174, 0),
                    Text = pairs[i].Value,
                    Font = Theme.Font(8.2f),
                    ForeColor = Theme.TextSecondary,
                    Margin = new Padding(0, 2, 0, 2)
                }, 1, i);
            }
            card.Controls.Add(grid);
            card.Controls.Add(lblCap);
            stack.Controls.Add(card);
        }

        _metaKvHost.Controls.Add(stack);
    }

    private void ResetFit()
    {
        _fitToWindow = true;
        _zoom = 1f;
        _panOffset = PointF.Empty;
        _zoomLabel.Text = "Vừa khung";
        _canvas.Invalidate();
    }

    private void SetActualSize()
    {
        _fitToWindow = false;
        _zoom = 1f;
        _panOffset = PointF.Empty;
        _zoomLabel.Text = "100%";
        _canvas.Invalidate();
    }

    private void AdjustZoom(float factor)
    {
        if (_fitToWindow)
        {
            _fitToWindow = false;
            _zoom = ComputeFitZoom();
        }
        _zoom = Math.Clamp(_zoom * factor, 0.2f, 5.0f);
        _zoomLabel.Text = $"{Math.Round(_zoom * 100)}%";
        _canvas.Invalidate();
    }

    private float ComputeFitZoom()
    {
        if (_currentBitmap is null || _currentBitmap.Width <= 0 || _currentBitmap.Height <= 0) return 1f;
        var availW = Math.Max(40f, _canvas.Width - 36f);
        var availH = Math.Max(40f, _canvas.Height - 36f);
        return Math.Min(availW / _currentBitmap.Width, availH / _currentBitmap.Height);
    }

    private void PaintStage(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.Primitives.Espresso900);

        if (_currentBitmap is null) return;

        var activeScale = _fitToWindow ? ComputeFitZoom() : _zoom;
        var drawW = _currentBitmap.Width * activeScale;
        var drawH = _currentBitmap.Height * activeScale;
        var x = (_canvas.Width - drawW) / 2f + (_fitToWindow ? 0f : _panOffset.X);
        var y = (_canvas.Height - drawH) / 2f + (_fitToWindow ? 0f : _panOffset.Y);

        var rect = new RectangleF(x, y, drawW, drawH);
        using (var framePen = new Pen(Color.FromArgb(70, Theme.Primitives.Stone400), 1f))
            g.DrawRectangle(framePen, x - 1, y - 1, drawW + 1, drawH + 1);
        g.DrawImage(_currentBitmap, rect);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _currentBitmap?.Dispose();
        base.Dispose(disposing);
    }
}
