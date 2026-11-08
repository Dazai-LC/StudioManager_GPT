using StudioManager.Application.Services;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Pages;

public sealed class BackupPage : UserControl
{
    private readonly AppFacade _app;
    private readonly DataGridView _history = Theme.Grid();

    public BackupPage(AppFacade app)
    {
        _app = app;
        BackColor = Theme.Background;

        var header = new PageHeader("Sao lưu & Phục hồi", "Bảo vệ an toàn dữ liệu studio bằng các bản sao lưu cơ sở dữ liệu SQL Server.");

        var operationsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 220,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 0, 0, 14),
            BackColor = Color.Transparent
        };
        operationsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        operationsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        operationsPanel.Controls.Add(BuildBackupCard(), 0, 0);
        operationsPanel.Controls.Add(BuildRestoreCard(), 1, 0);

        var historyCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 14),
            CornerRadius = 12
        };

        var historyTitle = new Label
        {
            Text = "Nhật ký các lần sao lưu & phục hồi gần nhất",
            Dock = DockStyle.Top,
            Height = 36,
            Font = Theme.Font(11, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };

        historyCard.Controls.Add(Ui.DataHost(_history));
        historyCard.Controls.Add(historyTitle);

        Controls.Add(historyCard);
        Controls.Add(operationsPanel);
        Controls.Add(header);

        Load += async (_, _) => await LoadHistoryAsync();
    }

    private Control BuildBackupCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            Margin = new Padding(0, 0, 8, 0),
            CornerRadius = 12,
            BorderColor = Theme.Border
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "💾   Tạo bản sao lưu dữ liệu (Backup)",
            Font = Theme.Font(11, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };

        var desc = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Text = "Xuất toàn bộ cơ sở dữ liệu studio (lịch hẹn, tài chính, khách hàng) thành tệp .bak an toàn trên máy chủ SQL Server.",
            Font = Theme.Font(9.0f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        };

        var backupBtn = Theme.Button("Tạo bản sao lưu ngay", Theme.Primary);
        backupBtn.Dock = DockStyle.Bottom;
        backupBtn.Height = 42;
        backupBtn.Click += async (_, _) => await BackupAsync();

        card.Controls.Add(backupBtn);
        card.Controls.Add(desc);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildRestoreCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            Margin = new Padding(8, 0, 0, 0),
            CornerRadius = 12,
            BackColor = Color.FromArgb(255, 251, 251),
            BorderColor = Color.FromArgb(254, 202, 202)
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "⚠   Phục hồi dữ liệu (Restore)",
            Font = Theme.Font(11, FontStyle.Bold),
            ForeColor = Theme.Danger,
            BackColor = Color.Transparent
        };

        var desc = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Text = "Cảnh báo: Thao tác phục hồi sẽ ghi đè toàn bộ dữ liệu hiện tại, ngắt tất cả các kết nối đang mở và yêu cầu khởi động lại ứng dụng.",
            Font = Theme.Font(9.0f),
            ForeColor = Theme.Danger,
            BackColor = Color.Transparent
        };

        var restoreBtn = Theme.Button("Phục hồi dữ liệu từ file .bak", Theme.Danger);
        restoreBtn.Dock = DockStyle.Bottom;
        restoreBtn.Height = 42;
        restoreBtn.Click += async (_, _) => await RestoreAsync();

        card.Controls.Add(restoreBtn);
        card.Controls.Add(desc);
        card.Controls.Add(title);
        return card;
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
