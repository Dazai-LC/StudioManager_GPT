using System.Drawing.Drawing2D;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.WinForms.Controls;

namespace StudioManager.WinForms.Forms;

public sealed class ApplicationInitializationForm : Form
{
    private readonly AppFacade _app;
    private readonly Label _headline = new()
    {
        Dock = DockStyle.Fill,
        Text = "Đang khởi tạo workspace...",
        Font = Theme.Font(15, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleCenter
    };
    private readonly Label _detail = new()
    {
        Dock = DockStyle.Fill,
        Text = "StudioManager đang chuẩn bị dữ liệu và thiết lập quyền truy cập",
        Font = Theme.Font(9.5f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.TopCenter
    };
    private readonly Label[] _steps = new Label[5];
    private readonly MilestoneProgressBar _progress = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 10, 0, 10)
    };
    private readonly SpinnerControl _spinner = new() { Dock = DockStyle.Fill };
    private readonly ModernButton _retry = Theme.Button("↻  Thử lại");
    private readonly ModernButton _logout = Theme.Button("Quay lại đăng nhập", Theme.Secondary);
    private readonly FlowLayoutPanel _actions = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        Visible = false
    };
    private CancellationTokenSource? _initializationCts;
    public DashboardData? DashboardData { get; private set; }

    public ApplicationInitializationForm(AppFacade app)
    {
        _app = app;
        Text = "Khởi tạo hệ thống • StudioManager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 580);
        MinimumSize = new Size(560, 540);
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Theme.Background;
        Font = Theme.Font();

        Controls.Add(BuildCard());
        _retry.Width = 126;
        _logout.Width = 174;
        _actions.ClientSizeChanged += (_, _) => CenterActions();

        Shown += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => _initializationCts?.Cancel();
        _retry.Click += async (_, _) => await InitializeAsync();
        _logout.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
    }

    private Control BuildCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(54, 36, 54, 32),
            Margin = new Padding(16),
            BackColor = Theme.Surface,
            CornerRadius = 14
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            BackColor = Theme.Surface
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // Logo
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Headline
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // Detail
        for (var i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // 5 Steps
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Progress
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // Spinner
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Actions

        var brand = new Label
        {
            Dock = DockStyle.Fill,
            Text = "◈",
            Font = Theme.Font(32, FontStyle.Bold),
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleCenter
        };

        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(_headline, 0, 1);
        layout.Controls.Add(_detail, 0, 2);

        var names = new[]
        {
            "Xác thực thông tin tài khoản",
            "Tải hồ sơ & phiên làm việc",
            "Kiểm tra phân quyền hệ thống",
            "Đồng bộ dữ liệu hoạt động studio",
            "Không gian làm việc đã sẵn sàng"
        };

        for (var i = 0; i < names.Length; i++)
        {
            _steps[i] = StepLabel(names[i]);
            layout.Controls.Add(_steps[i], 0, i + 3);
        }

        layout.Controls.Add(_progress, 0, 8);
        layout.Controls.Add(_spinner, 0, 9);

        _actions.Controls.Add(_retry);
        _actions.Controls.Add(_logout);
        layout.Controls.Add(_actions, 0, 10);

        card.Controls.Add(layout);
        return card;
    }

    private async Task InitializeAsync()
    {
        _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = new CancellationTokenSource();
        var ct = _initializationCts.Token;

        DashboardData = null;
        _actions.Visible = false;
        _spinner.Running = true;
        _headline.Text = "Đang khởi tạo workspace...";
        _detail.Text = "StudioManager đang chuẩn bị không gian làm việc của bạn";
        ResetSteps();

        try
        {
            CompleteStep(0, 20); // Authentication completed before this form is shown.
            if (_app.Session is null) throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");

            CompleteStep(1, 40);
            _ = _app.Session.VaiTro switch
            {
                VaiTro.QuanTriVien => true,
                VaiTro.NhanVien => true,
                _ => throw new UnauthorizedAccessException("Vai trò tài khoản không hợp lệ.")
            };

            CompleteStep(2, 55);
            SetActiveStep(3, "Đang tải dữ liệu vận hành studio...");

            DashboardData = await _app.Dashboard.LoadAsync(ct);

            CompleteStep(3, 85);
            SetActiveStep(4, "Đang chuẩn bị bảng điều khiển...");

            CompleteStep(4, 100);
            _headline.Text = "Không gian làm việc sẵn sàng";
            _detail.Text = "Đang mở bảng điều khiển...";
            _spinner.Running = false;

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _spinner.Running = false;
            _headline.Text = "Không thể khởi tạo workspace";
            _detail.Text = FriendlyError(ex);
            MarkCurrentFailed();
            _actions.Visible = true;
        }
    }

    private void ResetSteps()
    {
        for (var i = 0; i < _steps.Length; i++)
        {
            _steps[i].Text = "○   " + StepName(i);
            _steps[i].ForeColor = Theme.Muted;
        }
        _progress.Value = 0;
    }

    private void CompleteStep(int index, int progress)
    {
        _steps[index].Text = "✓   " + StepName(index);
        _steps[index].ForeColor = Theme.Success;
        _progress.Value = progress;
    }

    private void SetActiveStep(int index, string detail)
    {
        _steps[index].Text = "◌   " + StepName(index);
        _steps[index].ForeColor = Theme.Primary;
        _detail.Text = detail;
    }

    private void MarkCurrentFailed()
    {
        var current = Array.FindIndex(_steps, x => x.Text.StartsWith("◌", StringComparison.Ordinal));
        if (current >= 0)
        {
            _steps[current].Text = "⚠   " + StepName(current);
            _steps[current].ForeColor = Theme.Danger;
        }
    }

    private static string StepName(int index) => new[]
    {
        "Xác thực thông tin tài khoản",
        "Tải hồ sơ & phiên làm việc",
        "Kiểm tra phân quyền hệ thống",
        "Đồng bộ dữ liệu hoạt động studio",
        "Không gian làm việc đã sẵn sàng"
    }[index];

    private static Label StepLabel(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = "○   " + text,
        Font = Theme.Font(9.5f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(20, 0, 0, 0)
    };

    private static string FriendlyError(Exception ex) =>
        ex.Message.Contains("SQL", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            ? "Không thể kết nối cơ sở dữ liệu SQL Server. Vui lòng kiểm tra dịch vụ và thử lại."
            : ex.Message;

    private void CenterActions()
    {
        var contentWidth = _retry.Width + _logout.Width + _retry.Margin.Horizontal + _logout.Margin.Horizontal;
        _actions.Padding = new Padding(Math.Max(0, (_actions.ClientSize.Width - contentWidth) / 2), 0, 0, 0);
    }
}

internal sealed class MilestoneProgressBar : Control
{
    private int _value;
    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public MilestoneProgressBar()
    {
        DoubleBuffered = true;
        Height = 14;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var track = new Rectangle(0, Math.Max(0, Height / 2 - 4), Math.Max(1, Width - 1), 8);
        using var trackPath = Theme.RoundedRectangle(new RectangleF(track.X, track.Y, track.Width, track.Height), 4);
        using var trackBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
        g.FillPath(trackBrush, trackPath);

        if (_value <= 0) return;

        var fillWidth = Math.Max(8, track.Width * _value / 100);
        var fillRect = new Rectangle(0, track.Y, fillWidth, track.Height);
        using var fillPath = Theme.RoundedRectangle(new RectangleF(fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height), 4);
        using var fillBrush = new LinearGradientBrush(fillRect, Theme.Primary, Theme.Cyan, 0f);
        g.FillPath(fillBrush, fillPath);
    }
}

internal sealed class SpinnerControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 60 };
    private int _angle;
    private bool _running;

    public bool Running
    {
        get => _running;
        set
        {
            _running = value;
            if (value) _timer.Start();
            else _timer.Stop();
            Invalidate();
        }
    }

    public SpinnerControl()
    {
        DoubleBuffered = true;
        _timer.Tick += (_, _) => { _angle = (_angle + 30) % 360; Invalidate(); };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!_running) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        const int size = 20;
        var x = (Width - size) / 2;
        var y = (Height - size) / 2;

        using var pen = new Pen(Theme.Primary, 2.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawArc(pen, x, y, size, size, _angle, 250);
    }
}
