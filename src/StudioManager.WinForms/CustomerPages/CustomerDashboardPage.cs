using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public interface ICustomerNavigator
{
    void NavigateTo(string sectionKey, object? payload = null);
    void RefreshBadgeCounts();
}

public sealed class CustomerDashboardPage : UserControl
{
    private readonly CustomerSessionContext _session;
    private readonly ICustomerNavigator _nav;

    public CustomerDashboardPage(CustomerSessionContext session, ICustomerNavigator nav)
    {
        _session = session;
        _nav = nav;
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 18);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Background
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // 1. Page Header
        var header = new PageHeader(
            $"Xin chào, {_session.Profile.FullName}",
            "Không gian Khách hàng (Demo) • Khám phá gói chụp, gửi yêu cầu và theo dõi tiến độ");
        var newReqBtn = Theme.Button("Tạo yêu cầu đặt lịch", Theme.Primitives.Sage600, IconKind.Calendar);
        newReqBtn.Width = 190;
        newReqBtn.Click += (_, _) => _nav.NavigateTo("wizard");
        header.Actions.Controls.Add(newReqBtn);

        // 2. Hero Editorial Banner with Real Photo + Link Status
        var heroCard = BuildHeroBanner();

        // 3. 4 Even KPI Cards
        var kpiRow = BuildKpiStrip();

