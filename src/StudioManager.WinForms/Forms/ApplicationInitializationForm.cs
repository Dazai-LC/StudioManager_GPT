using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
        Text = "Đang hiệu chuẩn không gian làm việc...",
        Font = Theme.Font(15.5f, FontStyle.Bold),
        ForeColor = Theme.Text,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Label _detail = new()
    {
        Dock = DockStyle.Fill,
        Text = "Đang đồng bộ danh mục phòng chụp, hồ sơ nhân sự và trạng thái lịch chụp",
        Font = Theme.Font(9.2f),
        ForeColor = Theme.Muted,
        TextAlign = ContentAlignment.TopLeft
    };

    private readonly CalibrationStepperControl _stepper = new()
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 8, 0, 8)
    };

    private readonly ApertureSpinnerControl _spinner = new()
    {
        Width = 64,
        Height = 64,
        Dock = DockStyle.Left
    };

    private readonly ModernButton _retry = Theme.Button("Thử lại kết nối", Theme.Primary, IconKind.Refresh);
    private readonly ModernButton _logout = Theme.OutlineButton("Quay lại đăng nhập", Theme.BorderStrong, IconKind.LogOut);

    private readonly FlowLayoutPanel _actions = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        Visible = false,
        BackColor = Color.Transparent
    };

    private CancellationTokenSource? _initializationCts;
    public DashboardData? DashboardData { get; private set; }
    public Exception? LastError { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public ApplicationInitializationForm(AppFacade app)
    {
        _app = app;
        Text = "Hiệu chuẩn hệ thống • StudioManager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(740, 440);
        MinimumSize = new Size(700, 410);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Theme.Background;
        Font = Theme.Font();

        Controls.Add(BuildConsoleCard());

        Shown += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => _initializationCts?.Cancel();
        _retry.Click += async (_, _) => await InitializeAsync();
        _logout.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
    }

    private Control BuildConsoleCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(36, 26, 36, 22),
            Margin = new Padding(18),
            BackColor = Theme.Surface,
            BorderColor = Theme.BorderStrong,
            CornerRadius = 12,
            AccentStripeColor = Theme.Primary,
            AccentStripeThickness = 4
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));  // Top Header Row (Aperture Spinner + Headline)
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Horizontal 5-Node Stepper
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));  // Telemetry Status Strip
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Action Row

        var topHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var textCol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 2, 0, 0), BackColor = Color.Transparent };
        textCol.Controls.Add(_detail);
        textCol.Controls.Add(_headline);
        _headline.Dock = DockStyle.Top;
        _headline.Height = 36;

        topHeader.Controls.Add(textCol);
        topHeader.Controls.Add(_spinner);

        var telemetryStrip = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceWarm,
            BorderColor = Theme.Border,
            CornerRadius = 8,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 4, 0, 4)
        };
        var sessionUser = _app.Session?.HoTen ?? "Chưa xác định";
        var sessionRole = _app.Session?.VaiTro == VaiTro.QuanTriVien ? "Quản trị viên (Toàn quyền)" : "Nhân viên vận hành";
        var sessionLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Phiên làm việc: {sessionUser}   •   Phân quyền: {sessionRole}",
            Font = Theme.Font(8.8f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        telemetryStrip.Controls.Add(sessionLabel);

        _actions.Controls.Add(_retry);
        _actions.Controls.Add(_logout);

        layout.Controls.Add(topHeader, 0, 0);
        layout.Controls.Add(_stepper, 0, 1);
        layout.Controls.Add(telemetryStrip, 0, 2);
        layout.Controls.Add(_actions, 0, 3);

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
        LastError = null;
        LastErrorMessage = null;
        _actions.Visible = false;
        _spinner.Running = true;
        _headline.Text = "Đang hiệu chuẩn không gian làm việc...";
        _detail.ForeColor = Theme.Muted;
        _detail.Text = "StudioManager đang chuẩn bị dữ liệu và kiểm tra phân quyền truy cập";
        _stepper.Reset();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _stepper.SetStepState(0, StepNodeState.Completed);
            if (_app.Session is null) throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");

            // Smooth async step cadence giving user clear visual feedback
            await Task.Delay(130, ct);
            _stepper.SetStepState(1, StepNodeState.Completed);
            _ = _app.Session.VaiTro switch
            {
                VaiTro.QuanTriVien => true,
                VaiTro.NhanVien => true,
                _ => throw new UnauthorizedAccessException("Vai trò tài khoản không hợp lệ.")
            };

            await Task.Delay(130, ct);
            _stepper.SetStepState(2, StepNodeState.Completed);
            _stepper.SetStepState(3, StepNodeState.Active);
            _detail.Text = "Đang nạp dữ liệu lịch chụp, doanh thu và chỉ số vận hành studio...";

            DashboardData = await _app.Dashboard.LoadAsync(ct);

            _stepper.SetStepState(3, StepNodeState.Completed);
            _stepper.SetStepState(4, StepNodeState.Completed);
            _headline.Text = "Không gian làm việc đã sẵn sàng";
            _detail.Text = "Đang mở bàn điều khiển trung tâm...";
            _spinner.Running = false;

            // Maintain gentle ~900-1100ms hold when local database responds unusually fast
            var elapsed = sw.ElapsedMilliseconds;
            if (elapsed < 950)
            {
                await Task.Delay((int)(950 - elapsed), ct);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            LastError = ex;
            Ui.LogError("ApplicationInitializationForm.InitializeAsync", ex);
            LastErrorMessage = FriendlyError(ex);
            _spinner.Running = false;
            _headline.Text = "Không thể khởi tạo không gian làm việc";
            _detail.ForeColor = Theme.Danger;
            _detail.Text = LastErrorMessage;
            _stepper.MarkActiveFailed();
            _actions.Visible = true;
        }
    }

    private static string FriendlyError(Exception ex) =>
        Ui.IsDatabaseException(ex)
            ? $"Không thể kết nối cơ sở dữ liệu SQL Server ({ex.GetType().Name}): {ex.Message}"
            : $"Lỗi khởi tạo dữ liệu ({ex.GetType().Name}): {ex.Message}";
}

