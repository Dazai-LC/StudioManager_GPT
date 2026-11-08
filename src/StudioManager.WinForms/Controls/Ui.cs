using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

public sealed class PageHeader : Panel
{
    public FlowLayoutPanel Actions { get; } = new()
    {
        Dock = DockStyle.Right,
        Width = 480,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = true,
        AutoScroll = false,
        Padding = new Padding(0, 10, 0, 0),
        BackColor = Color.Transparent
    };

    public PageHeader(string title, string subtitle)
    {
        Height = 74;
        Dock = DockStyle.Top;
        BackColor = Color.Transparent;
        Padding = new Padding(2, 4, 0, 10);
        AccessibleName = title;

        var copy = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        copy.Controls.Add(new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            Font = Theme.Font(9.8f),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        });

        Controls.Add(copy);
        Controls.Add(Actions);
        Actions.BringToFront();
    }
}

public class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 12;
    public Color BorderColor { get; set; } = Theme.Border;
    public bool ShowBorder { get; set; } = true;

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        Padding = new Padding(18);
        Margin = Padding.Empty;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, CornerRadius);

        // Fill background
        using var fillBrush = new SolidBrush(BackColor);
        g.FillPath(fillBrush, path);

        // Subtle 1px border
        if (ShowBorder && BorderColor != Color.Transparent)
        {
            using var borderPen = new Pen(BorderColor, 1f);
            g.DrawPath(borderPen, path);
        }
    }
}

public sealed class StatCard : CardPanel
{
    private readonly Label _value;
    private readonly Label _trend;
    private readonly Label _titleLabel;
    private readonly Label _badge;

    public StatCard(string title, Color accent, string icon = "●")
    {
        Height = 126;
        Padding = new Padding(18, 14, 18, 14);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _badge = new Label
        {
            Text = icon,
            Dock = DockStyle.Fill,
            Font = Theme.Font(16, FontStyle.Bold),
            ForeColor = accent,
            BackColor = Color.FromArgb(24, accent.R, accent.G, accent.B),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 4)
        };
        _badge.Resize += (_, _) => Theme.Round(_badge, 10);

        _titleLabel = new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(8.2f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _value = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(19, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _trend = new Label
        {
            Text = "Cập nhật trực tiếp",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = Theme.Font(8.0f),
            ForeColor = accent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(_titleLabel, 0, 0);
        layout.Controls.Add(_value, 0, 1);
        layout.Controls.Add(_trend, 0, 2);
        layout.Controls.Add(_badge, 1, 0);
        layout.SetRowSpan(_badge, 2);

        Controls.Add(layout);
    }

    public string Value { get => _value.Text; set => _value.Text = value; }
    public string Trend { get => _trend.Text; set => _trend.Text = value; }
}

public static class Ui
{
    public static SearchInput SearchBox(string placeholder = "Tìm kiếm...") =>
        new(placeholder) { Width = 310, Height = 40, Margin = new Padding(0, 1, 8, 1) };

    public static Label FieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Height = 24,
        Dock = DockStyle.Top,
        Font = Theme.Font(8.2f, FontStyle.Bold),
        ForeColor = Theme.Secondary,
        TextAlign = ContentAlignment.BottomLeft
    };

    public static Panel Field(string label, Control input, int width = 260)
    {
        var p = new Panel { Width = width, Height = 68, Margin = new Padding(0, 0, 14, 8), BackColor = Color.Transparent };
        input.Dock = DockStyle.Bottom;
        input.Height = 36;
        p.Controls.Add(input);
        p.Controls.Add(FieldLabel(label));
        return p;
    }

    public static Panel ToolbarButton(Control button)
    {
        button.Margin = Padding.Empty;
        button.Height = 38;
        button.Dock = DockStyle.Bottom;
        var slot = new Panel { Width = button.Width, Height = 68, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        slot.Controls.Add(button);
        return slot;
    }

    public static Panel ToolbarInput(Control input)
    {
        input.Margin = Padding.Empty;
        input.Dock = DockStyle.Bottom;
        input.Height = 38;
        var slot = new Panel { Width = input.Width, Height = 68, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        slot.Controls.Add(input);
        return slot;
    }

    public static Panel ToolbarButtonGroup(params Control[] buttons)
    {
        const int gap = 8;
        var width = buttons.Sum(x => x.Width) + Math.Max(0, buttons.Length - 1) * gap;
        var slot = new Panel { Width = width, Height = 68, Margin = new Padding(0, 0, 10, 8), BackColor = Color.Transparent };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Width = width, Height = 38, WrapContents = false, Padding = Padding.Empty, BackColor = Color.Transparent };
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            button.Dock = DockStyle.None;
            button.Height = 38;
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
        Margin = new Padding(0, 0, 10, 8),
        Font = Theme.Font(8.5f, FontStyle.Bold),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static FlowLayoutPanel Toolbar() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 2, 0, 8)
    };

    public static CardPanel SearchToolbarCard() => new()
    {
        Dock = DockStyle.Top,
        Height = 64,
        Padding = new Padding(12, 11, 12, 11),
        Margin = new Padding(0, 0, 0, 12),
        CornerRadius = 10
    };

    public static FlowLayoutPanel ActionButtonGroup() => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Height = 48,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        Padding = Padding.Empty,
        Margin = Padding.Empty,
        BackColor = Theme.Surface
    };

