using StudioManager.WinForms;
using StudioManager.WinForms.Controls;

namespace StudioManager.CustomerDesktop;

/// <summary>Layout only, using the existing Atelier palette and controls. No session or data source.</summary>
internal static class AtelierUi
{
    internal static void HideTabHeaders(TabControl tabs)
    {
        tabs.Appearance = TabAppearance.FlatButtons; tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(0, 1); tabs.Padding = Point.Empty;
    }

    internal static FlowLayoutPanel Stack(Control parent)
    {
        parent.BackColor = Theme.Background; parent.Padding = new Padding(12);
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Theme.Surface,
            BorderColor = Theme.Border, CornerRadius = 10 };
        var flow = new ResponsiveFlow { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false,
            FlowDirection = FlowDirection.TopDown, Padding = new Padding(2), BackColor = Theme.Surface };
        card.Controls.Add(flow); parent.Controls.Add(card); return flow;
    }

    internal static Label Copy(string text, bool heading = false) => new()
    {
        Text = text, AutoSize = true, UseMnemonic = false, Font = Theme.Font(heading ? 11f : 9.2f, heading ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = heading ? Theme.Text : Theme.TextSecondary, Margin = new Padding(0, heading ? 14 : 5, 0, 10)
    };

    internal static ModernButton Action(string text, bool primary = false, IconKind icon = IconKind.ArrowRight)
    {
        var button = primary ? Theme.Button(text, Theme.Primary, icon) : Theme.OutlineButton(text, Theme.BorderStrong, icon);
        void FitCaption()
        {
            var width = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width + 52;
            button.MinimumSize = new Size(width, Math.Max(38, button.Font.Height + 14));
            if (button.Width < width) button.Width = width;
        }
        button.FontChanged += (_, _) => FitCaption(); button.TextChanged += (_, _) => FitCaption(); FitCaption();
        button.Height = Math.Max(38, button.PreferredSize.Height); button.Margin = new Padding(0, 4, 8, 4); return button;
    }

    internal static void StyleGrid(DataGridView grid, bool hideIdentifiers = true)
    {
        using var reference = Theme.Grid();
        grid.BackgroundColor = reference.BackgroundColor; grid.BorderStyle = reference.BorderStyle;
        grid.EnableHeadersVisualStyles = false; grid.RowHeadersVisible = false;
        grid.Font = Theme.Font(9.3f); grid.GridColor = reference.GridColor;
        grid.CellBorderStyle = reference.CellBorderStyle; grid.ColumnHeadersBorderStyle = reference.ColumnHeadersBorderStyle;
        grid.ColumnHeadersDefaultCellStyle = reference.ColumnHeadersDefaultCellStyle.Clone();
        grid.DefaultCellStyle = reference.DefaultCellStyle.Clone();
        grid.AlternatingRowsDefaultCellStyle = reference.AlternatingRowsDefaultCellStyle.Clone();
        grid.RowTemplate.Height = 42; grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.AllowUserToResizeRows = false; grid.AllowUserToDeleteRows = false;
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.MinimumWidth = 72;
                if (hideIdentifiers && (column.Name is "Id" or "ServiceId")) column.Visible = false;
                column.HeaderText = column.Name switch
                {
                    "Id" => "Mã yêu cầu", "AccountId" => "Tài khoản", "LoginEmail" => "Email", "FullName" => "Họ tên",
                    "Reference" => "Tham chiếu", "Evidence" => "Thông tin đối chiếu", "CustomerId" => "Hồ sơ studio",
                    "Code" => "Mã", "Name" => "Tên", "Description" => "Mô tả", "Price" => "Giá hiện hành",
                    "Unit" => "Đơn vị", "UnitPrice" => "Đơn giá", "Quantity" => "Số lượng", "DurationMinutes" => "Thời lượng (phút)",
                    "State" => "Trạng thái yêu cầu", "Customer" => "Khách hàng", "Package" => "Gói chụp", "VietnamTime" => "Giờ chụp (VN)",
                    "SubmittedVietnam" => "Gửi lúc (VN)", "Estimate" => "Giá dự kiến", "OfficialBooking" => "Lịch chính thức", _ => column.HeaderText
                };
                if (column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = "#,0.####";
                    column.DefaultCellStyle.FormatProvider = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
                }
            }
        };
    }

    internal static bool Confirm(IWin32Window owner, string title, string text, string confirm = "Xác nhận gửi")
    {
        using var dialog = new AtelierDialog(AtelierNotificationKind.Confirmation, title, text, confirm, "Quay lại");
        return dialog.ShowDialog(owner) == DialogResult.Yes;
    }
    internal static void Info(IWin32Window owner, string title, string text)
    {
        using var dialog = new AtelierDialog(AtelierNotificationKind.Info, title, text); dialog.ShowDialog(owner);
    }

    private sealed class ResponsiveFlow : FlowLayoutPanel
    {
        private bool _sizing;
        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (!_sizing)
            {
                _sizing = true;
                try
                {
                    var width = Math.Max(180, ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
                    foreach (Control child in Controls)
                    {
                        var available = Math.Max(100, width - child.Margin.Horizontal);
                        child.Width = available;
                        if (child is Label label) label.MaximumSize = new Size(available, 0);
                        if (child is FlowLayoutPanel actions)
                        {
                            actions.MaximumSize = new Size(available, 0);
                            actions.Height = actions.GetPreferredSize(new Size(available, 0)).Height;
                        }
                    }
                }
                finally { _sizing = false; }
            }
            base.OnLayout(levent);
        }
    }
}
