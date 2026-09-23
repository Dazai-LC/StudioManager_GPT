namespace StudioManager.WinForms.Controls;

public sealed class PageHeader : Panel
{
    public FlowLayoutPanel Actions { get; } = new() { Dock = DockStyle.Right, Width = 420, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
    public PageHeader(string title, string subtitle)
    {
        Height = 88; Dock = DockStyle.Top; BackColor = Theme.Background; Padding = new Padding(4, 2, 0, 10);
        var copy = new Panel { Dock = DockStyle.Fill };
        copy.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Bottom, Height = 28, Font = Theme.Font(9.5f), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft });
        copy.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 48, Font = Theme.Font(22, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.BottomLeft });
        Controls.Add(copy); Controls.Add(Actions); Actions.BringToFront();
    }
}

public class CardPanel : Panel
{
    public CardPanel() { BackColor = Theme.Surface; Padding = new Padding(20); Margin = new Padding(8); Resize += (_, _) => Theme.Round(this, 14); }
}

public sealed class StatCard : CardPanel
{
    private readonly Label _value; private readonly Label _trend;
    public StatCard(string title, Color accent, string icon = "●")
    {
        Width = 250; Height = 126; Padding = new Padding(20, 15, 18, 13);
        var badge = new Label { Text = icon, Dock = DockStyle.Right, Width = 48, Font = Theme.Font(19, FontStyle.Bold), ForeColor = accent, BackColor = Color.FromArgb(248, 250, 252), TextAlign = ContentAlignment.MiddleCenter };
        badge.Resize += (_, _) => Theme.Round(badge, 12); Controls.Add(badge);
        Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 28, Font = Theme.Font(9, FontStyle.Bold), ForeColor = Theme.Muted });
        _trend = new Label { Text = "Cập nhật trực tiếp", Dock = DockStyle.Bottom, Height = 22, Font = Theme.Font(8.5f), ForeColor = accent }; Controls.Add(_trend);
        _value = new Label { Text = "—", Dock = DockStyle.Fill, Font = Theme.Font(22, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft }; Controls.Add(_value);
    }
    public string Value { get => _value.Text; set => _value.Text = value; }
    public string Trend { get => _trend.Text; set => _trend.Text = value; }
}

public static class Ui
{
    public static TextBox SearchBox(string placeholder = "Tìm kiếm...") => new() { PlaceholderText = placeholder, Width = 280, Height = 36, BorderStyle = BorderStyle.FixedSingle, Font = Theme.Font(10), Margin = new Padding(0, 2, 8, 2) };
    public static Label FieldLabel(string text) => new() { Text = text, AutoSize = false, Height = 24, Dock = DockStyle.Top, Font = Theme.Font(9, FontStyle.Bold), ForeColor = Theme.Muted };
    public static Panel Field(string label, Control input, int width = 260)
    {
        var p = new Panel { Width = width, Height = 68, Margin = new Padding(0, 0, 14, 8) }; input.Dock = DockStyle.Bottom; input.Height = 36; p.Controls.Add(input); p.Controls.Add(FieldLabel(label)); return p;
    }
    public static Panel ToolbarButton(Button button)
    {
        button.Margin = Padding.Empty;
        button.Height = 36;
        button.Dock = DockStyle.Bottom;
        var slot = new Panel { Width = button.Width, Height = 68, Margin = new Padding(0, 0, 14, 8) };
        slot.Controls.Add(button);
        return slot;
    }
    public static Panel ToolbarButtonGroup(params Button[] buttons)
    {
        const int gap = 8;
        var width = buttons.Sum(x => x.Width) + Math.Max(0, buttons.Length - 1) * gap;
        var slot = new Panel { Width = width, Height = 68, Margin = new Padding(0, 0, 14, 8) };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Width = width, Height = 36, WrapContents = false, Padding = Padding.Empty };
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            button.Dock = DockStyle.None;
            button.Height = 36;
            button.Margin = new Padding(0, 0, index == buttons.Length - 1 ? 0 : gap, 0);
            bar.Controls.Add(button);
        }
        slot.Controls.Add(bar);
        return slot;
    }
    public static Label ToolbarCaption(string text, int width = 184) => new()
    {
        Text = text,
        AutoSize = false,
        Width = width,
        Height = 68,
        Margin = new Padding(0, 0, 14, 8),
        Font = Theme.Font(9, FontStyle.Bold),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft
    };
    public static void Error(IWin32Window owner, string message) => MessageBox.Show(owner, message, "Không thể thực hiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static void Info(IWin32Window owner, string message) => MessageBox.Show(owner, message, "Studio Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void ExportGrid(DataGridView grid, IWin32Window owner, string defaultName)
    {
        if (grid.Columns.Count == 0) { Error(owner, "Không có dữ liệu để xuất."); return; }
        using var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = defaultName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv" };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        using var writer = new StreamWriter(dialog.FileName, false, new System.Text.UTF8Encoding(true));
        var visible = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).OrderBy(x => x.DisplayIndex).ToList();
        writer.WriteLine(string.Join(",", visible.Select(x => Csv(x.HeaderText))));
        foreach (DataGridViewRow row in grid.Rows)
            writer.WriteLine(string.Join(",", visible.Select(x => Csv(row.Cells[x.Index].FormattedValue?.ToString() ?? ""))));
        Info(owner, "Đã xuất dữ liệu thành công.");
    }
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
