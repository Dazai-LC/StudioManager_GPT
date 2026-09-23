using System.Drawing.Drawing2D;

namespace StudioManager.WinForms;

public static class Theme
{
    public static readonly Color Background = Color.FromArgb(246, 249, 253);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(21, 42, 66);
    public static readonly Color SidebarHover = Color.FromArgb(43, 77, 113);
    public static readonly Color Primary = Color.FromArgb(47, 99, 214);
    public static readonly Color PrimaryLight = Color.FromArgb(232, 241, 255);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(107, 114, 128);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);
    public static readonly Color Success = Color.FromArgb(5, 150, 105);
    public static readonly Color Warning = Color.FromArgb(217, 119, 6);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Cyan = Color.FromArgb(8, 145, 178);
    public static readonly Color Purple = Color.FromArgb(124, 58, 237);

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
        var background = color ?? Primary;
        using var measureFont = Font(9.2f, FontStyle.Bold);
        var width = Math.Max(118, TextRenderer.MeasureText(text, measureFont).Width + 32);
        var b = new Button { Text = text, AutoSize = false, Height = 38, Width = width, FlatStyle = FlatStyle.Flat, BackColor = background, ForeColor = Color.White, Font = Font(9.2f, FontStyle.Bold), Cursor = Cursors.Hand, Padding = new Padding(12, 0, 12, 0), Margin = new Padding(5, 4, 5, 4), UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Blend(background,Color.White,0.12f);
        b.FlatAppearance.MouseDownBackColor = Blend(background,Color.Black,0.14f);
        b.Resize += (_, _) => Round(b,10); return b;
    }

    private static Color Blend(Color source,Color target,float amount)=>Color.FromArgb(source.A,(int)(source.R+(target.R-source.R)*amount),(int)(source.G+(target.G-source.G)*amount),(int)(source.B+(target.B-source.B)*amount));
    public static DataGridView Grid()
    {
        var g = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, Font = Font(9.5f), RowTemplate = { Height = 38 }, EnableHeadersVisualStyles = false };
        g.ColumnHeadersDefaultCellStyle = new() { BackColor = Color.FromArgb(249, 250, 251), ForeColor = Muted, Font = Font(9, FontStyle.Bold), Padding = new Padding(8), SelectionBackColor = Color.FromArgb(249, 250, 251) };
        g.ColumnHeadersHeight = 44; g.DefaultCellStyle = new() { BackColor = Surface, ForeColor = Text, SelectionBackColor = PrimaryLight, SelectionForeColor = Primary, Padding = new Padding(8, 4, 8, 4) }; g.GridColor = Border; return g;
    }
}
