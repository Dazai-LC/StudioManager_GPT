using System.Drawing.Drawing2D;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

// Shared rendering extracted unchanged from the approved auth chrome.
internal static class AtelierInput
{
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
