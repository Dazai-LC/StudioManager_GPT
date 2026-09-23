using System.Drawing.Drawing2D;

namespace StudioManager.WinForms;

public static class Theme
{
    public static readonly Color Background = Color.FromArgb(246, 249, 253);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(21, 42, 66);
    public static readonly Color SidebarHover = Color.FromArgb(43, 77, 113);
    public static readonly Color Primary = Color.FromArgb(55, 116, 236);
    public static readonly Color PrimaryLight = Color.FromArgb(232, 241, 255);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(107, 114, 128);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color Success = Color.FromArgb(16, 185, 129);
    public static readonly Color Warning = Color.FromArgb(245, 158, 11);
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);
    public static readonly Color Cyan = Color.FromArgb(6, 182, 212);
    public static readonly Color Purple = Color.FromArgb(139, 92, 246);

    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
    public static void Round(Control c, int radius = 12)
    {
        using var p = new GraphicsPath(); var r = new Rectangle(0, 0, c.Width, c.Height); var d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
        c.Region = new Region(p);
    }
    public static Button Button(string text, Color? color = null)
    {
        var activeColor = color ?? Primary;
        var b = new Button { Text = text, AutoSize = false, Height = 40, FlatStyle = FlatStyle.Flat, Font = Font(9.5f, FontStyle.Bold), Padding = new Padding(8, 0, 8, 0), Margin = new Padding(5, 4, 5, 4) };
        b.Width = Math.Max(122, TextRenderer.MeasureText(text, b.Font).Width + 36);
        b.FlatAppearance.BorderSize = 0;
        void ApplyEnabledState()
        {
            b.BackColor = b.Enabled ? activeColor : Color.FromArgb(229, 231, 235);
            b.ForeColor = b.Enabled ? Color.White : Muted;
            b.Cursor = b.Enabled ? Cursors.Hand : Cursors.Default;
            b.FlatAppearance.MouseOverBackColor = b.BackColor;
            b.FlatAppearance.MouseDownBackColor = b.BackColor;
        }
        ApplyEnabledState();
        b.EnabledChanged += (_, _) => ApplyEnabledState();
        b.Resize += (_, _) => Round(b, 8);
        return b;
    }
    public static DataGridView Grid()
    {
        var g = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, Font = Font(9.5f), RowTemplate = { Height = 38 }, EnableHeadersVisualStyles = false };
        g.ColumnHeadersDefaultCellStyle = new() { BackColor = Color.FromArgb(249, 250, 251), ForeColor = Muted, Font = Font(9, FontStyle.Bold), Padding = new Padding(8), SelectionBackColor = Color.FromArgb(249, 250, 251) };
        g.ColumnHeadersHeight = 44; g.DefaultCellStyle = new() { BackColor = Surface, ForeColor = Text, SelectionBackColor = PrimaryLight, SelectionForeColor = Primary, Padding = new Padding(8, 4, 8, 4) }; g.GridColor = Border; return g;
    }
}
