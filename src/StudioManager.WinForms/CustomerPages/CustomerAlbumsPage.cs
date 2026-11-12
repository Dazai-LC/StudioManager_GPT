using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerAlbumsPage : UserControl
{
    private readonly CustomerSessionContext _session;

    public CustomerAlbumsPage(CustomerSessionContext session)
    {
        _session = session;
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Album ảnh của tôi",
            "Bộ sưu tập ảnh bàn giao theo quyền sở hữu đã xác minh (Demo)");

        var albums = DesktopDemoSessionStore.GetCustomerAlbums(_session.Profile);
        if (albums.Count == 0)
        {
            var empty = new CardPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28),
                AccentStripeColor = Theme.Warning,
                AccentStripeLeft = true,
                AccentStripeThickness = 4
            };
            var lbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 160,
                UseMnemonic = false,
                Text =
                    $"Chưa mở khóa album ảnh cho tài khoản '{_session.Profile.FullName}'.\r\n\r\n" +
                    $"Lý do bảo mật: Trạng thái liên kết hiện tại là '{_session.Profile.LinkStatusHeadline}'.\r\n" +
                    "Album ảnh chỉ được mở khóa khi tài khoản khách hàng đã xác minh quyền sở hữu hoặc được nhân viên liên kết thủ công có kiểm soát.\r\n\r\n" +
                    "Hãy chuyển sang mục 'Hồ sơ & Liên kết' để gửi thông tin đối chiếu mã lịch cũ với bộ phận Tiếp nhận.",
                Font = Theme.Font(10.0f),
                ForeColor = Theme.TextSecondary
            };
            empty.Controls.Add(lbl);
            Controls.Add(empty);
            Controls.Add(header);
            return;
        }

        var album = albums[0];

        var sampleBanner = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Theme.WarningLight,
            BorderColor = Theme.WarningBorder,
            AccentStripeColor = Theme.Warning,
            AccentStripeLeft = true,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(0, 0, 0, 10)
        };
        sampleBanner.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            UseMnemonic = false,
            Text = "Album minh họa mẫu (Frontend Demo): Hiển thị ảnh concept đại diện để trình bày bố cục giao diện bàn giao ảnh — chưa kết nối kho ảnh bàn giao thực tế của khách hàng.",
            Font = Theme.Font(8.7f, FontStyle.Bold),
            ForeColor = Theme.WarningHover,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var metaCard = new CardPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 12),
            AccentStripeColor = Theme.Primitives.Sage600,
            AccentStripeLeft = true,
            AccentStripeThickness = 4
        };
        var metaTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 26,
            UseMnemonic = false,
            Text = $"{album.Title}  •  Tiến độ: {album.DeliveryStatus} ({album.DeliveryProgressPercent}%)",
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Text
        };
        var metaSub = new Label
        {
            Dock = DockStyle.Fill,
            UseMnemonic = false,
            Text = $"Phạm vi hiển thị: {album.OwnerScopeLabel}\n{album.Subtitle} • Cập nhật: {album.UpdatedAtLabel}",
            Font = Theme.Font(8.8f),
            ForeColor = Theme.TextSecondary
        };
        metaCard.Controls.Add(metaSub);
        metaCard.Controls.Add(metaTitle);

        var galleryEntries = album.Photos.Select((ph, i) =>
        {
            var resolved = VisualAssetService.ResolveAssetFilePath(ph.RelativeAssetPath, ph.ResolvedFullPath);
            var exists = !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved);
            return new VisualAssetEntryDto(
                ph.PhotoId, MediaTargetEntityKind.StudioAlbum, album.AlbumId,
                ph.Title, ph.Caption, ph.Title, ph.RelativeAssetPath, resolved,
                i == 0, true, exists ? MediaSourceKind.LocalAiDefault : MediaSourceKind.SystemFallback, i + 1,
                exists ? new FileInfo(resolved!).Length : 0, 1200, 800,
                "Atelier Lumière (Album Demo)", album.UpdatedAtLabel);
        }).ToList();

        var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
        for (var i = 0; i < galleryEntries.Count; i++)
        {
            var entry = galleryEntries[i];
            var ph = album.Photos[i];
            var card = new CardPanel
            {
                Size = new Size(306, 256),
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(10),
                BackColor = Theme.Surface
            };
            var box = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            box.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
            box.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            box.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pic = new VisualMediaPictureBox(entry, galleryEntries) { Dock = DockStyle.Fill, MaxThumbWidth = 480 };
            var ttl = new Label { Dock = DockStyle.Fill, Text = ph.Title, Font = Theme.Font(9.4f, FontStyle.Bold), ForeColor = Theme.Text, AutoEllipsis = true };
            var sub = new Label { Dock = DockStyle.Fill, Text = $"{ph.ShotLabel} • {(ph.IsRetouched ? "Đã retouch (Mẫu Demo)" : "Ảnh chọn lọc")} • {ph.Caption}", Font = Theme.Font(8.1f), ForeColor = Theme.Muted };

            box.Controls.Add(pic, 0, 0);
            box.Controls.Add(ttl, 0, 1);
            box.Controls.Add(sub, 0, 2);
            card.Controls.Add(box);
            grid.Controls.Add(card);
        }

        Controls.Add(grid);
        Controls.Add(metaCard);
        Controls.Add(sampleBanner);
        Controls.Add(header);
    }
}
