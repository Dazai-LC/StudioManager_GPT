using System.Drawing.Drawing2D;
using System.Drawing.Text;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Forms;

internal static class AuthChromeFactory
{
    public static Control BuildTopRibbon(string brandSub, string badgeText, Color badgeDotColor)
    {
        var ribbon = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Sidebar,
            Padding = new Padding(24, 0, 24, 0)
        };

        ribbon.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using var linePen = new Pen(Color.FromArgb(90, Theme.Primary), 1f);
            g.DrawLine(linePen, 0, ribbon.Height - 1, ribbon.Width, ribbon.Height - 1);

            var iconRect = new RectangleF(24, (ribbon.Height - 22) / 2f, 22, 22);
            VectorIcons.Draw(g, IconKind.Aperture, iconRect, Theme.Primary, 1.9f);

            using var titleFont = Theme.Font(9.5f, FontStyle.Bold);
            using var subFont = Theme.MonoFont(8.2f, FontStyle.Regular);

            const string brandTitle = "STUDIOMANAGER";
            var titleSz = TextRenderer.MeasureText(g, brandTitle, titleFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var badgeSz = TextRenderer.MeasureText(g, badgeText, subFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            var titleRect = new Rectangle(54, 0, titleSz.Width + 6, ribbon.Height);
            TextRenderer.DrawText(g, brandTitle, titleFont, titleRect, Theme.Primitives.Alabaster100,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            var bx = Math.Max(titleRect.Right + 160, ribbon.Width - badgeSz.Width - 28);
            using var dotBrush = new SolidBrush(badgeDotColor);
            g.FillEllipse(dotBrush, bx - 14, (ribbon.Height - 7) / 2f, 7, 7);
            var badgeRect = new Rectangle(bx, 0, badgeSz.Width + 8, ribbon.Height);
            TextRenderer.DrawText(g, badgeText, subFont, badgeRect, Theme.SidebarMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            var subStartX = titleRect.Right + 16;
            var subMaxW = Math.Max(60, bx - 28 - subStartX);
            var subRect = new Rectangle(subStartX, 0, subMaxW, ribbon.Height);
            TextRenderer.DrawText(g, brandSub, subFont, subRect, Theme.SidebarMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        };

        return ribbon;
    }

    public static Control BuildAccessModeBar(AccessMode activeMode, Action onSelectStaff, Action onSelectCustomer)
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4),
            Padding = Padding.Empty
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        Control CreateTab(string label, IconKind icon, bool active, Color activeAccent, Action onClick)
        {
            var btn = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(active ? 0 : 2, 0, active ? 2 : 0, 0),
                BackColor = Color.Transparent,
                Cursor = active ? Cursors.Default : Cursors.Hand
            };
            btn.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var b = new RectangleF(1f, 1f, Math.Max(1, btn.Width - 2), Math.Max(1, btn.Height - 2));
                using var path = Theme.RoundedRectangle(b, 8f);
                var bgColor = active ? activeAccent : Color.FromArgb(232, Theme.Primitives.Alabaster100);
                using (var brush = new SolidBrush(bgColor)) g.FillPath(brush, path);
                if (!active)
                {
                    using var border = new Pen(Theme.BorderStrong, 1f);
                    g.DrawPath(border, path);
                }

                var iconColor = active ? Color.White : Theme.TextSecondary;
                var fg = active ? Color.White : Theme.TextSecondary;
                VectorIcons.Draw(g, icon, new RectangleF(12, (btn.Height - 15) / 2f, 15, 15), iconColor, 1.8f);

                using var f = Theme.Font(8.8f, FontStyle.Bold);
                var textRect = new Rectangle(32, 0, Math.Max(40, btn.Width - 38), btn.Height);
                TextRenderer.DrawText(g, label, f, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            };
            if (!active) btn.Click += (_, _) => onClick();
            return btn;
        }

        bar.Controls.Add(CreateTab("Nhân sự & Quản trị (SQL)", IconKind.RoleAdmin, activeMode == AccessMode.StaffAdminSql, Theme.Primary, onSelectStaff), 0, 0);
        bar.Controls.Add(CreateTab("Khách hàng (Demo)", IconKind.RoleCustomer, activeMode == AccessMode.CustomerDemo, Theme.Primitives.Sage600, onSelectCustomer), 1, 0);
        return bar;
    }

    public static Control CreateTranslucentPillarTile(IconKind icon, string title, string subtitle, Color iconTint, Color iconBoxFill)
    {
        var tile = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            BackColor = Color.Transparent
        };

