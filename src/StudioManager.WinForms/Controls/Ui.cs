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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        var badge = new Label { Text = icon, Dock = DockStyle.Fill, Font = Theme.Font(19, FontStyle.Bold), ForeColor = accent, BackColor = Color.FromArgb(248, 250, 252), TextAlign = ContentAlignment.MiddleCenter };
        badge.Resize += (_, _) => Theme.Round(badge, 12);
        layout.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, AutoEllipsis = true, Font = Theme.Font(9, FontStyle.Bold), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _value = new Label { Text = "—", Dock = DockStyle.Fill, AutoEllipsis = true, Font = Theme.Font(22, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
        _trend = new Label { Text = "Cập nhật trực tiếp", Dock = DockStyle.Fill, AutoEllipsis = true, Font = Theme.Font(8.5f), ForeColor = accent, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(_value, 0, 1); layout.Controls.Add(_trend, 0, 2); layout.Controls.Add(badge, 1, 0); layout.SetRowSpan(badge, 3);
        Controls.Add(layout);
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
        var visible = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).OrderBy(x => x.DisplayIndex).ToList();
        if (visible.Count == 0) { Error(owner, "Không có cột hiển thị để xuất."); return; }
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook Unicode (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
            FilterIndex = 1,
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = defaultName + "_" + timestamp + ".xlsx"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        if (string.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            ExportCsv(dialog.FileName, grid, visible);
            Info(owner, "Đã xuất CSV UTF-8. Để mở trực tiếp trong Excel và giữ tiếng Việt/font, hãy chọn định dạng Excel Workbook (*.xlsx).");
            return;
        }
        ExportWorkbook(dialog.FileName, grid, visible);
        Info(owner, "Đã xuất file Excel Unicode với font Times New Roman.");
    }

    private static void ExportCsv(string path, DataGridView grid, IReadOnlyList<DataGridViewColumn> visible)
    {
        // CSV is plain text: it cannot store a font. Keep a UTF-8 BOM for systems
        // that consume CSV, while the default .xlsx option is for direct Excel use.
        using var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(true));
        var separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        writer.WriteLine("sep=" + separator);
        writer.WriteLine(string.Join(separator, visible.Select(x => Csv(x.HeaderText))));
        foreach (DataGridViewRow row in grid.Rows.Cast<DataGridViewRow>().Where(x => !x.IsNewRow))
            writer.WriteLine(string.Join(separator, visible.Select(x => Csv(row.Cells[x.Index].FormattedValue?.ToString() ?? ""))));
    }

    private static void ExportWorkbook(string path, DataGridView grid, IReadOnlyList<DataGridViewColumn> visible)
    {
        var rows = new List<IReadOnlyList<string>> { visible.Select(x => x.HeaderText).ToArray() };
        rows.AddRange(grid.Rows.Cast<DataGridViewRow>().Where(x => !x.IsNewRow)
            .Select(row => (IReadOnlyList<string>)visible.Select(column => row.Cells[column.Index].FormattedValue?.ToString() ?? string.Empty).ToArray()));

        using var archive = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
        WriteArchiveEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        WriteArchiveEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        WriteArchiveEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Dữ liệu\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        WriteArchiveEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        WriteArchiveEntry(archive, "xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Times New Roman\"/></font><font><b/><sz val=\"11\"/><name val=\"Times New Roman\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs></styleSheet>");

        var sheet = new System.Text.StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        sheet.Append("<cols>");
        for (var column = 0; column < visible.Count; column++)
        {
            var longest = rows.Max(row => row.Count > column ? row[column].Length : 0);
            sheet.Append($"<col min=\"{column + 1}\" max=\"{column + 1}\" width=\"{Math.Clamp(longest + 2, 12, 42)}\" customWidth=\"1\"/>");
        }
        sheet.Append("</cols><sheetData>");
        for (var row = 0; row < rows.Count; row++)
        {
            sheet.Append($"<row r=\"{row + 1}\">");
            for (var column = 0; column < visible.Count; column++)
                sheet.Append($"<c r=\"{ExcelColumnName(column + 1)}{row + 1}\" t=\"inlineStr\" s=\"{(row == 0 ? 1 : 0)}\"><is><t xml:space=\"preserve\">{Xml(rows[row][column])}</t></is></c>");
            sheet.Append("</row>");
        }
        sheet.Append("</sheetData><autoFilter ref=\"A1:").Append(ExcelColumnName(visible.Count)).Append(rows.Count).Append("\"/></worksheet>");
        WriteArchiveEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
    }

    private static void WriteArchiveEntry(System.IO.Compression.ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ExcelColumnName(int number)
    {
        var name = string.Empty;
        while (number > 0) { number--; name = (char)('A' + number % 26) + name; number /= 26; }
        return name;
    }

    private static string Xml(string value) => System.Security.SecurityElement.Escape(value.Replace("\0", string.Empty)) ?? string.Empty;
    private static string Csv(string value)
    {
        var trimmed = value.TrimStart();
        var safe = trimmed.Length > 0 && "=+-@".Contains(trimmed[0]) ? "'" + value : value;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
