using System.Drawing.Drawing2D;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;

namespace StudioManager.WinForms.Forms;

public sealed class ApplicationInitializationForm : Form
{
    private readonly AppFacade _app;
    private readonly Label _headline = new() { Dock = DockStyle.Fill, Text = "Đang khởi tạo workspace...", Font = Theme.Font(15, FontStyle.Bold), ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _detail = new() { Dock = DockStyle.Fill, Text = "StudioManager đang chuẩn bị không gian làm việc của bạn", Font = Theme.Font(9.5f), ForeColor = Theme.Muted, TextAlign = ContentAlignment.TopCenter };
    private readonly Label[] _steps = new Label[5];
    private readonly MilestoneProgressBar _progress = new() { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10) };
    private readonly SpinnerControl _spinner = new() { Dock = DockStyle.Fill };
    private readonly Button _retry = Theme.Button("↻  Thử lại");
    private readonly Button _logout = Theme.Button("Quay lại đăng nhập", Color.FromArgb(100, 116, 139));
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Visible = false };
    private CancellationTokenSource? _initializationCts;
    public DashboardData? DashboardData { get; private set; }

    public ApplicationInitializationForm(AppFacade app)
    {
        _app = app; Text = "Đang khởi tạo • StudioManager"; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(680, 620); MinimumSize = new Size(600, 560); MaximizeBox = false; MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle; BackColor = Theme.Background; Font = Theme.Font();
        Controls.Add(BuildCard());
        _retry.Width = 126; _logout.Width = 174;
        _actions.ClientSizeChanged += (_, _) => CenterActions();
        Shown += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => _initializationCts?.Cancel();
        _retry.Click += async (_, _) => await InitializeAsync();
        _logout.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
    }

    private Control BuildCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(76, 42, 76, 38), BackColor = Theme.Surface };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 11, BackColor = Theme.Surface };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        for (var i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var brand = new Panel { Dock = DockStyle.Fill };
        brand.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "▣", Font = Theme.Font(38, FontStyle.Bold), ForeColor = Theme.Primary, TextAlign = ContentAlignment.MiddleCenter });
        layout.Controls.Add(brand, 0, 0); layout.Controls.Add(_headline, 0, 1); layout.Controls.Add(_detail, 0, 2);
        var names = new[] { "Xác thực tài khoản", "Tải thông tin người dùng", "Kiểm tra quyền truy cập", "Tải dữ liệu studio", "Workspace sẵn sàng" };
        for (var i = 0; i < names.Length; i++) { _steps[i] = StepLabel(names[i]); layout.Controls.Add(_steps[i], 0, i + 3); }
        layout.Controls.Add(_progress, 0, 8); layout.Controls.Add(_spinner, 0, 9);
        _actions.Controls.Add(_retry); _actions.Controls.Add(_logout); layout.Controls.Add(_actions, 0, 10); card.Controls.Add(layout); return card;
    }

    private async Task InitializeAsync()
    {
        _initializationCts?.Cancel(); _initializationCts?.Dispose(); _initializationCts = new CancellationTokenSource(); var ct = _initializationCts.Token;
        DashboardData = null; _actions.Visible = false; _spinner.Running = true; _headline.Text = "Đang khởi tạo workspace..."; _detail.Text = "StudioManager đang chuẩn bị không gian làm việc của bạn"; ResetSteps();
        try
        {
            CompleteStep(0, 20); // Authentication completed before this form is shown.
            if (_app.Session is null) throw new InvalidOperationException("Phiên đăng nhập không còn hợp lệ.");
            CompleteStep(1, 40);
            _ = _app.Session.VaiTro switch { VaiTro.QuanTriVien => true, VaiTro.NhanVien => true, _ => throw new UnauthorizedAccessException("Vai trò tài khoản không hợp lệ.") };
            CompleteStep(2, 55); SetActiveStep(3, "Đang tải dữ liệu studio...");
            DashboardData = await _app.Dashboard.LoadAsync(ct);
            CompleteStep(3, 85); SetActiveStep(4, "Đang chuẩn bị Dashboard...");
            CompleteStep(4, 100); _headline.Text = "Workspace sẵn sàng"; _detail.Text = "Đang mở Dashboard..."; _spinner.Running = false;
            DialogResult = DialogResult.OK; Close();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _spinner.Running = false; _headline.Text = "Không thể khởi tạo workspace"; _detail.Text = FriendlyError(ex); MarkCurrentFailed(); _actions.Visible = true;
        }
    }

    private void ResetSteps() { for (var i = 0; i < _steps.Length; i++) { _steps[i].Text = "○  " + StepName(i); _steps[i].ForeColor = Theme.Muted; } _progress.Value = 0; }
    private void CompleteStep(int index, int progress) { _steps[index].Text = "✓  " + StepName(index); _steps[index].ForeColor = Theme.Success; _progress.Value = progress; }
    private void SetActiveStep(int index, string detail) { _steps[index].Text = "◌  " + StepName(index); _steps[index].ForeColor = Theme.Primary; _detail.Text = detail; }
    private void MarkCurrentFailed() { var current = Array.FindIndex(_steps, x => x.Text.StartsWith("◌", StringComparison.Ordinal)); if (current >= 0) { _steps[current].Text = "⚠  " + StepName(current); _steps[current].ForeColor = Theme.Danger; } }
    private static string StepName(int index) => new[] { "Xác thực tài khoản", "Tải thông tin người dùng", "Kiểm tra quyền truy cập", "Tải dữ liệu studio", "Workspace sẵn sàng" }[index];
    private static Label StepLabel(string text) => new() { Dock = DockStyle.Fill, Text = "○  " + text, Font = Theme.Font(10), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0) };
    private static string FriendlyError(Exception ex) => ex.Message.Contains("SQL", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ? "Không thể kết nối tới dữ liệu cần thiết. Kiểm tra SQL Server rồi thử lại." : ex.Message;
    private void CenterActions() { var contentWidth = _retry.Width + _logout.Width + _retry.Margin.Horizontal + _logout.Margin.Horizontal; _actions.Padding = new Padding(Math.Max(0, (_actions.ClientSize.Width - contentWidth) / 2), 0, 0, 0); }
}