    public static Control SelectionActionBar(string hint, FlowLayoutPanel actions)
    {
        var card = new CardPanel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(18, 11, 18, 11), CornerRadius = 10 };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = Theme.Surface };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var copy = new Label
        {
            Dock = DockStyle.Fill,
            Text = hint,
            ForeColor = Theme.Muted,
            Font = Theme.Font(9.2f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 12, 0)
        };

        actions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        foreach (Control action in actions.Controls) action.Margin = new Padding(4);
        actions.ControlAdded += (_, e) =>
        {
            if (e.Control is { } action) action.Margin = new Padding(4);
        };

        layout.Controls.Add(copy, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    public static Control DataHost(DataGridView grid)
    {
        var frame = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            CornerRadius = 10
        };
        grid.Margin = Padding.Empty;
        frame.Controls.Add(grid);

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };
        host.Controls.Add(frame);
        return host;
    }

    public static Label SectionLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        AutoSize = false,
        Height = 28,
        Width = 220,
        Margin = new Padding(12, 16, 0, 4),
        Font = Theme.Font(7.8f, FontStyle.Bold),
        ForeColor = Color.FromArgb(148, 163, 184),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public static void Error(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, "Không thể thực hiện", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Info(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, "Studio Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
            Info(owner, "Đã xuất CSV UTF-8 thành công.");
            return;
        }
        ExportWorkbook(dialog.FileName, grid, visible);
        Info(owner, "Đã xuất file Excel Unicode thành công.");
    }

    private static void ExportCsv(string path, DataGridView grid, IReadOnlyList<DataGridViewColumn> visible)
    {
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

public sealed class SearchInput : UserControl
{
    private readonly Label _icon = new()
    {
        Text = "⌕",
        Width = 28,
        ForeColor = Theme.Muted,
        Font = Theme.Font(14),
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.Transparent
    };

    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(248, 250, 252),
        ForeColor = Theme.Text,
        Font = Theme.Font(9.8f)
    };

    private readonly Label _clearBtn = new()
    {
        Text = "×",
        Width = 24,
        ForeColor = Theme.Muted,
        Font = Theme.Font(13, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter,
        Cursor = Cursors.Hand,
        Visible = false,
        BackColor = Color.Transparent
    };

    public SearchInput(string placeholder)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.FromArgb(248, 250, 252);
        Cursor = Cursors.IBeam;
        MinimumSize = new Size(180, 40);
        Height = 40;

        _input.PlaceholderText = placeholder;
        _input.TextChanged += (_, _) =>
        {
            _clearBtn.Visible = !string.IsNullOrEmpty(_input.Text);
            OnTextChanged(EventArgs.Empty);
        };
        _input.KeyDown += (_, e) => OnKeyDown(e);
        _input.KeyPress += (_, e) => OnKeyPress(e);
        _input.KeyUp += (_, e) => OnKeyUp(e);
        _input.GotFocus += (_, _) => Invalidate();
        _input.LostFocus += (_, _) => Invalidate();

        _clearBtn.Click += (_, _) => { _input.Clear(); _input.Focus(); };
        Click += (_, _) => _input.Focus();
        _icon.Click += (_, _) => _input.Focus();

        Controls.Add(_clearBtn);
        Controls.Add(_icon);
        Controls.Add(_input);

        Resize += (_, _) => LayoutInnerControls();
        LayoutInnerControls();
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text { get => _input.Text; set => _input.Text = value ?? string.Empty; }
    public string PlaceholderText { get => _input.PlaceholderText; set => _input.PlaceholderText = value; }
    public new event KeyEventHandler? KeyDown { add => _input.KeyDown += value; remove => _input.KeyDown -= value; }
    public new event KeyPressEventHandler? KeyPress { add => _input.KeyPress += value; remove => _input.KeyPress -= value; }
    public new event KeyEventHandler? KeyUp { add => _input.KeyUp += value; remove => _input.KeyUp -= value; }
    public void Clear() => _input.Clear();
    public override void Refresh() { _input.Refresh(); base.Refresh(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var path = Theme.RoundedRectangle(bounds, 9f);
        using var fill = new SolidBrush(BackColor);
        using var pen = new Pen(_input.Focused ? Theme.Primary : Theme.Border, _input.Focused ? 1.5f : 1f);

        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e) =>
        e.Graphics.Clear(Parent?.BackColor is { A: byte.MaxValue } color ? color : Theme.Background);

    private void LayoutInnerControls()
    {
        _icon.Bounds = new Rectangle(8, 0, 24, Height);
        var rightGutter = _clearBtn.Visible ? 28 : 8;
        _input.Bounds = new Rectangle(36, Math.Max(0, (Height - _input.PreferredHeight) / 2), Math.Max(1, Width - 36 - rightGutter), _input.PreferredHeight);
        _clearBtn.Bounds = new Rectangle(Width - 28, 0, 24, Height);
    }
}