        // 4. Split Row: Featured Packages (left) + Notifications & Recent Requests (right)
        var lowerSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 330,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0),
            BackColor = Color.Transparent
        };
        lowerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        lowerSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        lowerSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lowerSplit.Controls.Add(BuildFeaturedPackagesCard(), 0, 0);
        lowerSplit.Controls.Add(BuildNotificationsAndStatusCard(), 1, 0);

        stack.Controls.Add(header, 0, 0);
        stack.Controls.Add(heroCard, 0, 1);
        stack.Controls.Add(kpiRow, 0, 2);
        stack.Controls.Add(lowerSplit, 0, 3);
        scroll.Controls.Add(stack);
        Controls.Add(scroll);
    }

    private Control BuildHeroBanner()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 214,
            BackColor = Theme.Primitives.Espresso900,
            BorderColor = Theme.Primitives.Espresso800,
            AccentStripeColor = Theme.Primitives.Sage600,
            AccentStripeLeft = true,
            AccentStripeThickness = 5,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 4, 0, 12)
        };

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        var overline = new Label
        {
            AutoSize = true,
            Text = "ATELIER LUMIÈRE  •  KHÔNG GIAN KHÁCH HÀNG (DEMO DESKTOP)",
            Font = Theme.MonoFont(7.8f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Sage200,
            Margin = new Padding(0, 0, 0, 4)
        };
        var headline = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = "Lưu giữ khoảnh khắc tinh tế trong ánh sáng kiến trúc studio",
            Font = Theme.Font(13.8f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Alabaster50,
            Margin = new Padding(0, 0, 0, 6)
        };
        var linkDesc = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Text = $"Trạng thái hồ sơ: {_session.Profile.LinkStatusHeadline} — Dữ liệu minh họa phục vụ trải nghiệm quy trình.",
            Font = Theme.Font(8.6f),
            ForeColor = Theme.Primitives.Stone300,
            Margin = new Padding(0, 0, 0, 10)
        };

        var ctaBar = new FlowLayoutPanel { AutoSize = true, WrapContents = true, BackColor = Color.Transparent, Margin = Padding.Empty };
        var browseBtn = Theme.Button("Khám phá gói chụp", Theme.Primary, IconKind.Sparkles);
        browseBtn.Width = 172;
        browseBtn.Height = 36;
        browseBtn.Margin = new Padding(0, 0, 8, 4);
        browseBtn.Click += (_, _) => _nav.NavigateTo("packages");
        var spacesBtn = Theme.OutlineButton("Xem phòng & trang phục", Theme.Primitives.Alabaster200, IconKind.Home);
        spacesBtn.Width = 206;
        spacesBtn.Height = 36;
        spacesBtn.ForeColor = Theme.Text;
        spacesBtn.Margin = new Padding(0, 0, 0, 4);
        spacesBtn.Click += (_, _) => _nav.NavigateTo("spaces");
        ctaBar.Controls.Add(browseBtn);
        ctaBar.Controls.Add(spacesBtn);

        left.Controls.Add(overline);
        left.Controls.Add(headline);
        left.Controls.Add(linkDesc);
        left.Controls.Add(ctaBar);

        card.SizeChanged += (_, _) =>
        {
            var leftW = Math.Max(320, (int)((card.ClientSize.Width - 52) * 0.65f));
            headline.MaximumSize = new Size(leftW, 0);
            linkDesc.MaximumSize = new Size(leftW, 0);
            ctaBar.MaximumSize = new Size(leftW, 0);
            var desiredH = overline.Height + headline.Height + linkDesc.Height + ctaBar.Height + 52;
            var targetH = Math.Clamp(desiredH, 196, 256);
            if (card.Height != targetH) card.Height = targetH;
        };

        var cover = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry("GC000001"), VisualAssetService.GetGallery("GC000001"))
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12, 0, 0, 0),
            MaxThumbWidth = 480
        };

        grid.Controls.Add(left, 0, 0);
        grid.Controls.Add(cover, 1, 0);
        card.Controls.Add(grid);
        return card;
    }

    private Control BuildKpiStrip()
    {
        var reqs = DesktopDemoSessionStore.GetCustomerBookingRequests(_session.Profile.DemoAccountId);
        var bookings = DesktopDemoSessionStore.GetCustomerBookings(_session.Profile);
        var payments = DesktopDemoSessionStore.GetCustomerPayments(_session.Profile.DemoAccountId);
        var albums = DesktopDemoSessionStore.GetCustomerAlbums(_session.Profile);

        var openReqs = reqs.Count(r => r.TrangThai is BookingRequestStatus.CHO_XU_LY or BookingRequestStatus.CAN_BO_SUNG);
        var pendingPay = payments.Count(p => p.TrangThai == PaymentReconciliationStatus.CHO_DOI_SOAT);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        for (var i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        StatCard MakeStat(string title, string val, string sub, Color accent, IconKind icon, string navTarget)
        {
            var card = new StatCard(title, accent, icon)
            {
                Value = val,
                Trend = sub,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, navTarget == "albums" ? 0 : 10, 0),
                Cursor = Cursors.Hand
            };
            card.Click += (_, _) => _nav.NavigateTo(navTarget);
            return card;
        }

        grid.Controls.Add(MakeStat("Yêu cầu mở", openReqs.ToString(), $"Tổng {reqs.Count} yêu cầu", Theme.Primary, IconKind.History, "wizard"), 0, 0);
        grid.Controls.Add(MakeStat("Lịch chụp", bookings.Count.ToString(), _session.Profile.LinkState == CustomerIdentityLinkState.VerifiedOwnership ? "Đã xác minh" : "Chưa liên kết", Theme.Primitives.Sage600, IconKind.Calendar, "bookings"), 1, 0);
        grid.Controls.Add(MakeStat("Chờ đối soát", pendingPay.ToString(), "Duyệt thủ công", Theme.Warning, IconKind.Wallet, "payments"), 2, 0);
        grid.Controls.Add(MakeStat("Album ảnh", albums.Count.ToString(), albums.Count > 0 ? "Xem trước Demo" : "Cần xác minh", Theme.Primitives.Ochre600, IconKind.Layers, "albums"), 3, 0);
        return grid;
    }

    private Control BuildFeaturedPackagesCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(16)
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Gói chụp tiêu biểu tại Atelier Lumière",
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, Padding = new Padding(0, 6, 0, 0) };

        foreach (var pkg in SeedGroundedCatalogRepository.GetPackages().Take(4))
        {
            var tile = new CardPanel
            {
                Size = new Size(256, 128),
                Margin = new Padding(0, 0, 10, 10),
                Padding = new Padding(8),
                BackColor = Theme.Primitives.Alabaster50,
                Cursor = Cursors.Hand
            };
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var pic = new VisualMediaPictureBox(VisualAssetService.GetCoverEntry(pkg.MaGoiChup), pkg.Gallery)
            {
                Dock = DockStyle.Fill,
                ShowBadge = true,
                MaxThumbWidth = 240,
                Cursor = Cursors.Hand
            };

            var info = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 2, 0, 0) };
            var nameLbl = new Label { Dock = DockStyle.Top, Height = 24, Text = pkg.TenGoi, Font = Theme.Font(9.4f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
            var priceLbl = new Label { Dock = DockStyle.Top, Height = 22, Text = $"{pkg.GiaCoBan:N0} đ", Font = Theme.Font(9.0f, FontStyle.Bold), ForeColor = Theme.Primary };
            var metaLbl = new Label { Dock = DockStyle.Fill, Text = $"{pkg.ThoiLuongPhut} phút  •  Nhấn để xem chi tiết\n{pkg.MoTa}", Font = Theme.Font(8.1f), ForeColor = Theme.Muted, AutoEllipsis = true };
            info.Controls.Add(metaLbl);
            info.Controls.Add(priceLbl);
            info.Controls.Add(nameLbl);

            row.Controls.Add(pic, 0, 0);
            row.Controls.Add(info, 1, 0);
            tile.Controls.Add(row);

            void OpenPkg(object? _, EventArgs __) => _nav.NavigateTo("packages", pkg.MaGoiChup);
            tile.Click += OpenPkg;
            pic.Click += OpenPkg;
            info.Click += OpenPkg;
            nameLbl.Click += OpenPkg;
            priceLbl.Click += OpenPkg;
            metaLbl.Click += OpenPkg;

            flow.Controls.Add(tile);
        }

        void UpdateTileWidths()
        {
            var avail = Math.Max(260, flow.ClientSize.Width - 20);
            var cols = avail >= 520 ? 2 : 1;
            var w = cols == 2 ? Math.Max(240, (avail - 12) / 2) : avail;
            foreach (Control c in flow.Controls)
            {
                c.Width = w;
            }
        }
        flow.SizeChanged += (_, _) => UpdateTileWidths();
        UpdateTileWidths();

        card.Controls.Add(flow);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildNotificationsAndStatusCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(16)
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Thông báo & Cập nhật phiên Demo",
            UseMnemonic = false,
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var list = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };

        var notifications = DesktopDemoSessionStore.GetNotifications(_session.Profile.DemoAccountId);
        if (notifications.Count == 0)
        {
            list.Controls.Add(new Label
            {
                Width = 340,
                Height = 72,
                Text = "Chưa có thông báo phát sinh cho tài khoản Demo này. Tạo yêu cầu đặt lịch hoặc thanh toán để xem cập nhật tức thì.",
                Font = Theme.Font(8.4f),
                ForeColor = Theme.Muted
            });
        }
        else
        {
            foreach (var n in notifications)
            {
                var nCard = new CardPanel
                {
                    Width = 340,
                    Height = 82,
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(12, 8, 10, 8),
                    BackColor = n.IsRead ? Theme.Primitives.Alabaster50 : Theme.PrimaryLight,
                    BorderColor = n.IsRead ? Theme.Border : Theme.PrimaryLightBorder,
                    AccentStripeColor = n.IsRead ? Theme.Primitives.Stone300 : Theme.Primary,
                    AccentStripeLeft = true,
                    AccentStripeThickness = 3,
                    Cursor = Cursors.Hand
                };
                var lblTitle = new Label { Dock = DockStyle.Top, Height = 20, Text = $"{n.Title}  •  {n.CreatedAtLabel}", Font = Theme.Font(8.5f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
                var lblMsg = new Label { Dock = DockStyle.Fill, Text = n.Message, Font = Theme.Font(8.1f), ForeColor = Theme.TextSecondary, AutoEllipsis = true };
                nCard.Controls.Add(lblMsg);
                nCard.Controls.Add(lblTitle);
                nCard.Click += (_, _) => _nav.NavigateTo(n.TargetSectionKey);
                lblTitle.Click += (_, _) => _nav.NavigateTo(n.TargetSectionKey);
                lblMsg.Click += (_, _) => _nav.NavigateTo(n.TargetSectionKey);
                list.Controls.Add(nCard);
            }
        }

        void UpdateNotificationWidths()
        {
            var w = Math.Max(220, list.ClientSize.Width - 20);
            foreach (Control c in list.Controls)
            {
                c.Width = w;
            }
        }
        list.SizeChanged += (_, _) => UpdateNotificationWidths();
        UpdateNotificationWidths();

        card.Controls.Add(list);
        card.Controls.Add(title);
        return card;
    }
}

internal static class CustomerPagePaletteExtensions
{
    public static readonly Color Stone300Fallback = Theme.Primitives.Stone300;
}