        tile.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, tile.Width - 1), Math.Max(1, tile.Height - 1));
            using var path = Theme.RoundedRectangle(bounds, 10f);
            using var fill = new SolidBrush(Color.FromArgb(164, 22, 20, 18));
            using var border = new Pen(Color.FromArgb(62, Theme.Primitives.Alabaster200), 1f);
            g.FillPath(fill, path);
            g.DrawPath(border, path);

            var iconBox = new RectangleF(14, (tile.Height - 36) / 2f, 36, 36);
            using var iconPath = Theme.RoundedRectangle(iconBox, 8f);
            using var iconBg = new SolidBrush(iconBoxFill);
            g.FillPath(iconBg, iconPath);
            VectorIcons.Draw(g, icon, new RectangleF(iconBox.Left + 9, iconBox.Top + 9, 18, 18), iconTint, 1.8f);

            using var tFont = Theme.Font(9.2f, FontStyle.Bold);
            using var sFont = Theme.Font(8.2f);

            var textX = 62;
            var textW = Math.Max(60, tile.Width - textX - 12);
            var tH = Math.Max(20, tFont.Height + 2);
            var sMeasured = TextRenderer.MeasureText(g, subtitle, sFont, new Size(textW, 120), TextFormatFlags.WordBreak);
            var sH = Math.Max(22, sMeasured.Height + 2);

            var totalBlockH = tH + 3 + sH;
            var startY = Math.Max(6, (tile.Height - totalBlockH) / 2);

            TextRenderer.DrawText(g, title, tFont, new Rectangle(textX, startY, textW, tH), Theme.Primitives.Alabaster50,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            var subY = startY + tH + 2;
            var subAvailH = Math.Max(20, tile.Height - subY - 4);
            TextRenderer.DrawText(g, subtitle, sFont, new Rectangle(textX, subY, textW, subAvailH), Theme.Primitives.Stone300,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        };

        return tile;
    }

    public static Panel CreateVectorInputField(string label, TextBox input, IconKind icon, bool allowReveal = false)
    {
        var p = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent };

        var caption = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = label,
            UseMnemonic = false,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Font = Theme.Font(8.0f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var border = new ModernInputContainer
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Theme.Primitives.Alabaster50,
            LeadingIcon = icon
        };

        input.BackColor = Theme.Primitives.Alabaster50;
        input.GotFocus += (_, _) => border.Invalidate();
        input.LostFocus += (_, _) => border.Invalidate();
        border.Click += (_, _) => input.Focus();
        border.Controls.Add(input);

        Panel? revealBtn = null;
        if (allowReveal)
        {
            revealBtn = new Panel
            {
                Dock = DockStyle.Right,
                Width = 36,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            revealBtn.Paint += (_, e) =>
            {
                var k = input.UseSystemPasswordChar ? IconKind.Eye : IconKind.EyeOff;
                VectorIcons.Draw(e.Graphics, k, new RectangleF(10, (revealBtn.Height - 16) / 2f, 16, 16), Theme.Primary, 1.75f);
            };
            revealBtn.Click += (_, _) =>
            {
                input.UseSystemPasswordChar = !input.UseSystemPasswordChar;
                revealBtn.Invalidate();
                input.Focus();
            };
            border.Controls.Add(revealBtn);
        }

        void LayoutInput()
        {
            var rightGutter = revealBtn is not null ? 40 : 12;
            var th = Math.Max(20, input.PreferredHeight);
            input.SetBounds(38, Math.Max(4, (border.ClientSize.Height - th) / 2),
                Math.Max(40, border.ClientSize.Width - 38 - rightGutter), th);
        }

        border.Resize += (_, _) => LayoutInput();
        input.FontChanged += (_, _) => LayoutInput();
        LayoutInput();

        p.Controls.Add(border);
        p.Controls.Add(caption);
        return p;
    }
}

internal sealed class ModernInputContainer : Panel
{
    public IconKind LeadingIcon { get; set; } = IconKind.None;

    public ModernInputContainer()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var hasFocus = ContainsFocus;
        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 8f);

        using (var fillBrush = new SolidBrush(BackColor))
            g.FillPath(fillBrush, path);

        using (var borderPen = new Pen(hasFocus ? Theme.Primary : Theme.BorderStrong, hasFocus ? 1.6f : 1f))
            g.DrawPath(borderPen, path);

        if (LeadingIcon != IconKind.None)
        {
            var iconColor = hasFocus ? Theme.Primary : Theme.Muted;
            VectorIcons.Draw(g, LeadingIcon, new RectangleF(13, (Height - 16) / 2f, 16, 16), iconColor, 1.75f);
        }
    }
}