internal enum StepNodeState
{
    Pending,
    Active,
    Completed,
    Failed
}

internal sealed class CalibrationStepperControl : Control
{
    private static readonly (string Code, string Title, string Subtitle)[] Steps =
    [
        ("01", "Xác thực", "Kiểm tra thông tin"),
        ("02", "Phiên làm việc", "Nạp hồ sơ nhân sự"),
        ("03", "Phân quyền", "Áp dụng vai trò"),
        ("04", "Đồng bộ SQL", "Tải lịch & doanh thu"),
        ("05", "Sẵn sàng", "Mở bàn làm việc")
    ];

    private readonly StepNodeState[] _states = new StepNodeState[5];

    public CalibrationStepperControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
    }

    public void Reset()
    {
        for (var i = 0; i < _states.Length; i++) _states[i] = StepNodeState.Pending;
        Invalidate();
    }

    public void SetStepState(int index, StepNodeState state)
    {
        if (index < 0 || index >= _states.Length) return;
        _states[index] = state;
        Invalidate();
    }

    public void MarkActiveFailed()
    {
        var active = Array.FindIndex(_states, x => x == StepNodeState.Active);
        if (active >= 0) _states[active] = StepNodeState.Failed;
        else _states[0] = StepNodeState.Failed;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var count = Steps.Length;
        var slotW = Width / (float)count;
        var nodeY = Math.Max(38f, Height / 2f - 22f);
        const float nodeR = 18f;

        // Draw connecting tracks first
        for (var i = 0; i < count - 1; i++)
        {
            var x1 = slotW * i + slotW / 2f + nodeR;
            var x2 = slotW * (i + 1) + slotW / 2f - nodeR;
            var done = _states[i] == StepNodeState.Completed;
            using var trackPen = new Pen(done ? Theme.Success : Theme.Primitives.Alabaster200, 3f);
            g.DrawLine(trackPen, x1, nodeY, x2, nodeY);
        }

        using var codeFont = Theme.MonoFont(8.5f, FontStyle.Bold);
        using var titleFont = Theme.Font(9.0f, FontStyle.Bold);
        using var subFont = Theme.Font(7.8f);

        for (var i = 0; i < count; i++)
        {
            var cx = slotW * i + slotW / 2f;
            var state = _states[i];
            var (fill, border, fg) = state switch
            {
                StepNodeState.Completed => (Theme.SuccessLight, Theme.Success, Theme.Success),
                StepNodeState.Active => (Theme.PrimaryLight, Theme.Primary, Theme.Primary),
                StepNodeState.Failed => (Theme.DangerLight, Theme.Danger, Theme.Danger),
                _ => (Theme.Primitives.Alabaster100, Theme.BorderStrong, Theme.Muted)
            };

            var circle = new RectangleF(cx - nodeR, nodeY - nodeR, nodeR * 2, nodeR * 2);
            using (var fillBrush = new SolidBrush(fill))
            using (var borderPen = new Pen(border, state == StepNodeState.Active ? 2.2f : 1.5f))
            {
                g.FillEllipse(fillBrush, circle);
                g.DrawEllipse(borderPen, circle);
            }

            if (state == StepNodeState.Completed)
            {
                VectorIcons.Draw(g, IconKind.Check, new RectangleF(cx - 8, nodeY - 8, 16, 16), fg, 2.0f);
            }
            else if (state == StepNodeState.Failed)
            {
                VectorIcons.Draw(g, IconKind.AlertTriangle, new RectangleF(cx - 8, nodeY - 8, 16, 16), fg, 1.9f);
            }
            else
            {
                var codeSize = g.MeasureString(Steps[i].Code, codeFont);
                using var fgBrush = new SolidBrush(fg);
                g.DrawString(Steps[i].Code, codeFont, fgBrush, cx - codeSize.Width / 2f, nodeY - codeSize.Height / 2f);
            }

            var tSize = g.MeasureString(Steps[i].Title, titleFont);
            using var titleBrush = new SolidBrush(state == StepNodeState.Pending ? Theme.Muted : Theme.Text);
            g.DrawString(Steps[i].Title, titleFont, titleBrush, cx - tSize.Width / 2f, nodeY + nodeR + 8f);

            var sSize = g.MeasureString(Steps[i].Subtitle, subFont);
            using var subBrush = new SolidBrush(Theme.Muted);
            g.DrawString(Steps[i].Subtitle, subFont, subBrush, cx - sSize.Width / 2f, nodeY + nodeR + 26f);
        }
    }
}

internal sealed class ApertureSpinnerControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 45 };
    private float _angle;
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

    public ApertureSpinnerControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
        _timer.Tick += (_, _) =>
        {
            _angle = (_angle + 12f) % 360f;
            Invalidate();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var cx = Width / 2f;
        var cy = Height / 2f;
        var r = Math.Min(Width, Height) * 0.40f;

        using var bgFill = new SolidBrush(Theme.PrimaryLight);
        g.FillEllipse(bgFill, cx - r - 4, cy - r - 4, (r + 4) * 2, (r + 4) * 2);

        var state = g.Save();
        g.TranslateTransform(cx, cy);
        if (_running) g.RotateTransform(_angle);
        g.TranslateTransform(-cx, -cy);

        VectorIcons.Draw(g, IconKind.Aperture, new RectangleF(cx - r, cy - r, r * 2, r * 2), Theme.Primary, 2.0f);
        g.Restore(state);
    }
}
