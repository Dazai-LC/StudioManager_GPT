using StudioManager.Application.Services;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.Pages;

/// <summary>
/// STUDIOMANAGER v2.2 — WORKSTREAM G FRONTEND WORKSPACE
/// Trung tâm Đối soát VietQR, Chuyển khoản Thủ công & Biên nhận (Tab 2.3 Hub Lịch Chụp).
/// Không sử dụng SePay SDK/Webhook; mọi xác nhận thực hiện đối soát thủ công hoặc thu tiền mặt tại quầy.
/// </summary>
public sealed class PaymentWorkspacePlaceholderPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _grid = Theme.Grid();
    private readonly SearchInput _searchInput = new("Tìm theo mã GD, mã tham chiếu, tên khách hoặc nội dung CK...")
    {
        Dock = DockStyle.Fill,
        Height = 38
    };

    private StatCard _cardTotal = null!;
    private StatCard _cardPending = null!;
    private StatCard _cardMatched = null!;
    private StatCard _cardIssueOrCash = null!;

    private readonly Label _dossierHeader = new() { Dock = DockStyle.Top, Height = 36, Font = Theme.Font(11.0f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly CardPanel _dossierKvCard = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        BackColor = Theme.Primitives.Alabaster50,
        BorderColor = Theme.Border,
        Padding = new Padding(10, 8, 10, 8),
        Margin = new Padding(0, 2, 0, 8)
    };
    private readonly Label _kvCustomer = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f), ForeColor = Theme.Text };
    private readonly Label _kvTarget = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.MonoFont(8.4f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly Label _kvSyntax = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.MonoFont(8.4f, FontStyle.Bold), ForeColor = Theme.Primitives.Teal600 };
    private readonly Label _kvAmounts = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.5f, FontStyle.Bold), ForeColor = Theme.Text };
    private readonly Label _kvTimes = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.4f), ForeColor = Theme.TextSecondary };
    private readonly Label _kvCustomerNote = new() { AutoSize = true, Dock = DockStyle.Fill, Font = Theme.Font(8.4f), ForeColor = Theme.Muted };
    private readonly NumericUpDown _confirmAmountBox = new() { Minimum = 0, Maximum = 100_000_000, Increment = 100_000, ThousandsSeparator = true, Width = 148, Font = Theme.Font(9.5f) };
    private readonly TextBox _reviewerNoteBox = new() { Dock = DockStyle.Top, Height = 44, Multiline = true, Font = Theme.Font(8.9f), PlaceholderText = "Ghi chú đối soát sao kê / lý do sai lệch..." };
    private readonly Label _evidenceInfo = new() { Dock = DockStyle.Top, Height = 26, Font = Theme.MonoFont(8.2f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly Label _summaryLabel = new() { Dock = DockStyle.Right, Width = 220, Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Primitives.Teal600, TextAlign = ContentAlignment.MiddleRight };

    private string _activeFilter = "ALL";
    private List<PaymentSubmissionDto> _visible = [];

    public PaymentWorkspacePlaceholderPage(AppFacade app)
    {
        _app = app;
        DesktopDemoSessionStore.EnsureSeeded();

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Đối soát thanh toán VietQR & Thu tiền mặt (Frontend Demo)",
            "Chuyển khoản VietQR cú pháp PHOENIX [Mã] • Nhân sự kiểm tra chứng từ đối soát thủ công hoặc thu tiền mặt tại quầy");

        var refreshBtn = Theme.OutlineButton("Làm mới danh sách", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Click += (_, _) => ReloadPayments();
        header.Actions.Controls.Add(refreshBtn);

        Controls.Add(BuildWorkspace());
        Controls.Add(BuildToolbar());
        Controls.Add(BuildKpiStrip());
        Controls.Add(header);

        ReloadPayments();
    }

    private Control BuildKpiStrip()
    {
        var strip = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 10)
        };
        for (var i = 0; i < 4; i++) strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        _cardTotal = new StatCard("TỔNG KHAI BÁO", Theme.Primary, IconKind.Wallet) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardPending = new StatCard("CHỜ ĐỐI SOÁT", Theme.Warning, IconKind.Clock) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardMatched = new StatCard("ĐÃ KHỚP THỦ CÔNG", Theme.Success, IconKind.Check) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0) };
        _cardIssueOrCash = new StatCard("SAI LỆCH / TIỀN MẶT", Theme.Cyan, IconKind.AlertTriangle) { Dock = DockStyle.Fill, Margin = Padding.Empty };

        strip.Controls.Add(_cardTotal, 0, 0);
        strip.Controls.Add(_cardPending, 1, 0);
        strip.Controls.Add(_cardMatched, 2, 0);
        strip.Controls.Add(_cardIssueOrCash, 3, 0);
        return strip;
    }

    private Control BuildToolbar()
    {
        var card = new CardPanel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 0, 10) };
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var sRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        sRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        sRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        _searchInput.TextChanged += (_, _) => ApplyFilter();
        sRow.Controls.Add(_searchInput, 0, 0);
        sRow.Controls.Add(_summaryLabel, 1, 0);

        var tabs = new SegmentedTabBar { Dock = DockStyle.Fill };
        tabs.SetItems([
            new("ALL", "Tất cả", IconKind.Layers),
            new(nameof(PaymentReconciliationStatus.CHO_DOI_SOAT), "Chờ đối soát", IconKind.Clock),
            new(nameof(PaymentReconciliationStatus.DA_KHOP_THU_CONG), "Đã khớp thủ công", IconKind.Check),
            new(nameof(PaymentReconciliationStatus.SAI_LECH_THONG_TIN), "Sai lệch", IconKind.AlertTriangle),
            new(nameof(PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT), "Tiền mặt", IconKind.Wallet)
        ], "ALL");
        tabs.SegmentSelected += (_, seg) => { _activeFilter = seg.Key; ApplyFilter(); };

        rows.Controls.Add(sRow, 0, 0);
        rows.Controls.Add(tabs, 0, 1);
        card.Controls.Add(rows);
        return card;
    }

    private Control BuildWorkspace()
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(12) };
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => BindSelected();
        gridCard.Controls.Add(_grid);

        var rightCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(14),
            AccentStripeColor = Theme.Primitives.Teal600,
            AccentStripeThickness = 4
        };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var amountRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false, Padding = new Padding(0, 2, 0, 0) };
        amountRow.Controls.Add(new Label { AutoSize = true, Text = "Số tiền xác nhận:", Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = Theme.TextSecondary, Padding = new Padding(0, 6, 6, 0) });
        amountRow.Controls.Add(_confirmAmountBox);
        var viewEvidenceBtn = Theme.OutlineButton("Xem chứng từ", Theme.Primary, IconKind.Eye);
        viewEvidenceBtn.Width = 138;
        viewEvidenceBtn.Height = 30;
        viewEvidenceBtn.Margin = new Padding(8, 0, 0, 0);
        viewEvidenceBtn.Click += (_, _) => OpenEvidenceViewer();
        amountRow.Controls.Add(viewEvidenceBtn);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 96, WrapContents = true, Padding = new Padding(0, 6, 0, 0), BackColor = Color.Transparent };
        var matchBtn = Theme.Button("Khớp thủ công VietQR", Theme.Success, IconKind.Check);
        matchBtn.Width = 172;
        matchBtn.Height = 36;
        matchBtn.Click += (_, _) => Reconcile(PaymentReconciliationStatus.DA_KHOP_THU_CONG);

        var mismatchBtn = Theme.OutlineButton("Đánh dấu sai lệch", Theme.Warning, IconKind.AlertTriangle);
        mismatchBtn.Width = 152;
        mismatchBtn.Height = 36;
        mismatchBtn.Click += (_, _) => Reconcile(PaymentReconciliationStatus.SAI_LECH_THONG_TIN);

        var cashBtn = Theme.OutlineButton("Thu tiền mặt tại quầy", Theme.Primitives.Teal600, IconKind.Wallet);
        cashBtn.Width = 174;
        cashBtn.Height = 36;
        cashBtn.Click += (_, _) => Reconcile(PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT);

        var rejectBtn = Theme.OutlineButton("Từ chối chứng từ", Theme.Danger, IconKind.Close);
        rejectBtn.Width = 150;
        rejectBtn.Height = 36;
        rejectBtn.Click += (_, _) => Reconcile(PaymentReconciliationStatus.TU_CHOI);

        var receiptBtn = Theme.Button("Xem trước biên nhận (A4 / K80 / K58 / PDF)", Theme.Primary, IconKind.Printer);
        receiptBtn.Width = 310;
        receiptBtn.Height = 36;
        receiptBtn.Click += (_, _) => OpenReceipt();

        actions.Controls.Add(matchBtn);
        actions.Controls.Add(mismatchBtn);
        actions.Controls.Add(cashBtn);
        actions.Controls.Add(rejectBtn);
        actions.Controls.Add(receiptBtn);

        var kvGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        kvGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        kvGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddKvRow(int rowIdx, string key, Label val)
        {
            var lblK = new Label
            {
                Dock = DockStyle.Fill,
                Text = key,
                UseMnemonic = false,
                Font = Theme.Font(8.2f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 2, 6, 2)
            };
            val.UseMnemonic = false;
            val.Margin = new Padding(0, 2, 0, 2);
            kvGrid.Controls.Add(lblK, 0, rowIdx);
            kvGrid.Controls.Add(val, 1, rowIdx);
        }

        AddKvRow(0, "Khách hàng:", _kvCustomer);
        AddKvRow(1, "Tham chiếu:", _kvTarget);
        AddKvRow(2, "Cú pháp CK:", _kvSyntax);
        AddKvRow(3, "Khai báo / Thu:", _kvAmounts);
        AddKvRow(4, "Khởi tạo / Khớp:", _kvTimes);
        AddKvRow(5, "Ghi chú khách:", _kvCustomerNote);
        _dossierKvCard.Controls.Add(kvGrid);
        scroll.SizeChanged += (_, _) =>
        {
            var maxValW = Math.Max(160, scroll.ClientSize.Width - 156);
            _kvCustomer.MaximumSize = new Size(maxValW, 0);
            _kvTarget.MaximumSize = new Size(maxValW, 0);
            _kvSyntax.MaximumSize = new Size(maxValW, 0);
            _kvAmounts.MaximumSize = new Size(maxValW, 0);
            _kvTimes.MaximumSize = new Size(maxValW, 0);
            _kvCustomerNote.MaximumSize = new Size(maxValW, 0);
        };

        scroll.Controls.Add(_reviewerNoteBox);
        scroll.Controls.Add(new Label { Dock = DockStyle.Top, Height = 20, Text = "GHI CHÚ ĐỐI SOÁT CỦA NHÂN VIÊN / KẾ TOÁN", UseMnemonic = false, Font = Theme.Font(7.8f, FontStyle.Bold), ForeColor = Theme.Muted });
        scroll.Controls.Add(amountRow);
        scroll.Controls.Add(_evidenceInfo);
        scroll.Controls.Add(_dossierKvCard);
        scroll.Controls.Add(_dossierHeader);

        rightCard.Controls.Add(scroll);
        rightCard.Controls.Add(actions);
        split.Controls.Add(gridCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        return split;
    }

    private void ReloadPayments()
    {
        var all = DesktopDemoSessionStore.GetAllPaymentSubmissions();
        _cardTotal.Value = all.Count.ToString();
        _cardTotal.Trend = "Đồng bộ VietQR & Quầy (Demo)";
        _cardPending.Value = all.Count(x => x.TrangThai == PaymentReconciliationStatus.CHO_DOI_SOAT).ToString();
        _cardPending.Trend = "Cần kiểm tra sao kê thủ công";
        _cardMatched.Value = all.Count(x => x.TrangThai == PaymentReconciliationStatus.DA_KHOP_THU_CONG).ToString();
        _cardMatched.Trend = "Đã khớp chứng từ CK";
        _cardIssueOrCash.Value = all.Count(x => x.TrangThai is PaymentReconciliationStatus.SAI_LECH_THONG_TIN or PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT).ToString();
        _cardIssueOrCash.Trend = "Sai lệch cần tra soát / Tiền mặt";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = _searchInput.Text.Trim();
        var items = DesktopDemoSessionStore.GetAllPaymentSubmissions().AsEnumerable();
        if (_activeFilter != "ALL")
            items = items.Where(x => x.TrangThai.ToString() == _activeFilter);

        if (!string.IsNullOrWhiteSpace(kw))
        {
            items = items.Where(x =>
                x.MaGiaoDichDemo.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.MaThamChieu.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.TenKhachHang.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                x.NoiDungChuyenKhoan.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        _visible = items.ToList();
        _summaryLabel.Text = $"{_visible.Count} giao dịch hiển thị";

        _grid.DataSource = _visible.Select(x => new
        {
            MaGD = x.MaGiaoDichDemo,
            ThamChieu = x.MaThamChieu,
            KhachHang = x.TenKhachHang,
            KhaiBao = $"{x.SoTienKhaiBao:N0} đ",
            NoiDungCK = x.NoiDungChuyenKhoan,
            TrangThai = DesktopDemoSessionStore.GetPaymentStatusLabel(x.TrangThai)
        }).ToList();

        if (_grid.Columns.Count >= 6)
        {
            _grid.Columns["MaGD"]!.HeaderText = "Mã giao dịch";
            _grid.Columns["ThamChieu"]!.HeaderText = "Tham chiếu";
            _grid.Columns["KhachHang"]!.HeaderText = "Khách hàng";
            _grid.Columns["KhaiBao"]!.HeaderText = "Khai báo";
            _grid.Columns["NoiDungCK"]!.HeaderText = "Cú pháp CK";
            _grid.Columns["TrangThai"]!.HeaderText = "Trạng thái";
            _grid.Columns["MaGD"]!.FillWeight = 110f;
            _grid.Columns["ThamChieu"]!.FillWeight = 112f;
            _grid.Columns["NoiDungCK"]!.FillWeight = 128f;
            _grid.Columns["TrangThai"]!.FillWeight = 112f;
        }

        BindSelected();
    }

    private PaymentSubmissionDto? SelectedTx()
    {
        if (_grid.CurrentRow is null) return _visible.FirstOrDefault();
        var code = _grid.CurrentRow.Cells["MaGD"]?.Value?.ToString();
        return _visible.FirstOrDefault(x => x.MaGiaoDichDemo == code);
    }

    private void BindSelected()
    {
        var tx = SelectedTx();
        if (tx is null)
        {
            _dossierHeader.Text = "Chưa chọn giao dịch";
            _kvCustomer.Text = "-";
            _kvTarget.Text = "-";
            _kvSyntax.Text = "-";
            _kvAmounts.Text = "-";
            _kvTimes.Text = "-";
            _kvCustomerNote.Text = "-";
            return;
        }
        _dossierHeader.Text = $"{tx.MaGiaoDichDemo}  •  {DesktopDemoSessionStore.GetPaymentStatusLabel(tx.TrangThai)}";
        _kvCustomer.Text = $"{tx.TenKhachHang} ({tx.SoDienThoai})";
        _kvTarget.Text = $"{tx.MaThamChieu} ({tx.TargetType})";
        _kvSyntax.Text = tx.NoiDungChuyenKhoan;
        _kvAmounts.Text = $"Khai báo: {tx.SoTienKhaiBao:N0} đ  •  Xác nhận: {tx.SoTienXacNhan:N0} đ";
        _kvTimes.Text = $"{tx.CreatedAtLabel}  •  {(tx.ReconciledAtLabel ?? "Chưa đối soát")}";
        _kvCustomerNote.Text = string.IsNullOrWhiteSpace(tx.EvidenceNotes) ? "Không có" : tx.EvidenceNotes;
        _evidenceInfo.Text = $"Chứng từ đính kèm: {tx.EvidenceFileName ?? "Dùng thẻ minh họa mặc định"}";
        _confirmAmountBox.Value = Math.Clamp(tx.SoTienXacNhan > 0 ? tx.SoTienXacNhan : tx.SoTienKhaiBao, 0, 100_000_000);
        _reviewerNoteBox.Text = tx.StaffReviewerNote;
    }

    private void OpenEvidenceViewer()
    {
        var tx = SelectedTx();
        if (tx is null) return;
        if (!string.IsNullOrWhiteSpace(tx.EvidenceFilePath) && File.Exists(tx.EvidenceFilePath))
        {
            var entry = new VisualAssetEntryDto(
                tx.MaGiaoDichDemo, MediaTargetEntityKind.StudioAlbum, tx.MaGiaoDichDemo,
                $"Chứng từ {tx.MaGiaoDichDemo}", tx.EvidenceNotes, "Chứng từ thanh toán",
                tx.EvidenceFilePath, tx.EvidenceFilePath, true, false, MediaSourceKind.AdminUploadPreview,
                1, new FileInfo(tx.EvidenceFilePath).Length, 1000, 1000, tx.TenKhachHang, tx.CreatedAtLabel);
            using var viewer = new ImageViewerForm([entry], 0);
            viewer.ShowDialog(FindForm());
            return;
        }

        AtelierNotificationService.ShowInfo(
            FindForm(),
            $"Giao dịch {tx.MaGiaoDichDemo} ({tx.MaThamChieu}) chưa có tệp ảnh chụp chứng từ từ khách hàng.\n\n" +
            $"• Nội dung CK khai báo: {tx.NoiDungChuyenKhoan}\n" +
            $"• Số tiền khai báo: {tx.SoTienKhaiBao:N0} đ\n" +
            $"• Ghi chú khách: {tx.EvidenceNotes}",
            "Chưa có ảnh chứng từ đính kèm");
    }

    private void Reconcile(PaymentReconciliationStatus status)
    {
        var tx = SelectedTx();
        if (tx is null) return;
        var note = string.IsNullOrWhiteSpace(_reviewerNoteBox.Text)
            ? $"Cập nhật thủ công bởi {_app.Session?.HoTen ?? "Nhân sự"} lúc {DateTime.Now:HH:mm}"
            : _reviewerNoteBox.Text.Trim();
        DesktopDemoSessionStore.UpdatePaymentReconciliationFromStaff(
            tx.MaGiaoDichDemo, status, _confirmAmountBox.Value, note);
        ReloadPayments();
        AtelierNotificationService.ShowToast(
            FindForm(),
            status is PaymentReconciliationStatus.DA_KHOP_THU_CONG or PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT
                ? AtelierNotificationKind.Success
                : AtelierNotificationKind.Warning,
            "Đã cập nhật đối soát thanh toán",
            $"Giao dịch {tx.MaGiaoDichDemo}: {DesktopDemoSessionStore.GetPaymentStatusLabel(status)} ({_confirmAmountBox.Value:N0} đ).");
    }

    private void OpenReceipt()
    {
        var tx = SelectedTx();
        if (tx is null) return;
        var doc = DesktopDemoSessionStore.BuildReceiptForPayment(
            tx.MaGiaoDichDemo,
            _app.Session?.HoTen ?? "Nhân sự Kế toán (Demo)",
            ReceiptFormatKind.A4Invoice);
        if (doc is null) return;

        using var dlg = new ExtendedReceiptPreviewForm(doc);
        dlg.ShowDialog(FindForm());
    }
}
