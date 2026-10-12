using System.Drawing.Drawing2D;

namespace StudioManager.WinForms;

public static class Theme
{
    // The UI reference uses a quiet neutral canvas with a single blue brand accent.
    // Keeping these values central prevents pages from drifting into unrelated styles.
    public static readonly Color Background = Color.FromArgb(244, 246, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(23, 32, 46);
    public static readonly Color SidebarHover = Color.FromArgb(38, 54, 76);
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
    public static readonly Color PrimaryLight = Color.FromArgb(234, 241, 255);
    public static readonly Color Text = Color.FromArgb(23, 43, 77);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(223, 228, 235);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color Warning = Color.FromArgb(217, 119, 6);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Cyan = Color.FromArgb(14, 116, 144);
    public static readonly Color Purple = Color.FromArgb(109, 40, 217);
    public static readonly Color Secondary = Color.FromArgb(71, 85, 105);
    public static readonly Color Disabled = Color.FromArgb(226, 232, 240);

    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
    public static void Round(Control c, int radius = 12)
    {
        if (c.Width <= 1 || c.Height <= 1) return;
        using var p = new GraphicsPath(); var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1); var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
        c.Region = new Region(p);
    }
    public static Button Button(string text, Color? color = null)
    {
        var activeColor = color ?? Primary;
        // A single Facebook-like button recipe keeps action density comfortable without
        // letting long Vietnamese labels wrap or clip on individual pages.
        var b = new Button { Text = text, AutoSize = false, Height = 42, FlatStyle = FlatStyle.Flat, Font = Font(9.7f, FontStyle.Bold), Padding = new Padding(16, 0, 16, 0), Margin = new Padding(4), UseVisualStyleBackColor = false, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = false };
        b.MinimumSize = new Size(116, 42);
        b.Width = Math.Max(b.MinimumSize.Width, TextRenderer.MeasureText(text, b.Font).Width + 48);
        b.FlatAppearance.BorderSize = 0;
        void ApplyEnabledState()
        {
            b.BackColor = b.Enabled ? activeColor : Disabled;
            b.ForeColor = b.Enabled ? Color.White : Muted;
            b.Cursor = b.Enabled ? Cursors.Hand : Cursors.Default;
            b.FlatAppearance.MouseOverBackColor = b.BackColor;
            b.FlatAppearance.MouseDownBackColor = b.BackColor;
        }
        ApplyEnabledState();
        b.EnabledChanged += (_, _) => ApplyEnabledState();
        b.Resize += (_, _) => Round(b, 10);
        return b;
    }
    public static DataGridView Grid()
    {
        var g = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, Font = Font(9.5f), RowTemplate = { Height = 40 }, EnableHeadersVisualStyles = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single };
        g.ColumnHeadersDefaultCellStyle = new() { BackColor = Color.FromArgb(248, 250, 252), ForeColor = Muted, Font = Font(8.8f, FontStyle.Bold), Padding = new Padding(10, 2, 10, 2), SelectionBackColor = Color.FromArgb(248, 250, 252) };
        g.ColumnHeadersHeight = 42; g.DefaultCellStyle = new() { BackColor = Surface, ForeColor = Text, SelectionBackColor = PrimaryLight, SelectionForeColor = Primary, Padding = new Padding(10, 5, 10, 5) }; g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(251, 252, 254) }; g.GridColor = Border; return g;
    }
}