internal sealed class MilestoneProgressBar : Control
{
    private int _value; public int Value { get => _value; set { _value = Math.Clamp(value, 0, 100); Invalidate(); } }
    public MilestoneProgressBar() { DoubleBuffered = true; Height = 18; }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; var track = new Rectangle(0, Math.Max(0, Height / 2 - 5), Math.Max(1, Width - 1), 10); using var trackPath = Rounded(track, 5); using var trackBrush = new SolidBrush(Color.FromArgb(226, 232, 240)); e.Graphics.FillPath(trackBrush, trackPath); if (_value <= 0) return; var fillRect = new Rectangle(0, track.Y, Math.Max(10, track.Width * _value / 100), track.Height); using var fillPath = Rounded(fillRect, 5); using var fillBrush = new LinearGradientBrush(fillRect, Theme.Primary, Theme.Cyan, 0f); e.Graphics.FillPath(fillBrush, fillPath); }
    private static GraphicsPath Rounded(Rectangle r, int radius) { var p = new GraphicsPath(); var d = radius * 2; p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
}

internal sealed class SpinnerControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 70 }; private int _angle; private bool _running;
    public bool Running { get => _running; set { _running = value; if (value) _timer.Start(); else _timer.Stop(); Invalidate(); } }
    public SpinnerControl() { DoubleBuffered = true; _timer.Tick += (_, _) => { _angle = (_angle + 30) % 360; Invalidate(); }; }
    protected override void Dispose(bool disposing) { if (disposing) _timer.Dispose(); base.Dispose(disposing); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); if (!_running) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; var size = 22; var x = (Width - size) / 2; var y = (Height - size) / 2; using var pen = new Pen(Theme.Primary, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round }; e.Graphics.DrawArc(pen, x, y, size, size, _angle, 245); }
}
