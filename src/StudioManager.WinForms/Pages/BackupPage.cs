using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class BackupPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _history = Theme.Grid();
    private readonly Label _historyCount = new()
    {
        Dock = DockStyle.Right,
        Width = 140,
        Font = Theme.MonoFont(8.2f, FontStyle.Bold),
        ForeColor = Theme.Primary,
        TextAlign = ContentAlignment.MiddleRight,
        Text = "0 bản ghi"
    };

    public BackupPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var vaultSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = Color.Transparent
        };
        vaultSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        vaultSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        vaultSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        vaultSplit.Controls.Add(BuildLeftVaultChambers(), 0, 0);
        vaultSplit.Controls.Add(BuildRightHistoryConsole(), 1, 0);

        Controls.Add(vaultSplit);
        Load += async (_, _) => await LoadHistoryAsync();
    }

    private Control BuildLeftVaultChambers()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.Transparent
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        stack.Controls.Add(BuildBackupVaultCard(), 0, 0);
        stack.Controls.Add(BuildProtectedRestoreChamber(), 0, 1);
        return stack;
    }

    private Control BuildBackupVaultCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            Margin = new Padding(0, 0, 0, 8),
            CornerRadius = 12,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Success,
            AccentStripeThickness = 4
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "CHAMBER 01  •  ACTIVE SNAPSHOT VAULT",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Success,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "Khởi tạo Bản sao lưu An toàn (.bak)",
            Font = Theme.Font(12.2f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var desc = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 6),
            Text = "Đóng gói toàn bộ cơ sở dữ liệu SQL Server hiện hành (bao gồm lịch chụp, hồ sơ khách hàng, giao dịch tài chính và nhật ký kiểm toán) thành tệp .bak chuẩn.\n\n• Không làm gián đoạn phiên làm việc hiện tại.\n• Tự động ghi nhận vào nhật ký sao lưu hệ thống.",
            Font = Theme.Font(9.0f),
            ForeColor = Theme.TextSecondary
        };

        var backupBtn = Theme.Button("Tạo bản sao lưu SQL ngay", Theme.Success, IconKind.Database);
        backupBtn.Dock = DockStyle.Bottom;
        backupBtn.Height = 42;
        backupBtn.Click += async (_, _) => await BackupAsync();

        card.Controls.Add(desc);
        card.Controls.Add(title);
        card.Controls.Add(overline);
        card.Controls.Add(backupBtn);
        overline.SendToBack();
        title.SendToBack();
        backupBtn.SendToBack();
        desc.BringToFront();
        return card;
    }

    private Control BuildProtectedRestoreChamber()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            Margin = new Padding(0, 8, 0, 0),
            CornerRadius = 12,
            BackColor = Theme.DangerLight,
            BorderColor = Theme.DangerBorder,
            AccentStripeColor = Theme.Danger,
            AccentStripeThickness = 4
        };

        var overline = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "CHAMBER 02  •  PROTECTED RECOVERY PROTOCOL (2-STEP)",
            Font = Theme.MonoFont(7.6f, FontStyle.Bold),
            ForeColor = Theme.Danger,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "Phục hồi Cơ sở dữ liệu từ Bản sao lưu",
            Font = Theme.Font(12.2f, FontStyle.Bold),
            ForeColor = Theme.Primitives.Crimson700,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var desc = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 6),
            Text = "CẢNH BÁO AN TOÀN DỮ LIỆU:\nThao tác phục hồi sẽ ngắt toàn bộ kết nối hiện hành, ghi đè dữ liệu hiện tại bằng tệp .bak được chọn và tự động khởi động lại ứng dụng.\n\n• Yêu cầu xác nhận 2 bước độc lập trước khi thực thi.\n• Hãy tạo bản sao lưu mới nhất trước khi phục hồi.",
            Font = Theme.Font(8.9f, FontStyle.Bold),
            ForeColor = Theme.Danger
        };

        var restoreBtn = Theme.Button("Chọn tệp .bak & Phục hồi dữ liệu", Theme.Danger, IconKind.AlertTriangle);
        restoreBtn.Dock = DockStyle.Bottom;
        restoreBtn.Height = 42;
        restoreBtn.Click += async (_, _) => await RestoreAsync();

        card.Controls.Add(desc);
        card.Controls.Add(title);
        card.Controls.Add(overline);
        card.Controls.Add(restoreBtn);
        overline.SendToBack();
        title.SendToBack();
        restoreBtn.SendToBack();
        desc.BringToFront();
        return card;
    }

    private Control BuildRightHistoryConsole()
    {
        var historyCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
            Padding = new Padding(18, 14, 18, 14),
            CornerRadius = 12,
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            AccentStripeColor = Theme.Primitives.Espresso900,
            AccentStripeThickness = 4
        };

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8)
        };

        var refreshBtn = Theme.OutlineButton("Làm mới nhật ký", Theme.BorderStrong, IconKind.Refresh);
        refreshBtn.Dock = DockStyle.Right;
        refreshBtn.Height = 36;
        refreshBtn.Click += async (_, _) => await LoadHistoryAsync();

        var historyTitle = new Label
        {
            Text = "Sổ cái Nhật ký Sao lưu & Phục hồi Gần nhất",
            Dock = DockStyle.Fill,
            Font = Theme.Font(10.8f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        topBar.Controls.Add(historyTitle);
        topBar.Controls.Add(_historyCount);
        topBar.Controls.Add(refreshBtn);
        refreshBtn.SendToBack();
        _historyCount.SendToBack();
        historyTitle.BringToFront();

        _history.Margin = Padding.Empty;
        var dataHost = Ui.DataHost(_history);
        historyCard.Controls.Add(dataHost);
        historyCard.Controls.Add(topBar);
        topBar.SendToBack();
        dataHost.BringToFront();
        return historyCard;
    }

    private async Task BackupAsync()
    {
        var serverDirectory = await _app.BackupRestore.GetDefaultBackupDirectoryAsync(_app.Session!);
        using var s = new SaveFileDialog
        {
            Filter = "SQL Server backup (*.bak)|*.bak",
            FileName = $"StudioManager_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
        };

        if (!string.IsNullOrWhiteSpace(serverDirectory) && Directory.Exists(serverDirectory))
            s.InitialDirectory = serverDirectory;

        if (s.ShowDialog(this) != DialogResult.OK) return;

        var r = await _app.BackupRestore.BackupAsync(s.FileName, _app.Session!);
        if (!r.Success) Ui.Error(this, r.Message);
        else
        {
            Ui.Info(this, r.Message);
            await LoadHistoryAsync();
        }
    }

    private async Task RestoreAsync()
    {
        using var o = new OpenFileDialog { Filter = "SQL Server backup (*.bak)|*.bak" };
        if (o.ShowDialog(this) != DialogResult.OK) return;

        if (MessageBox.Show("Dữ liệu hiện tại sẽ bị thay thế hoàn toàn bởi bản sao lưu. Bạn đã chắc chắn có bản sao lưu gần nhất chưa?",
            "Xác nhận phục hồi (Bước 1/2)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        if (MessageBox.Show("Xác nhận lần cuối: Tiếp tục phục hồi dữ liệu và khởi động lại ứng dụng?",
            "Xác nhận phục hồi (Bước 2/2)", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
            return;

        var r = await _app.BackupRestore.RestoreAsync(o.FileName, _app.Session!);
        if (!r.Success)
        {
            Ui.Error(this, r.Message);
            await LoadHistoryAsync();
        }
        else
        {
            Ui.Info(this, r.Message);
            System.Windows.Forms.Application.Restart();
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var rows = await _app.BackupRestore.GetHistoryAsync(_app.Session!);
            _historyCount.Text = $"{rows.Count} lần thực thi";
            var table = new System.Data.DataTable();
            if (rows.Count > 0)
            {
                foreach (var key in rows[0].Keys) table.Columns.Add(key, typeof(object));
                foreach (var source in rows)
                {
                    var row = table.NewRow();
                    foreach (var value in source) row[value.Key] = value.Value ?? DBNull.Value;
                    table.Rows.Add(row);
                }
            }
            _history.DataSource = table;
            if (_history.Columns.Contains("Id")) _history.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            Ui.Error(this, ex.Message);
        }
    }
}
