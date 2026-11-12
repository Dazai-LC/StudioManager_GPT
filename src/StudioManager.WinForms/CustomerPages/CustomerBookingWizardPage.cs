using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerBookingWizardPage : UserControl
{
    private readonly CustomerSessionContext _session;
    private readonly ICustomerNavigator _nav;

    private readonly IReadOnlyList<CatalogPackageViewDto> _packages = SeedGroundedCatalogRepository.GetPackages();
    private readonly IReadOnlyList<CatalogRoomViewDto> _rooms = SeedGroundedCatalogRepository.GetRooms();
    private readonly IReadOnlyList<CatalogServiceViewDto> _services = SeedGroundedCatalogRepository.GetServices();

    private CatalogPackageViewDto _chosenPkg;
    private readonly Dictionary<string, PackageCardRefs> _packageCards = new(StringComparer.OrdinalIgnoreCase);

    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Short, MinDate = DateTime.Today.AddDays(1), Value = DateTime.Today.AddDays(3), Width = 180 };
    private readonly ComboBox _slotBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly ComboBox _roomPrefBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly NumericUpDown _guestCount = new() { Minimum = 1, Maximum = 35, Value = 2, Width = 96 };
    private readonly Label _durationHintLbl = new() { AutoSize = true, Font = Theme.Font(8.4f, FontStyle.Italic), ForeColor = Theme.Muted };
    private readonly Dictionary<string, NumericUpDown> _serviceQtyBoxes = new(StringComparer.OrdinalIgnoreCase);

    private readonly TextBox _contactName = new() { Width = 340, Font = Theme.Font(9.5f) };
    private readonly TextBox _contactPhone = new() { Width = 240, Font = Theme.Font(9.5f) };
    private readonly TextBox _contactEmail = new() { Width = 340, Font = Theme.Font(9.5f) };
    private readonly TextBox _notesBox = new() { Width = 540, Height = 76, Multiline = true, Font = Theme.Font(9.2f), PlaceholderText = "Concept mong muốn, trang phục dự kiến mượn hoặc ghi chú thành viên..." };

    private VisualMediaPictureBox _summaryThumb = null!;
    private Label _sumPkgTitle = null!;
    private Label _sumPkgMeta = null!;
    private Label _sumDateVal = null!;
    private Label _sumRoomVal = null!;
    private Label _sumGuestVal = null!;
    private Label _sumBasePriceVal = null!;
    private FlowLayoutPanel _sumExtraServicesHost = null!;
    private Label _sumGrandTotalVal = null!;
    private Label _sumDepositHintVal = null!;
    private readonly AtelierInlineAlert _wizardAlert = new() { Dock = DockStyle.Bottom, Height = 58, Margin = new Padding(0, 6, 0, 6) };

    private Panel _step5ReviewHost = null!;

    private readonly SegmentedTabBar _modeTabs = new() { Dock = DockStyle.Top, Height = 42, Margin = new Padding(0, 0, 0, 8) };
    private readonly Panel _wizardHost = new() { Dock = DockStyle.Fill };
    private readonly Panel _historyHost = new() { Dock = DockStyle.Fill, Visible = false };

    private int _stepIndex;
    private readonly List<Panel> _stepPanels = [];
    private readonly List<StepPillRefs> _stepPills = [];
    private readonly Label _stepBanner = new() { Dock = DockStyle.Top, Height = 30, UseMnemonic = false, Font = Theme.Font(10.2f, FontStyle.Bold), ForeColor = Theme.Primary };
    private readonly ModernButton _prevStepBtn = Theme.OutlineButton("Quay lại bước trước");
    private readonly ModernButton _nextStepBtn = Theme.Button("Tiếp tục", Theme.Primary, IconKind.ArrowRight);

    private readonly DataGridView _historyGrid = Theme.Grid();
    private readonly Panel _historyDetailHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(2) };
    private readonly Label _historyCountBadge = new() { AutoSize = true, Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = Theme.Muted };
    private readonly ModernButton _cancelReqBtn = Theme.OutlineButton("Hủy yêu cầu này", Theme.Danger, IconKind.Close);
    private readonly ModernButton _payReqBtn = Theme.Button("Thanh toán cọc VietQR (Demo)", Theme.Primitives.Sage600, IconKind.Wallet);
    private string _statusFilter = "ALL";
    private readonly Dictionary<string, ModernButton> _filterButtons = new(StringComparer.OrdinalIgnoreCase);
    private List<CustomerBookingRequestDto> _visibleRequests = [];

    private sealed record PackageCardRefs(
        CatalogPackageViewDto Package,
        CardPanel Card,
        Label SelectedBadge,
        ModernButton SelectBtn);

    private sealed record StepPillRefs(
        int StepIdx,
        Panel Pill,
        Label NumLbl,
        Label TitleLbl);

    public string SelectedPackageCode => _chosenPkg.MaGoiChup;
    public int CurrentStepIndex => _stepIndex;

    public CustomerBookingWizardPage(CustomerSessionContext session, ICustomerNavigator nav, string? preselectedPkgCode = null, bool openHistory = false)
    {
        _session = session;
        _nav = nav;
        _chosenPkg = _packages.FirstOrDefault(x => x.MaGoiChup == preselectedPkgCode) ?? _packages[0];

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        _contactName.Text = _session.Profile.FullName;
        _contactPhone.Text = _session.Profile.PhoneNumber.Replace(" ", "");
        _contactEmail.Text = _session.Profile.Email;

        RebuildTimeSlotsForPackage(0);
        _roomPrefBox.Items.Add("Nhờ studio tư vấn phòng phù hợp");
        foreach (var r in _rooms) _roomPrefBox.Items.Add($"{r.MaPhong} — {r.TenPhong}");
        _roomPrefBox.SelectedIndex = 0;

        var header = new PageHeader(
            "Đặt lịch & Theo dõi yêu cầu của tôi",
            "Chọn gói, ngày chụp mong muốn & dịch vụ • Yêu cầu gửi đi sẽ được nhân viên kiểm tra trùng lịch trước khi xác nhận");

        _modeTabs.SetItems([
            new("wizard", "Tạo yêu cầu đặt lịch (5 bước)", IconKind.Plus),
            new("history", "Lịch sử yêu cầu đã gửi", IconKind.History)
        ], openHistory ? "history" : "wizard");
        _modeTabs.SegmentSelected += (_, seg) =>
        {
            _wizardHost.Visible = seg.Key == "wizard";
            _historyHost.Visible = seg.Key == "history";
            if (seg.Key == "history") ReloadHistory();
        };

        BuildWizardView();
        BuildHistoryView();

        _wizardHost.Visible = !openHistory;
        _historyHost.Visible = openHistory;

        var contentHost = new Panel { Dock = DockStyle.Fill };
        contentHost.Controls.Add(_historyHost);
        contentHost.Controls.Add(_wizardHost);

        Controls.Add(contentHost);
        Controls.Add(_modeTabs);
        Controls.Add(header);

        ApplyPreselectedPackage(preselectedPkgCode ?? _chosenPkg.MaGoiChup);
        if (openHistory) ReloadHistory();
    }

    public void ApplyPreselectedPackage(string? pkgCode)
    {
        if (string.IsNullOrWhiteSpace(pkgCode)) return;
        SelectPackage(pkgCode);
    }

    public bool SelectPackage(string pkgCode)
    {
        var found = _packages.FirstOrDefault(x => string.Equals(x.MaGoiChup, pkgCode, StringComparison.OrdinalIgnoreCase));
        if (found is null) return false;

        _chosenPkg = found;
        var prevSlotIdx = Math.Max(0, _slotBox.SelectedIndex);
        RebuildTimeSlotsForPackage(prevSlotIdx);
        UpdatePackageCardVisualStates();
        RefreshSummary();
        if (_stepIndex == 4) RebuildStep5SummaryCard();
        return true;
    }

    public void GoToStep(int idx) => SetStep(idx);

    private void RebuildTimeSlotsForPackage(int preserveIdx)
    {
        var minutes = Math.Max(60, _chosenPkg?.ThoiLuongPhut ?? 120);
        var startTimes = new[] { new TimeSpan(9, 0, 0), new TimeSpan(13, 30, 0), new TimeSpan(16, 30, 0) };
        _slotBox.BeginUpdate();
        _slotBox.Items.Clear();
        foreach (var start in startTimes)
        {
            var end = start.Add(TimeSpan.FromMinutes(minutes));
            _slotBox.Items.Add($"{start:hh\\:mm} - {end:hh\\:mm}");
        }
        _slotBox.SelectedIndex = Math.Clamp(preserveIdx, 0, _slotBox.Items.Count - 1);
        _slotBox.EndUpdate();

        _durationHintLbl.Text = $"Thời lượng gói {_chosenPkg?.TenGoi}: {minutes} phút ({minutes / 60m:0.#} giờ) — khung giờ tự động tính theo gói đã chọn.";
    }

    private void BuildWizardView()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));

        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(18, 14, 18, 14) };

        var progressBar = BuildStepProgressHeader();
        var bodyHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 4, 0, 4) };

        _stepPanels.Clear();
        _stepPanels.Add(BuildStep1Package());
        _stepPanels.Add(BuildStep2TimeRoom());
        _stepPanels.Add(BuildStep3Services());
        _stepPanels.Add(BuildStep4Contact());
        _stepPanels.Add(BuildStep5Review());
        foreach (var sp in _stepPanels) bodyHost.Controls.Add(sp);

        var navBar = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(0, 8, 0, 0) };
        navBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawLine(pen, 0, 0, navBar.Width, 0);
        };
        var navFlow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        _nextStepBtn.Width = 210;
        _nextStepBtn.Height = 38;
        _nextStepBtn.Click += (_, _) => OnNextOrSubmit();
        _prevStepBtn.Width = 176;
        _prevStepBtn.Height = 38;
        _prevStepBtn.Click += (_, _) => SetStep(_stepIndex - 1);
        navFlow.Controls.Add(_nextStepBtn);
        navFlow.Controls.Add(_prevStepBtn);
        navBar.Controls.Add(navFlow);

        leftCard.Controls.Add(bodyHost);
        leftCard.Controls.Add(_wizardAlert);
        leftCard.Controls.Add(navBar);
        leftCard.Controls.Add(_stepBanner);
        leftCard.Controls.Add(progressBar);

        var sumCard = BuildStickySummaryPanel();
        layout.Controls.Add(leftCard, 0, 0);
        layout.Controls.Add(sumCard, 1, 0);
        _wizardHost.Controls.Add(layout);

        UpdatePackageCardVisualStates();
        SetStep(0);
    }

    private Panel BuildStepProgressHeader()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Color.Transparent
        };
        for (var i = 0; i < 5; i++)
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        string[] labels = ["Gói chụp", "Thời gian", "Dịch vụ", "Liên hệ", "Rà soát"];
        _stepPills.Clear();

        for (var i = 0; i < labels.Length; i++)
        {
            var idx = i;
            var pill = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, idx == 4 ? 0 : 6, 8),
                Cursor = Cursors.Hand,
                BackColor = Theme.Primitives.Alabaster100
            };
            var numLbl = new Label
            {
                Location = new Point(8, 7),
                Size = new Size(22, 22),
                Text = (idx + 1).ToString(),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Font(8.2f, FontStyle.Bold),
                BackColor = Theme.BorderStrong,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            var titleLbl = new Label
            {
                Location = new Point(35, 6),
                Size = new Size(92, 24),
                Text = labels[idx],
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.Font(8.4f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            void Jump(object? _, EventArgs __)
            {
                // Allow clicking backward anytime, or forward cleanly through validation
                if (idx <= _stepIndex) SetStep(idx);
            }
            pill.Click += Jump;
            numLbl.Click += Jump;
            titleLbl.Click += Jump;
            pill.Controls.AddRange([numLbl, titleLbl]);
            bar.Controls.Add(pill, idx, 0);
            _stepPills.Add(new StepPillRefs(idx, pill, numLbl, titleLbl));
        }

        return bar;
    }

    private CardPanel BuildStickySummaryPanel()
    {
        var sumCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = Theme.Primitives.Alabaster50,
            AccentStripeLeft = true,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var sumEyebrow = new Label
        {
            AutoSize = false,
            Size = new Size(340, 22),
            Text = "TÓM TẮT YÊU CẦU TẠM TÍNH",
            Font = Theme.MonoFont(8.0f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 0, 6)
        };

        var assetEntry = VisualAssetService.GetCoverEntry(_chosenPkg.MaGoiChup);
        _summaryThumb = new VisualMediaPictureBox(assetEntry, _chosenPkg.Gallery)
        {
            Size = new Size(340, 122),
            Margin = new Padding(0, 0, 0, 8)
        };

        _sumPkgTitle = new Label
        {
            AutoSize = false,
            Size = new Size(340, 26),
            Font = Theme.Font(11.0f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoEllipsis = true
        };
        _sumPkgMeta = new Label
        {
            AutoSize = false,
            Size = new Size(340, 22),
            Font = Theme.Font(8.4f),
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 0, 0, 10)
        };

        Panel KvRow(string caption, out Label valueLbl)
        {
            var r = new Panel { Size = new Size(340, 26), Margin = new Padding(0, 0, 0, 4) };
            var c = new Label { Dock = DockStyle.Left, Width = 118, Text = caption, UseMnemonic = false, Font = Theme.Font(8.4f), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft };
            valueLbl = new Label { Dock = DockStyle.Fill, UseMnemonic = false, Font = Theme.Font(8.7f, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true };
            r.Controls.Add(valueLbl);
            r.Controls.Add(c);
            return r;
        }

        var divider1 = new Panel { Size = new Size(340, 1), BackColor = Theme.Border, Margin = new Padding(0, 6, 0, 8) };
        var svcHeader = new Label { Size = new Size(340, 20), Text = "Dịch vụ bổ sung", Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = Theme.TextSecondary, Margin = new Padding(0, 4, 0, 4) };
        _sumExtraServicesHost = new FlowLayoutPanel { Width = 340, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        var divider2 = new Panel { Size = new Size(340, 1), BackColor = Theme.BorderStrong, Margin = new Padding(0, 6, 0, 8) };

        var totalRow = new Panel { Size = new Size(340, 32), Margin = new Padding(0, 0, 0, 4) };
        var totalCap = new Label { Dock = DockStyle.Left, Width = 148, Text = "TỔNG TẠM TÍNH", Font = Theme.Font(9.5f, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
        _sumGrandTotalVal = new Label { Dock = DockStyle.Fill, Font = Theme.Font(12.2f, FontStyle.Bold), ForeColor = Theme.Primary, TextAlign = ContentAlignment.MiddleRight };
        totalRow.Controls.Add(_sumGrandTotalVal);
        totalRow.Controls.Add(totalCap);

        _sumDepositHintVal = new Label
        {
            Size = new Size(340, 38),
            Font = Theme.Font(8.1f),
            ForeColor = Theme.TextSecondary,
            Margin = new Padding(0, 2, 0, 6)
        };

        var disclaimerCard = new CardPanel
        {
            Size = new Size(340, 56),
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            CornerRadius = 8,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 4, 0, 4)
        };
        disclaimerCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Gửi yêu cầu chưa khóa phòng tự động. Nhân sự Tiếp nhận sẽ kiểm tra trùng lịch và phản hồi xác nhận.",
            Font = Theme.Font(8.0f),
            ForeColor = Theme.WarningHover
        });

        stack.Controls.Add(sumEyebrow);
        stack.Controls.Add(_summaryThumb);
        stack.Controls.Add(_sumPkgTitle);
        stack.Controls.Add(_sumPkgMeta);
        stack.Controls.Add(KvRow("Ngày & khung giờ", out _sumDateVal));
        stack.Controls.Add(KvRow("Phòng ưu tiên", out _sumRoomVal));
        stack.Controls.Add(KvRow("Số người dự kiến", out _sumGuestVal));
        stack.Controls.Add(divider1);
        stack.Controls.Add(KvRow("Giá gói cơ bản", out _sumBasePriceVal));
        stack.Controls.Add(svcHeader);
        stack.Controls.Add(_sumExtraServicesHost);
        stack.Controls.Add(divider2);
        stack.Controls.Add(totalRow);
        stack.Controls.Add(_sumDepositHintVal);
        stack.Controls.Add(disclaimerCard);

        scroller.Controls.Add(stack);
        scroller.SizeChanged += (_, _) =>
        {
            var w = Math.Max(260, scroller.ClientSize.Width - 18);
            stack.Width = w;
            foreach (Control c in stack.Controls) c.Width = w;
        };

        sumCard.Controls.Add(scroller);
        return sumCard;
    }

    private Panel BuildStep1Package()
    {
        var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 4, 6, 4) };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        _packageCards.Clear();
        foreach (var pkg in _packages)
        {
            var card = new CardPanel
            {
                Size = new Size(280, 236),
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(12),
                Cursor = Cursors.Hand,
                CornerRadius = 10,
                AccentStripeThickness = 4
            };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent };
            var codeLbl = new Label
            {
                Dock = DockStyle.Left,
                Width = 98,
                Text = pkg.MaGoiChup,
                Font = Theme.MonoFont(7.8f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var selectedBadge = new Label
            {
                Dock = DockStyle.Right,
                Width = 104,
                Text = "✓ Đang chọn",
                Font = Theme.Font(7.8f, FontStyle.Bold),
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            topBar.Controls.Add(selectedBadge);
            topBar.Controls.Add(codeLbl);

            var media = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(pkg.MaGoiChup), pkg.Gallery)
            {
                Dock = DockStyle.Top,
                Height = 88,
                Margin = new Padding(0, 4, 0, 6)
            };

            var nameLbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = pkg.TenGoi,
                UseMnemonic = false,
                Font = Theme.Font(10.0f, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoEllipsis = true
            };

            var descLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"{pkg.MoTa}\n{pkg.Highlights}",
                UseMnemonic = false,
                Font = Theme.Font(8.1f),
                ForeColor = Theme.TextSecondary,
                AutoEllipsis = true
            };

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.Transparent };
            var priceLbl = new Label
            {
                Dock = DockStyle.Left,
                Width = 136,
                Text = $"{pkg.GiaCoBan:N0} đ • {pkg.ThoiLuongPhut}p",
                Font = Theme.Font(8.5f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var selectBtn = Theme.OutlineButton("Chọn gói");
            selectBtn.Dock = DockStyle.Right;
            selectBtn.Width = 96;
            selectBtn.Height = 30;
            priceLbl.Dock = DockStyle.Fill;

            footer.Controls.Add(priceLbl);
            footer.Controls.Add(selectBtn);

            void HandlePick(object? _, EventArgs __) => SelectPackage(pkg.MaGoiChup);

            void BindClicksRecursive(Control rootCtrl)
            {
                rootCtrl.Click += HandlePick;
                foreach (Control child in rootCtrl.Controls) BindClicksRecursive(child);
            }

            card.Controls.Add(descLbl);
            card.Controls.Add(nameLbl);
            card.Controls.Add(media);
            card.Controls.Add(topBar);
            card.Controls.Add(footer);

            BindClicksRecursive(card);
            card.TabStop = true;
            card.KeyDown += (_, e) =>
            {
                if (e.KeyCode is Keys.Enter or Keys.Space)
                {
                    SelectPackage(pkg.MaGoiChup);
                    e.Handled = true;
                }
            };

            _packageCards[pkg.MaGoiChup] = new PackageCardRefs(pkg, card, selectedBadge, selectBtn);
            flow.Controls.Add(card);
        }

        host.Controls.Add(flow);
        host.SizeChanged += (_, _) =>
        {
            var avail = Math.Max(280, host.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10);
            flow.MaximumSize = new Size(avail, 0);
            var cols = avail >= 510 ? 2 : 1;
            var cardW = Math.Max(232, (avail - cols * 14 - 6) / cols);
            foreach (var entry in _packageCards.Values)
            {
                entry.Card.Width = cardW;
            }
        };
        return host;
    }

    private void UpdatePackageCardVisualStates()
    {
        foreach (var entry in _packageCards.Values)
        {
            var isSelected = string.Equals(entry.Package.MaGoiChup, _chosenPkg.MaGoiChup, StringComparison.OrdinalIgnoreCase);
            entry.Card.BackColor = isSelected ? Theme.Primitives.Terracotta50 : Theme.Surface;
            entry.Card.BorderColor = isSelected ? Theme.Primary : Theme.BorderStrong;
            entry.Card.AccentStripeLeft = isSelected;
            entry.Card.AccentStripeColor = Theme.Primary;
            entry.SelectedBadge.Visible = isSelected;
            entry.SelectBtn.Text = isSelected ? "Đang chọn" : "Chọn gói";
            entry.SelectBtn.BorderColor = isSelected ? Theme.Primary : Theme.BorderStrong;
            entry.SelectBtn.ForeColor = isSelected ? Theme.Primary : Theme.TextSecondary;
            entry.Card.Invalidate();
        }
    }

    private Panel BuildStep2TimeRoom()
    {
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(4) };
        var notice = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            AccentStripeColor = Theme.Warning,
            AccentStripeLeft = true,
            AccentStripeThickness = 4,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 12)
        };
        notice.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Lưu ý minh bạch: Ngày giờ bạn chọn là khung giờ mong muốn. Hệ thống KHÔNG khóa lịch tự động; nhân sự Tiếp nhận sẽ kiểm tra trùng phòng & nhiếp ảnh gia trước khi xác nhận.",
            Font = Theme.Font(8.5f),
            ForeColor = Theme.WarningHover
        });

        var form = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        form.Controls.Add(WrapRow("Ngày chụp mong muốn", _datePicker));
        form.Controls.Add(WrapRow("Khung giờ mong muốn (tính theo thời lượng gói)", _slotBox));
        _durationHintLbl.Margin = new Padding(0, 0, 0, 10);
        form.Controls.Add(_durationHintLbl);
        form.Controls.Add(WrapRow("Không gian phòng ưu tiên", _roomPrefBox));
        form.Controls.Add(WrapRow("Số người dự kiến tham gia", _guestCount));

        _datePicker.ValueChanged += (_, _) => RefreshSummary();
        _slotBox.SelectedIndexChanged += (_, _) => RefreshSummary();
        _roomPrefBox.SelectedIndexChanged += (_, _) => RefreshSummary();
        _guestCount.ValueChanged += (_, _) => RefreshSummary();

        p.Controls.Add(form);
        p.Controls.Add(notice);
        return p;
    }

    private Panel BuildStep3Services()
    {
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(2) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        foreach (var svc in _services)
        {
            var rowCard = new CardPanel
            {
                Size = new Size(580, 56),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Theme.Primitives.Alabaster50,
                BorderColor = Theme.Border,
                Padding = new Padding(12, 8, 12, 8)
            };

            var infoPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var titleLbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = $"{svc.TenDichVu}  •  {svc.DonGia:N0} đ / {svc.DonViTinh}",
                Font = Theme.Font(9.1f, FontStyle.Bold),
                ForeColor = Theme.Text,
                AutoEllipsis = true
            };
            var descLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = svc.MoTa,
                Font = Theme.Font(8.1f),
                ForeColor = Theme.Muted,
                AutoEllipsis = true
            };
            infoPanel.Controls.Add(descLbl);
            infoPanel.Controls.Add(titleLbl);

            var qtyHost = new Panel { Dock = DockStyle.Right, Width = 110, BackColor = Color.Transparent };
            var qty = new NumericUpDown { Location = new Point(22, 8), Width = 78, Minimum = 0, Maximum = 20, Value = 0, Font = Theme.Font(9.5f, FontStyle.Bold) };
            qty.ValueChanged += (_, _) => RefreshSummary();
            _serviceQtyBoxes[svc.MaDichVu] = qty;
            qtyHost.Controls.Add(qty);

            rowCard.Controls.Add(infoPanel);
            rowCard.Controls.Add(qtyHost);
            flow.Controls.Add(rowCard);
        }

        flow.Controls.Add(WrapRow("Ghi chú concept / trang phục mong muốn", _notesBox, 108));
        p.Controls.Add(flow);
        p.SizeChanged += (_, _) =>
        {
            var w = Math.Max(360, p.ClientSize.Width - 26);
            foreach (Control c in flow.Controls) c.Width = w;
        };
        return p;
    }

    private Panel BuildStep4Contact()
    {
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.Add(WrapRow("Họ và tên người đặt lịch *", _contactName));
        flow.Controls.Add(WrapRow("Số điện thoại liên lạc (10 chữ số bắt đầu 0) *", _contactPhone));
        flow.Controls.Add(WrapRow("Địa chỉ Email nhận phản hồi *", _contactEmail));

        var privacyNote = new CardPanel
        {
            Size = new Size(560, 56),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.Border,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 6, 0, 0)
        };
        privacyNote.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Thông tin liên hệ được gắn trực tiếp với hồ sơ tài khoản khách hàng đang đăng nhập và chỉ nhân sự phụ trách lịch mới được xem.",
            Font = Theme.Font(8.3f),
            ForeColor = Theme.TextSecondary
        });
        flow.Controls.Add(privacyNote);
        p.Controls.Add(flow);
        return p;
    }

    private Panel BuildStep5Review()
    {
        var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(2) };
        _step5ReviewHost = new Panel { Dock = DockStyle.Top, AutoSize = true };
        _step5ReviewHost.SizeChanged += (_, _) => ResizeStep5Cards();
        p.Controls.Add(_step5ReviewHost);
        RebuildStep5SummaryCard();
        return p;
    }

    private void ResizeStep5Cards()
    {
        if (_step5ReviewHost.Controls.Count == 0 || _step5ReviewHost.Controls[0] is not FlowLayoutPanel stack)
            return;
        var w = Math.Max(340, _step5ReviewHost.ClientSize.Width - 18);
        stack.Width = w;
        foreach (Control card in stack.Controls)
        {
            card.Width = w;
        }
    }

    private void RebuildStep5SummaryCard()
    {
        if (_step5ReviewHost is null) return;
        _step5ReviewHost.Controls.Clear();

        var hostW = Math.Max(360, _step5ReviewHost.ClientSize.Width - 18);
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = hostW
        };

        var lines = BuildSelectedServiceLines();
        var grand = _chosenPkg.GiaCoBan + lines.Sum(s => s.ThanhTien);
        var svcSummary = lines.Count == 0
            ? "Không có"
            : string.Join(", ", lines.Select(s => $"{s.TenDichVu} ×{s.SoLuong}"));

        var bodyText =
            $"• Gói chụp: {_chosenPkg.TenGoi} ({_chosenPkg.ThoiLuongPhut} phút) — {_chosenPkg.GiaCoBan:N0} đ\n" +
            $"• Thời gian đề xuất: {_datePicker.Value:dd/MM/yyyy} • {_slotBox.SelectedItem}\n" +
            $"• Phòng ưu tiên: {_roomPrefBox.SelectedItem} • {_guestCount.Value} người\n" +
            $"• Dịch vụ thêm: {svcSummary}\n" +
            $"• Khách liên hệ: {_contactName.Text.Trim()} • {_contactPhone.Text.Trim()}\n" +
            $"• Tổng tạm tính: {grand:N0} đ (Cọc gợi ý 30% = {(grand * 0.3m):N0} đ | 50% = {(grand * 0.5m):N0} đ)";

        var bodyFont = Theme.Font(8.9f);
        var measuredBodyH = TextRenderer.MeasureText(
            bodyText,
            bodyFont,
            new Size(Math.Max(280, hostW - 36), 0),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

        var confirmCard = new CardPanel
        {
            Size = new Size(hostW, Math.Max(192, measuredBodyH + 68)),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 10)
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = "Xác nhận thông tin trước khi gửi yêu cầu",
            UseMnemonic = false,
            Font = Theme.Font(10.0f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var body = new Label
        {
            Dock = DockStyle.Fill,
            Font = bodyFont,
            ForeColor = Theme.TextSecondary,
            UseMnemonic = false,
            Text = bodyText
        };
        confirmCard.Controls.Add(body);
        confirmCard.Controls.Add(title);

        var ruleText =
            "• Sau khi gửi, mã yêu cầu REQ-YYYYMM-xxx xuất hiện ngay tại mục 'Lịch sử yêu cầu đã gửi'.\n" +
            "• Nhân sự Tiếp nhận sẽ kiểm tra trùng phòng/nhiếp ảnh gia trong cùng phiên Demo trước khi chuyển thành Lịch chụp chính thức.";
        var ruleFont = Theme.Font(8.6f);
        var measuredRuleH = TextRenderer.MeasureText(
            ruleText,
            ruleFont,
            new Size(Math.Max(280, hostW - 36), 0),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

        var ruleCard = new CardPanel
        {
            Size = new Size(hostW, Math.Max(92, measuredRuleH + 28)),
            BackColor = Theme.SuccessLight,
            BorderColor = Theme.SuccessBorder,
            AccentStripeLeft = true,
            AccentStripeColor = Theme.Success,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 10, 14, 10)
        };
        ruleCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = ruleFont,
            ForeColor = Theme.SuccessHover,
            UseMnemonic = false,
            Text = ruleText
        });

        stack.Controls.Add(confirmCard);
        stack.Controls.Add(ruleCard);
        _step5ReviewHost.Controls.Add(stack);
    }

    private static Panel WrapRow(string label, Control c, int height = 64)
    {
        var p = new Panel { Size = new Size(560, height), Margin = new Padding(0, 0, 0, 8) };
        var l = new Label { Dock = DockStyle.Top, Height = 22, Text = label, Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
        c.Margin = Padding.Empty;
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 0) };
        host.Controls.Add(c);
        p.Controls.Add(host);
        p.Controls.Add(l);
        return p;
    }

    private void SetStep(int idx)
    {
        _stepIndex = Math.Clamp(idx, 0, _stepPanels.Count - 1);
        _wizardAlert.Clear();
        for (var i = 0; i < _stepPanels.Count; i++)
            _stepPanels[i].Visible = i == _stepIndex;

        string[] titles =
        [
            "Bước 1/5 — Chọn gói chụp nền tảng",
            "Bước 2/5 — Chọn thời gian mong muốn & phòng ưu tiên",
            "Bước 3/5 — Dịch vụ bổ sung & Ghi chú concept",
            "Bước 4/5 — Thông tin liên hệ khách hàng",
            "Bước 5/5 — Rà soát & Gửi yêu cầu đặt lịch (Demo)"
        ];
        _stepBanner.Text = titles[_stepIndex];
        _prevStepBtn.Visible = _stepIndex > 0;
        _nextStepBtn.Text = _stepIndex == _stepPanels.Count - 1 ? "Gửi yêu cầu đặt lịch (Demo)" : "Tiếp tục";

        foreach (var p in _stepPills)
        {
            var active = p.StepIdx == _stepIndex;
            var done = p.StepIdx < _stepIndex;
            p.Pill.BackColor = active ? Theme.Primitives.Terracotta50 : done ? Theme.SuccessLight : Theme.Primitives.Alabaster100;
            p.NumLbl.BackColor = active ? Theme.Primary : done ? Theme.Success : Theme.BorderStrong;
            p.NumLbl.Text = done ? "✓" : (p.StepIdx + 1).ToString();
            p.TitleLbl.ForeColor = active ? Theme.Primary : done ? Theme.SuccessHover : Theme.TextSecondary;
        }

        if (_stepIndex == 4) RebuildStep5SummaryCard();
        RefreshSummary();
    }

    private void OnNextOrSubmit()
    {
        _wizardAlert.Clear();
        if (_stepIndex == 3)
        {
            if (string.IsNullOrWhiteSpace(_contactName.Text))
            {
                _wizardAlert.ShowMessage(AtelierNotificationKind.Error, "Thiếu họ tên liên hệ", "Vui lòng nhập họ và tên người đặt lịch trước khi tiếp tục.");
                return;
            }
            if (!Regex.IsMatch(_contactPhone.Text.Trim(), @"^0\d{9}$"))
            {
                _wizardAlert.ShowMessage(AtelierNotificationKind.Error, "Số điện thoại chưa hợp lệ", "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.");
                return;
            }
            if (!Regex.IsMatch(_contactEmail.Text.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                _wizardAlert.ShowMessage(AtelierNotificationKind.Error, "Email chưa đúng định dạng", "Vui lòng kiểm tra lại địa chỉ email nhận phản hồi từ studio.");
                return;
            }
        }

        if (_stepIndex < _stepPanels.Count - 1)
        {
            SetStep(_stepIndex + 1);
            return;
        }

        var selectedServices = BuildSelectedServiceLines();
        var roomSel = _roomPrefBox.SelectedItem?.ToString() ?? "";
        var roomObj = _rooms.FirstOrDefault(r => roomSel.StartsWith(r.MaPhong, StringComparison.OrdinalIgnoreCase));

        var code = DesktopDemoSessionStore.NextRequestCode();
        var grandTotal = _chosenPkg.GiaCoBan + selectedServices.Sum(s => s.ThanhTien);
        var dto = new CustomerBookingRequestDto
        {
            MaYeuCau = code,
            DemoCustomerId = _session.Profile.DemoAccountId,
            TenKhachHang = _contactName.Text.Trim(),
            SoDienThoai = _contactPhone.Text.Trim(),
            Email = _contactEmail.Text.Trim(),
            MaGoiChup = _chosenPkg.MaGoiChup,
            TenGoi = _chosenPkg.TenGoi,
            NgayChupMongMuon = _datePicker.Value.Date,
            KhungGioMongMuon = _slotBox.SelectedItem?.ToString() ?? "09:00 - 11:00",
            ThoiLuongPhut = _chosenPkg.ThoiLuongPhut,
            MaPhongUuTien = roomObj?.MaPhong,
            TenPhongUuTien = roomObj?.TenPhong ?? "Nhờ studio tư vấn",
            SoNguoiDuKien = (int)_guestCount.Value,
            ExtraServices = selectedServices,
            TamTinhGoi = _chosenPkg.GiaCoBan,
            GhiChuKhachHang = _notesBox.Text.Trim(),
            TrangThai = BookingRequestStatus.CHO_XU_LY,
            PhanHoiChoKhach = "Đã gửi yêu cầu trong phiên Demo. Chờ bộ phận Tiếp nhận kiểm tra lịch phòng & nhiếp ảnh gia.",
            CreatedAtLabel = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
            Timeline =
            [
                new(DateTime.Now.ToString("dd/MM/yyyy HH:mm"), _contactName.Text.Trim(), "Gửi yêu cầu đặt lịch mới", $"Gói {_chosenPkg.TenGoi} • Tổng tạm tính {grandTotal:N0} đ", BookingRequestStatus.CHO_XU_LY)
            ]
        };

        DesktopDemoSessionStore.SubmitCustomerBookingRequest(dto);
        _nav.RefreshBadgeCounts();

        AtelierNotificationService.ShowSuccess(
            FindForm(),
            $"Yêu cầu đặt lịch {code} đã được tiếp nhận ở trạng thái Chờ xử lý (Chế độ Demo).\nNhân sự Tiếp nhận sẽ kiểm tra trùng phòng và phản hồi trên tài khoản của bạn.",
            "Đã gửi yêu cầu đặt lịch");

        _modeTabs.SelectByKey("history");
        _wizardHost.Visible = false;
        _historyHost.Visible = true;
        ReloadHistory(code);
    }

    private List<BookingRequestServiceLineDto> BuildSelectedServiceLines()
    {
        var list = new List<BookingRequestServiceLineDto>();
        foreach (var svc in _services)
        {
            if (_serviceQtyBoxes.TryGetValue(svc.MaDichVu, out var num) && num.Value > 0)
                list.Add(new BookingRequestServiceLineDto(svc.MaDichVu, svc.TenDichVu, svc.DonViTinh, svc.DonGia, (int)num.Value));
        }
        return list;
    }

    private void RefreshSummary()
    {
        var lines = BuildSelectedServiceLines();
        var svcTotal = lines.Sum(x => x.ThanhTien);
        var grand = _chosenPkg.GiaCoBan + svcTotal;

        _summaryThumb.BindAsset(VisualAssetService.GetCoverEntry(_chosenPkg.MaGoiChup), _chosenPkg.Gallery);
        _sumPkgTitle.Text = $"{_chosenPkg.TenGoi} ({_chosenPkg.MaGoiChup})";
        _sumPkgMeta.Text = $"Thời lượng {_chosenPkg.ThoiLuongPhut} phút • Gợi ý phòng {_chosenPkg.SuggestedRoomCode}";
        _sumDateVal.Text = $"{_datePicker.Value:dd/MM/yyyy} ({_slotBox.SelectedItem})";
        _sumRoomVal.Text = _roomPrefBox.SelectedItem?.ToString() ?? "Nhờ studio tư vấn";
        _sumGuestVal.Text = $"{_guestCount.Value} khách";
        _sumBasePriceVal.Text = $"{_chosenPkg.GiaCoBan:N0} đ";
        _sumGrandTotalVal.Text = $"{grand:N0} đ";
        _sumDepositHintVal.Text = $"Đề xuất cọc tham khảo: 30% ({(grand * 0.3m):N0} đ) hoặc 50% ({(grand * 0.5m):N0} đ).";

        _sumExtraServicesHost.Controls.Clear();
        if (lines.Count == 0)
        {
            _sumExtraServicesHost.Controls.Add(new Label
            {
                Size = new Size(340, 22),
                Text = "Chưa chọn dịch vụ thêm",
                Font = Theme.Font(8.3f, FontStyle.Italic),
                ForeColor = Theme.Muted
            });
        }
        else
        {
            foreach (var item in lines)
            {
                var row = new Panel { Size = new Size(340, 22), Margin = new Padding(0, 0, 0, 2) };
                row.Controls.Add(new Label
                {
                    Dock = DockStyle.Right,
                    Width = 105,
                    Text = $"{item.ThanhTien:N0} đ",
                    Font = Theme.Font(8.3f, FontStyle.Bold),
                    ForeColor = Theme.Text,
                    TextAlign = ContentAlignment.MiddleRight
                });
                row.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = $"{item.TenDichVu} ×{item.SoLuong}",
                    Font = Theme.Font(8.3f),
                    ForeColor = Theme.TextSecondary,
                    AutoEllipsis = true
                });
                _sumExtraServicesHost.Controls.Add(row);
            }
        }
    }

    private void BuildHistoryView()
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14) };

        var leftTop = new Panel { Dock = DockStyle.Top, Height = 72 };
        var filterBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = true };
        void AddFilter(string label, string code)
        {
            var b = Theme.OutlineButton(label);
            b.Height = 30;
            b.Width = Math.Max(82, b.RequiredWidth);
            b.Click += (_, _) =>
            {
                _statusFilter = code;
                UpdateFilterStyles();
                ReloadHistory();
            };
            _filterButtons[code] = b;
            filterBar.Controls.Add(b);
        }
        AddFilter("Tất cả", "ALL");
        AddFilter("Chờ xử lý", nameof(BookingRequestStatus.CHO_XU_LY));
        AddFilter("Cần bổ sung", nameof(BookingRequestStatus.CAN_BO_SUNG));
        AddFilter("Đã xác nhận", nameof(BookingRequestStatus.DA_XAC_NHAN));

        _historyCountBadge.Dock = DockStyle.Bottom;
        _historyCountBadge.Height = 24;
        leftTop.Controls.Add(_historyCountBadge);
        leftTop.Controls.Add(filterBar);

        _historyGrid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _historyGrid.DefaultCellStyle.Font = Theme.Font(8.4f);
        _historyGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã YC", FillWeight = 31f });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Gói & Ngày", FillWeight = 43f });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", FillWeight = 26f });
        _historyGrid.SelectionChanged += (_, _) => RenderSelectedHistoryDossier();

        leftCard.Controls.Add(_historyGrid);
        leftCard.Controls.Add(leftTop);

        var rightCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(16) };
        var actionFlow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        _cancelReqBtn.Width = 158;
        _cancelReqBtn.Click += (_, _) => CancelSelectedRequest();
        _payReqBtn.Width = 242;
        _payReqBtn.Click += (_, _) =>
        {
            var req = GetSelectedHistoryRequest();
            if (req is not null)
                _nav.NavigateTo("payments", string.IsNullOrWhiteSpace(req.LinkedBookingCode) ? req.MaYeuCau : req.LinkedBookingCode);
        };
        actionFlow.Controls.Add(_cancelReqBtn);
        actionFlow.Controls.Add(_payReqBtn);

        rightCard.Controls.Add(_historyDetailHost);
        rightCard.Controls.Add(actionFlow);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        _historyHost.Controls.Add(split);
        UpdateFilterStyles();
    }

    private void UpdateFilterStyles()
    {
        foreach (var kv in _filterButtons)
        {
            var active = kv.Key == _statusFilter;
            kv.Value.BorderColor = active ? Theme.Primary : Theme.BorderStrong;
            kv.Value.ForeColor = active ? Theme.Primary : Theme.TextSecondary;
            kv.Value.BackColor = active ? Theme.Primitives.Terracotta50 : Theme.Surface;
        }
    }

    private CustomerBookingRequestDto? GetSelectedHistoryRequest()
    {
        if (_historyGrid.CurrentRow is null) return null;
        var idx = _historyGrid.CurrentRow.Index;
        return idx >= 0 && idx < _visibleRequests.Count ? _visibleRequests[idx] : null;
    }

    private void ReloadHistory(string? focusMaYeuCau = null)
    {
        var all = DesktopDemoSessionStore.GetCustomerBookingRequests(_session.Profile.DemoAccountId);
        _visibleRequests = _statusFilter == "ALL"
            ? all.ToList()
            : all.Where(x => x.TrangThai.ToString() == _statusFilter).ToList();

        _historyCountBadge.Text = $"Hiển thị {_visibleRequests.Count} / {all.Count} yêu cầu của tài khoản {_session.Profile.FullName}";
        _historyGrid.Rows.Clear();
        foreach (var r in _visibleRequests)
        {
            _historyGrid.Rows.Add(
                r.MaYeuCau,
                $"{r.TenGoi} ({r.NgayChupMongMuon:dd/MM})",
                DesktopDemoSessionStore.GetStatusVietnameseLabel(r.TrangThai));
        }

        if (_visibleRequests.Count > 0)
        {
            var targetIdx = 0;
            if (!string.IsNullOrWhiteSpace(focusMaYeuCau))
            {
                var found = _visibleRequests.FindIndex(x => x.MaYeuCau == focusMaYeuCau);
                if (found >= 0) targetIdx = found;
            }
            _historyGrid.Rows[targetIdx].Selected = true;
            _historyGrid.CurrentCell = _historyGrid.Rows[targetIdx].Cells[0];
        }
        RenderSelectedHistoryDossier();
    }

    private void RenderSelectedHistoryDossier()
    {
        _historyDetailHost.Controls.Clear();
        var r = GetSelectedHistoryRequest();
        if (r is null)
        {
            var empty = new CardPanel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(16), BackColor = Theme.Primitives.Alabaster50 };
            empty.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Chưa có yêu cầu đặt lịch nào khớp bộ lọc hiện tại.\nBạn có thể chuyển sang tab 'Tạo yêu cầu đặt lịch' để đề xuất lịch chụp mới.",
                Font = Theme.Font(9.2f),
                ForeColor = Theme.Muted
            });
            _historyDetailHost.Controls.Add(empty);
            _cancelReqBtn.Enabled = false;
            _payReqBtn.Enabled = false;
            return;
        }

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = Math.Max(360, _historyDetailHost.ClientSize.Width - 20)
        };

        var statusColor = r.TrangThai switch
        {
            BookingRequestStatus.DA_XAC_NHAN => Theme.Success,
            BookingRequestStatus.CAN_BO_SUNG => Theme.Warning,
            BookingRequestStatus.DA_TU_CHOI or BookingRequestStatus.KHACH_HUY => Theme.Danger,
            _ => Theme.Primary
        };

        var headerCard = new CardPanel
        {
            Size = new Size(stack.Width, 84),
            AccentStripeLeft = true,
            AccentStripeColor = statusColor,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        var hTop = new Panel { Dock = DockStyle.Top, Height = 26 };
        hTop.Controls.Add(new Label { Dock = DockStyle.Right, Width = 148, Text = DesktopDemoSessionStore.GetStatusVietnameseLabel(r.TrangThai), Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = statusColor, TextAlign = ContentAlignment.MiddleRight });
        hTop.Controls.Add(new Label { Dock = DockStyle.Fill, Text = $"{r.MaYeuCau} • {r.TenGoi}", Font = Theme.Font(10.8f, FontStyle.Bold), ForeColor = Theme.Text });
        var hSub = new Label { Dock = DockStyle.Fill, Text = $"Gửi lúc {r.CreatedAtLabel} • Tổng tạm tính {r.TongTamTinh:N0} đ", Font = Theme.Font(8.6f), ForeColor = Theme.Muted };
        headerCard.Controls.Add(hSub);
        headerCard.Controls.Add(hTop);

        var infoCard = new CardPanel
        {
            Size = new Size(stack.Width, 128),
            BackColor = Theme.Primitives.Alabaster50,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        infoCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = Theme.Font(8.9f),
            ForeColor = Theme.Text,
            Text =
                $"Thời gian mong muốn:  {r.NgayChupMongMuon:dd/MM/yyyy} ({r.KhungGioMongMuon}) • {r.ThoiLuongPhut} phút\n" +
                $"Phòng / điều phối:    {r.AssignedRoomName ?? r.TenPhongUuTien ?? "Studio đề xuất"}\n" +
                $"Nhiếp ảnh gia gợi ý:  {r.SuggestedPhotographerName ?? "Chờ phân công"}\n" +
                $"Lịch chụp liên kết:   {r.LinkedBookingCode ?? "Chưa chuyển thành lịch chính thức"}\n" +
                $"Dịch vụ thêm:         {(r.ExtraServices.Count == 0 ? "Không" : string.Join(", ", r.ExtraServices.Select(s => $"{s.TenDichVu} ×{s.SoLuong}")))}"
        });

        var feedbackCard = new CardPanel
        {
            Size = new Size(stack.Width, 84),
            BackColor = r.TrangThai == BookingRequestStatus.CAN_BO_SUNG ? Theme.WarningLight : Theme.Surface,
            BorderColor = r.TrangThai == BookingRequestStatus.CAN_BO_SUNG ? Theme.WarningBorder : Theme.BorderStrong,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        feedbackCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = Theme.Font(8.8f),
            ForeColor = Theme.TextSecondary,
            Text = string.IsNullOrWhiteSpace(r.PhanHoiChoKhach) ? "Chưa có phản hồi mới từ bộ phận Tiếp nhận." : r.PhanHoiChoKhach
        });
        feedbackCard.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "PHẢN HỒI TỪ STUDIO DÀNH CHO BẠN",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Muted
        });

        var timelineTitle = new Label
        {
            Size = new Size(stack.Width, 22),
            Text = "NHẬT KÝ TIẾN TRÌNH XỬ LÝ",
            Font = Theme.MonoFont(7.8f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            Margin = new Padding(2, 2, 0, 6)
        };

        stack.Controls.Add(headerCard);
        stack.Controls.Add(infoCard);
        stack.Controls.Add(feedbackCard);
        stack.Controls.Add(timelineTitle);

        foreach (var t in r.Timeline)
        {
            var tCard = new CardPanel
            {
                Size = new Size(stack.Width, 58),
                BackColor = Theme.Surface,
                BorderColor = Theme.Border,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 0, 0, 6)
            };
            tCard.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = t.Detail,
                Font = Theme.Font(8.4f),
                ForeColor = Theme.TextSecondary,
                AutoEllipsis = true
            });
            tCard.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = $"{t.TimestampLabel} • {t.ActorLabel} — {t.ActionLabel}",
                Font = Theme.Font(8.3f, FontStyle.Bold),
                ForeColor = Theme.Text
            });
            stack.Controls.Add(tCard);
        }

        _historyDetailHost.Controls.Add(stack);

        _cancelReqBtn.Enabled = r.TrangThai is BookingRequestStatus.CHO_XU_LY or BookingRequestStatus.CAN_BO_SUNG;
        _payReqBtn.Enabled = r.TrangThai is BookingRequestStatus.CHO_XU_LY or BookingRequestStatus.CAN_BO_SUNG or BookingRequestStatus.DA_XAC_NHAN;
    }

    private void CancelSelectedRequest()
    {
        var req = GetSelectedHistoryRequest();
        if (req is null) return;

        var confirmed = AtelierNotificationService.Confirm(
            FindForm(),
            "Xác nhận hủy yêu cầu đặt lịch",
            $"Bạn chắc chắn muốn hủy yêu cầu {req.MaYeuCau} ({req.TenGoi} ngày {req.NgayChupMongMuon:dd/MM/yyyy})?\nThao tác này không thể hoàn tác.",
            confirmText: "Hủy yêu cầu này",
            cancelText: "Giữ lại yêu cầu",
            isDestructive: true);
        if (!confirmed) return;

        if (DesktopDemoSessionStore.CancelRequestByCustomer(req.MaYeuCau, "Khách hàng xác nhận hủy từ Cổng Khách hàng (Demo)"))
        {
            ReloadHistory(req.MaYeuCau);
            _nav.RefreshBadgeCounts();
            AtelierNotificationService.ShowToast(
                FindForm(),
                AtelierNotificationKind.Warning,
                "Đã hủy yêu cầu đặt lịch",
                $"Yêu cầu {req.MaYeuCau} đã chuyển sang trạng thái Khách đã hủy.");
        }
    }
}
