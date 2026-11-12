using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerBookingsPage : UserControl
{
    private readonly CustomerSessionContext _session;
    private readonly ICustomerNavigator _nav;
    private readonly List<CustomerMockBookingDto> _bookings;
    private CustomerMockBookingDto? _selected;

    private readonly DataGridView _bookingGrid = Theme.Grid();
    private VisualMediaPictureBox _roomCover = null!;
    private readonly RoleBadgePill _photographerPill = new();
    private readonly Label _bookingHeader = new() { Dock = DockStyle.Top, Height = 52, Font = Theme.Font(11.4f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
    private readonly TableLayoutPanel _pipelineStrip = new() { Dock = DockStyle.Top, Height = 56, ColumnCount = 6, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };
    private readonly Panel _detailCardsHost = new() { Dock = DockStyle.Fill, AutoScroll = true };

    public CustomerBookingsPage(CustomerSessionContext session, ICustomerNavigator nav)
    {
        _session = session;
        _nav = nav;
        _bookings = DesktopDemoSessionStore.GetCustomerBookings(session.Profile).ToList();
        _selected = _bookings.FirstOrDefault();

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Lịch chụp của tôi (Demo)",
            "Theo dõi tiến độ buổi chụp, phòng được phân bổ, nhiếp ảnh gia và tình trạng thanh toán");

        if (_bookings.Count == 0)
        {
            var empty = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28),
                AccentStripeColor = Theme.Warning,
                AccentStripeLeft = true,
                AccentStripeThickness = 4
            };
            var msg = new Label
            {
                Dock = DockStyle.Top,
                Height = 144,
                Text =
                    $"Chưa tìm thấy lịch chụp chính thức thuộc tài khoản '{_session.Profile.FullName}'.\r\n\r\n" +
                    $"Trạng thái hồ sơ hiện tại: {_session.Profile.LinkStatusHeadline}.\r\n" +
                    "• Để bảo vệ quyền riêng tư, lịch sử chụp cũ chỉ hiển thị sau khi được xác minh quyền sở hữu.\r\n" +
                    "• Khi bạn gửi Yêu cầu đặt lịch mới và được Tiếp nhận xác nhận, lịch chụp mới sẽ tự động hiển thị tại đây.",
                Font = Theme.Font(10.0f),
                ForeColor = Theme.TextSecondary
            };
            var btnBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, WrapContents = false };
            var goWizard = Theme.Button("Tạo yêu cầu đặt lịch mới", Theme.Primary, IconKind.Plus);
            goWizard.Width = 220;
            goWizard.Click += (_, _) => _nav.NavigateTo("wizard");
            var goProfile = Theme.OutlineButton("Đi tới Hồ sơ & Xác minh", Theme.Primitives.Sage600, IconKind.Shield);
            goProfile.Width = 220;
            goProfile.Click += (_, _) => _nav.NavigateTo("profile");
            btnBar.Controls.AddRange([goWizard, goProfile]);

            empty.Controls.Add(btnBar);
            empty.Controls.Add(msg);
            Controls.Add(empty);
            Controls.Add(header);
            return;
        }

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14) };
        _bookingGrid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _bookingGrid.DefaultCellStyle.Font = Theme.Font(8.4f);
        _bookingGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _bookingGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã lịch", FillWeight = 40f });
        _bookingGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Gói & Ngày", FillWeight = 33f });
        _bookingGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", FillWeight = 27f });
        _bookingGrid.SelectionChanged += (_, _) =>
        {
            if (_bookingGrid.CurrentRow is null) return;
            var idx = _bookingGrid.CurrentRow.Index;
            if (idx >= 0 && idx < _bookings.Count)
                BindBooking(_bookings[idx]);
        };

        leftCard.Controls.Add(_bookingGrid);
        leftCard.Controls.Add(new Label { Dock = DockStyle.Top, Height = 26, Text = "DANH SÁCH LỊCH CHỤP CỦA TÔI", Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Muted });

        var rightCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(16) };
        var rGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        rGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 166));
        rGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        rGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        rGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _roomCover = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(_selected!.MaPhong), VisualAssetService.GetGallery(_selected.MaPhong))
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        var topMeta = new Panel { Dock = DockStyle.Fill };
        var photoHost = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34, WrapContents = false };
        _photographerPill.SetRole(RoleVisualResolver.ForPhotographer());
        photoHost.Controls.Add(new Label { AutoSize = true, Text = "Phụ trách:", Font = Theme.Font(8.8f, FontStyle.Bold), ForeColor = Theme.TextSecondary, Padding = new Padding(0, 6, 6, 0) });
        photoHost.Controls.Add(_photographerPill);
        topMeta.Controls.Add(photoHost);
        topMeta.Controls.Add(_bookingHeader);

        for (var i = 0; i < 6; i++)
            _pipelineStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));
        _pipelineStrip.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var detailWrap = new Panel { Dock = DockStyle.Fill };
        detailWrap.Controls.Add(_detailCardsHost);
        detailWrap.Controls.Add(_pipelineStrip);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        var payBtn = Theme.Button("Thanh toán / Xem VietQR", Theme.Primitives.Sage600, IconKind.Wallet);
        payBtn.Width = 216;
        payBtn.Click += (_, _) => { if (_selected is not null) _nav.NavigateTo("payments", _selected.MaLichChup); };
        var albumBtn = Theme.OutlineButton("Mở Album ảnh", Theme.Primary, IconKind.Layers);
        albumBtn.Width = 148;
        albumBtn.Click += (_, _) => _nav.NavigateTo("albums");
        actions.Controls.Add(payBtn);
        actions.Controls.Add(albumBtn);

        rGrid.Controls.Add(_roomCover, 0, 0);
        rGrid.Controls.Add(topMeta, 0, 1);
        rGrid.Controls.Add(detailWrap, 0, 2);
        rGrid.Controls.Add(actions, 0, 3);
        rightCard.Controls.Add(rGrid);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        Controls.Add(split);
        Controls.Add(header);

        foreach (var b in _bookings)
        {
            _bookingGrid.Rows.Add(
                b.MaLichChup,
                $"{b.TenGoi} ({b.NgayChup:dd/MM})",
                b.TrangThaiLich);
        }
        _bookingGrid.Rows[0].Selected = true;
        _bookingGrid.CurrentCell = _bookingGrid.Rows[0].Cells[0];
        BindBooking(_bookings[0]);
    }

    private void BindBooking(CustomerMockBookingDto b)
    {
        _selected = b;
        _roomCover.BindAsset(VisualAssetService.GetCoverEntry(b.MaPhong), VisualAssetService.GetGallery(b.MaPhong));
        _bookingHeader.Text = $"{b.MaLichChup} — {b.TenGoi}\r\n{b.NgayChup:dd/MM/yyyy} ({b.KhungGio}) • {b.TenPhong} ({b.MaPhong})";
        _photographerPill.SetRole(RoleVisualResolver.ForPhotographer($"{b.TenNhiepAnhGia} ({b.MaNhiepAnhGia})"));

        // Visual 6-stage pipeline
        _pipelineStrip.Controls.Clear();
        string[] stages = ["Đặt lịch", "Đã cọc", "Chuẩn bị", "Đang chụp", "Hậu kỳ", "Bàn giao"];
        for (var i = 0; i < stages.Length; i++)
        {
            var done = i <= b.PipelineStepIndex;
            var current = i == b.PipelineStepIndex;
            var cell = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, i == 5 ? 0 : 4, 6),
                BackColor = current ? Theme.PrimaryLight : done ? Theme.SuccessLight : Theme.Primitives.Alabaster100
            };
            cell.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = (done ? "✓ " : $"{i + 1}. ") + stages[i],
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Font(7.7f, done ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = current ? Theme.Primary : done ? Theme.SuccessHover : Theme.Muted
            });
            _pipelineStrip.Controls.Add(cell, i, 0);
        }

        _detailCardsHost.Controls.Clear();
        var w = Math.Max(340, _detailCardsHost.ClientSize.Width - 20);
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Width = w, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        var svcs = b.SelectedServices.Count == 0
            ? "Không có dịch vụ bổ sung"
            : string.Join(", ", b.SelectedServices.Select(s => $"{s.TenDichVu} ×{s.SoLuong}"));

        var paymentCard = new CardPanel
        {
            Size = new Size(w, 84),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 8)
        };
        paymentCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            UseMnemonic = false,
            Font = Theme.Font(8.8f),
            ForeColor = Theme.Text,
            Text =
                $"Tổng: {b.TongGiaTri:N0} đ  •  Đã thu: {b.DaThanhToan:N0} đ  •  Còn lại: {b.ConLai:N0} đ\n" +
                $"Dịch vụ kèm:  {svcs}"
        });
        paymentCard.Controls.Add(new Label { Dock = DockStyle.Top, Height = 18, Text = "TÀI CHÍNH & DỊCH VỤ ĐÍNH KÈM", UseMnemonic = false, Font = Theme.MonoFont(7.4f, FontStyle.Bold), ForeColor = Theme.Muted });

        var infoCard = new CardPanel
        {
            Size = new Size(w, 108),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(12, 8, 12, 8)
        };
        infoCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            UseMnemonic = false,
            Font = Theme.Font(8.8f),
            ForeColor = Theme.TextSecondary,
            Text =
                $"Trạng thái lịch:  {b.TrangThaiLich}\n" +
                $"Nguồn tạo lịch:  {b.MaYeuCauGoc ?? "Đặt trực tiếp tại quầy"}\n" +
                $"Ghi chú lịch:     {b.GhiChu}\n" +
                $"Bàn giao ảnh:    {b.DeliveryStageNote}"
        });

        stack.Controls.Add(paymentCard);
        stack.Controls.Add(infoCard);
        _detailCardsHost.Controls.Add(stack);
    }
}
