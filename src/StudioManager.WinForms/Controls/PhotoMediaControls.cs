using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Controls;

/// <summary>
/// Rounded visual photo surface with clean cover framing, subtle badge pill, and click-to-open ImageViewerForm.
/// Does not dispose shared cached bitmaps managed by VisualAssetService.
/// </summary>
public sealed class VisualMediaPictureBox : Panel
{
    private VisualAssetEntryDto _entry;
    private IReadOnlyList<VisualAssetEntryDto> _gallery;
    private bool _hovered;

    public VisualMediaPictureBox(VisualAssetEntryDto entry, IReadOnlyList<VisualAssetEntryDto>? gallery = null)
    {
        _entry = entry;
        _gallery = gallery ?? [entry];

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        AccessibleName = entry.AltText;

        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        Click += (_, _) => OpenLightbox();
    }

    public int CornerRadius { get; set; } = 10;
    public bool ShowBadge { get; set; } = true;
    public int MaxThumbWidth { get; set; } = 720;

    public void BindAsset(VisualAssetEntryDto cover, IReadOnlyList<VisualAssetEntryDto>? gallery = null)
    {
        _entry = cover;
        _gallery = gallery ?? [cover];
        AccessibleName = cover.AltText;
        Invalidate();
    }

    public void OpenLightbox()
    {
        var idx = 0;
        for (var i = 0; i < _gallery.Count; i++)
        {
            if (string.Equals(_gallery[i].AssetId, _entry.AssetId, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }
        using var viewer = new ImageViewerForm(_gallery, idx);
        viewer.ShowDialog(FindForm());
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Parent?.BackColor is { A: byte.MaxValue } bg ? bg : Theme.Surface);

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(2, Width - 1), Math.Max(2, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, CornerRadius);

        var clipState = g.Save();
        g.SetClip(path);

        var img = VisualAssetService.GetOrLoadImage(_entry, MaxThumbWidth);
        AuthVisualAssets.DrawImageCover(g, img, bounds);

        if (_hovered)
        {
            using var hoverVeil = new SolidBrush(Color.FromArgb(88, 22, 20, 18));
            g.FillRectangle(hoverVeil, bounds);
            const string zoomText = "Xem ảnh lớn";
            using var zf = Theme.Font(8.6f, FontStyle.Bold);
            var sz = TextRenderer.MeasureText(g, zoomText, zf);
            var pill = new RectangleF((Width - sz.Width - 22) / 2f, (Height - 28) / 2f, sz.Width + 22, 28);
            using var pillPath = Theme.RoundedRectangle(pill, 14f);
            using var pillBg = new SolidBrush(Color.FromArgb(220, Theme.Primitives.Espresso900));
            g.FillPath(pillBg, pillPath);
            TextRenderer.DrawText(g, zoomText, zf, Rectangle.Round(pill), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        if (ShowBadge && Width >= 150)
        {
            var badgeText = _entry.SourceBadgeLabel;
            using var bf = Theme.Font(7.4f, FontStyle.Bold);
            var bsz = TextRenderer.MeasureText(g, badgeText, bf);
            var bRect = new Rectangle(Math.Max(8, Width - bsz.Width - 18), 8, bsz.Width + 10, 20);
            using var bp = Theme.RoundedRectangle(bRect, 6f);
            using var bBg = new SolidBrush(Color.FromArgb(196, 22, 20, 18));
            g.FillPath(bBg, bp);
            TextRenderer.DrawText(g, badgeText, bf, bRect, Theme.Primitives.Alabaster100,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        g.Restore(clipState);

        using var borderPen = new Pen(_hovered ? Theme.Primary : Theme.BorderStrong, _hovered ? 1.5f : 1f);
        g.DrawPath(borderPen, path);
    }
}

/// <summary>
/// Admin Media Manager (Demo Mode) supporting local upload preview, cover selection,
/// ordering, caption/alt-text editing, and reset. Enforces strict Admin-only edit guard;
/// Staff see read-only inspection controls only.
/// </summary>
public sealed class AdminMediaManagerDialog : Form
{
    private readonly MediaTargetEntityKind _entityKind;
    private readonly string _targetCode;
    private readonly string _targetName;
    private readonly bool _canEdit;
    private readonly string _actorLabel;

    private List<VisualAssetEntryDto> _items = [];
    private int _selectedIndex;

    private readonly ListBox _list = new()
    {
        Dock = DockStyle.Fill,
        Font = Theme.Font(9.0f),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.Primitives.Alabaster50
    };

    private VisualMediaPictureBox _preview = null!;
    private readonly TextBox _titleBox = new() { Dock = DockStyle.Top, Height = 30, Font = Theme.Font(9.2f) };
    private readonly TextBox _captionBox = new() { Dock = DockStyle.Top, Height = 56, Multiline = true, Font = Theme.Font(8.9f) };
    private readonly TextBox _altBox = new() { Dock = DockStyle.Top, Height = 30, Font = Theme.Font(8.9f) };
    private readonly CheckBox _illustrativeChk = new() { Dock = DockStyle.Top, Height = 26, Text = "Đánh dấu là ảnh minh họa catalogue", Font = Theme.Font(8.8f) };
    private readonly Label _statusBanner = new() { Dock = DockStyle.Top, Height = 34, Font = Theme.Font(8.6f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

    public bool Changed { get; private set; }

    public AdminMediaManagerDialog(
        MediaTargetEntityKind entityKind,
        string targetCode,
        string targetName,
        bool canEdit,
        string actorLabel)
    {
        _entityKind = entityKind;
        _targetCode = targetCode;
        _targetName = targetName;
        _canEdit = canEdit;
        _actorLabel = actorLabel;

        Text = canEdit
            ? $"Quản lý hình ảnh (Demo) — {targetCode}: {targetName}"
            : $"Xem thư viện hình ảnh (Chỉ đọc) — {targetCode}: {targetName}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 620);
        MinimumSize = new Size(860, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Theme.Background;
        Font = Theme.Font();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _statusBanner.Text = _canEdit
            ? "QUẢN TRỊ VIÊN • CHẾ ĐỘ DEMO CỤC BỘ: Thay đổi chỉ áp dụng tạm trong phiên làm việc (không ghi xuống SQL / tệp gốc)."
            : "NHÂN VIÊN (CHẾ ĐỘ CHỈ XEM): Chỉ Quản trị viên mới được thêm hoặc chỉnh sửa ảnh đại diện của danh mục.";
        _statusBanner.ForeColor = _canEdit ? Theme.Primary : Theme.Muted;
        root.SetColumnSpan(_statusBanner, 2);

        // Left: Gallery List + Upload/Order Actions
        var leftPanel = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(0, 6, 8, 0) };
        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, _canEdit ? 92 : 42));

        var listTitle = new Label { Dock = DockStyle.Fill, Text = $"Thư viện của {targetCode}", Font = Theme.Font(9.5f, FontStyle.Bold), ForeColor = Theme.Text };
        var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), WrapContents = true };

        var viewFullBtn = Theme.OutlineButton("Mở trình xem ảnh", Theme.Primary, IconKind.Eye);
        viewFullBtn.Click += (_, _) => _preview.OpenLightbox();
        btnFlow.Controls.Add(viewFullBtn);

        if (_canEdit)
        {
            var addBtn = Theme.Button("Tải ảnh từ máy (Demo)...", Theme.Primary, IconKind.Plus);
            addBtn.Width = 185;
            addBtn.Click += (_, _) => OnUploadLocalFile();

            var setCoverBtn = Theme.OutlineButton("Đặt làm ảnh bìa");
            setCoverBtn.Click += (_, _) => OnSetCover();

            var upBtn = Theme.OutlineButton("Lên");
            upBtn.Width = 64;
            upBtn.Click += (_, _) => OnMove(-1);

            var downBtn = Theme.OutlineButton("Xuống");
            downBtn.Width = 74;
            downBtn.Click += (_, _) => OnMove(1);

            var delBtn = Theme.OutlineButton("Xóa ảnh", Theme.Danger);
            delBtn.Click += (_, _) => OnRemove();

            var resetBtn = Theme.OutlineButton("Khôi phục mặc định");
            resetBtn.Click += (_, _) => OnResetDefault();

            btnFlow.Controls.Add(addBtn);
            btnFlow.Controls.Add(setCoverBtn);
            btnFlow.Controls.Add(upBtn);
            btnFlow.Controls.Add(downBtn);
            btnFlow.Controls.Add(delBtn);
            btnFlow.Controls.Add(resetBtn);
        }

        leftLayout.Controls.Add(listTitle, 0, 0);
        leftLayout.Controls.Add(_list, 0, 1);
        leftLayout.Controls.Add(btnFlow, 0, 2);
        leftPanel.Controls.Add(leftLayout);

        // Right: Preview + Metadata
        var rightPanel = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(8, 6, 0, 0) };
        var rightLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 54f));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 46f));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _preview = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(_targetCode), _items) { Dock = DockStyle.Fill };

        var metaPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), AutoScroll = true };
        _titleBox.ReadOnly = !_canEdit;
        _captionBox.ReadOnly = !_canEdit;
        _altBox.ReadOnly = !_canEdit;
        _illustrativeChk.Enabled = _canEdit;

        Control WrapField(string lbl, Control c)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = c.Height + 24, Padding = new Padding(0, 2, 0, 4) };
            var l = new Label { Dock = DockStyle.Top, Height = 20, Text = lbl, Font = Theme.Font(8.2f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
            p.Controls.Add(c);
            p.Controls.Add(l);
            return p;
        }
        metaPanel.Controls.Add(_illustrativeChk);
        metaPanel.Controls.Add(WrapField("ALT-TEXT TRUY CẬP", _altBox));
        metaPanel.Controls.Add(WrapField("CHÚ THÍCH HIỂN THỊ", _captionBox));
        metaPanel.Controls.Add(WrapField("TIÊU ĐỀ ẢNH", _titleBox));

        var bottomBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var closeBtn = Theme.OutlineButton("Đóng");
        closeBtn.Click += (_, _) => Close();
        bottomBar.Controls.Add(closeBtn);

        if (_canEdit)
        {
            var saveMetaBtn = Theme.Button("Lưu chú thích (Demo)", Theme.Primitives.Sage600, IconKind.Check);
            saveMetaBtn.Width = 178;
            saveMetaBtn.Click += (_, _) => OnSaveMetadata();
            bottomBar.Controls.Add(saveMetaBtn);
        }

        rightLayout.Controls.Add(_preview, 0, 0);
        rightLayout.Controls.Add(metaPanel, 0, 1);
        rightLayout.Controls.Add(bottomBar, 0, 2);
        rightPanel.Controls.Add(rightLayout);

        root.Controls.Add(_statusBanner, 0, 0);
        root.Controls.Add(leftPanel, 0, 1);
        root.Controls.Add(rightPanel, 1, 1);
        Controls.Add(root);

        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _items.Count)
                ShowSelected(_list.SelectedIndex);
        };

        ReloadGallery(0);
    }

    private void ReloadGallery(int preferIndex)
    {
        _items = VisualAssetService.GetGallery(_targetCode).ToList();
        _list.Items.Clear();
        for (var i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            var badge = it.IsPrimaryCover ? "★ BÌA" : $"#{it.SortOrder}";
            _list.Items.Add($"[{badge}] {it.Title}  ({it.SourceBadgeLabel})");
        }
        if (_items.Count > 0)
        {
            _selectedIndex = Math.Clamp(preferIndex, 0, _items.Count - 1);
            _list.SelectedIndex = _selectedIndex;
            ShowSelected(_selectedIndex);
        }
    }

    private void ShowSelected(int idx)
    {
        _selectedIndex = idx;
        var item = _items[idx];
        _preview.BindAsset(item, _items);
        _titleBox.Text = item.Title;
        _captionBox.Text = item.Caption;
        _altBox.Text = item.AltText;
        _illustrativeChk.Checked = item.IsIllustrative;
    }

    private void OnUploadLocalFile()
    {
        using var ofd = new OpenFileDialog
        {
            Title = $"Chọn ảnh cục bộ cho {_targetName} (Demo)",
            Filter = "Tệp ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (ofd.ShowDialog(this) != DialogResult.OK) return;

        VisualAssetService.AddAdminDemoUpload(
            _entityKind, _targetCode, ofd.FileName,
            Path.GetFileNameWithoutExtension(ofd.FileName),
            $"Ảnh tải lên thử nghiệm bởi {_actorLabel}",
            $"Ảnh tải lên cho {_targetName}",
            setAsPrimaryCover: true,
            _actorLabel);
        Changed = true;
        ReloadGallery(0);
    }

    private void OnSetCover()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return;
        VisualAssetService.SetPrimaryCover(_targetCode, _items[_selectedIndex].AssetId);
        Changed = true;
        ReloadGallery(0);
    }

    private void OnMove(int delta)
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return;
        VisualAssetService.MoveOrder(_targetCode, _items[_selectedIndex].AssetId, delta);
        Changed = true;
        ReloadGallery(Math.Clamp(_selectedIndex + delta, 0, _items.Count - 1));
    }

    private void OnRemove()
    {
        if (_items.Count <= 1)
        {
            AtelierNotificationService.ShowWarning(this, "Cần giữ lại ít nhất 01 ảnh đại diện cho hạng mục này.", "Giới hạn an toàn");
            return;
        }
        VisualAssetService.RemoveAsset(_targetCode, _items[_selectedIndex].AssetId);
        Changed = true;
        ReloadGallery(0);
    }

    private void OnResetDefault()
    {
        VisualAssetService.ResetTargetToDefault(_targetCode);
        Changed = true;
        ReloadGallery(0);
    }

    private void OnSaveMetadata()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return;
        VisualAssetService.UpdateMetadata(_targetCode, _items[_selectedIndex].AssetId,
            _titleBox.Text, _captionBox.Text, _altBox.Text, _illustrativeChk.Checked);
        Changed = true;
        ReloadGallery(_selectedIndex);
    }
}

