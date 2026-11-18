using System.Drawing.Drawing2D;
using System.Windows.Forms;
using StudioManager.CustomerApi;
using StudioManager.WinForms;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Forms;

namespace StudioManager.CustomerDesktop;

/// <summary>
/// Operational staff workspace for reviewing, approving, rejecting, or revoking customer identity links.
/// Preserves the core security rule that staff must independently verify the studio record (KhachHangId)
/// before approval, without relying on email or phone coincidence.
/// </summary>
internal sealed class StaffLinkReviewWorkspace : UserControl
{
    private readonly Func<IdentityRequest, Task<IdentityResponse>> _send;
    private readonly Action<IdentityResponse> _show;
    private readonly Func<Func<Task>, Task> _execute;

    private readonly List<LinkReviewDto> _reviews = [];
    private LinkReviewDto? _selected;
    private long? _beforeLinkId;
    private readonly Stack<long?> _reviewHistory = new();
    private int _currentPageIndex = 1;
    private bool _initialized;
    private bool _busy;
    private bool _updatingGrid;

    // Root UI elements
    private readonly Label _titleLabel = CustomerUi.Text("Duyệt liên kết hồ sơ", 15f, true, Theme.Text, 0, 2);
    private readonly Label _subtitleLabel = CustomerUi.Text("Nhân sự đối chiếu trực tiếp hồ sơ khách hàng tại studio trước khi phê duyệt quyền truy cập lịch sử. Không tự động ghép theo email hay số điện thoại.", 9.2f, false, Theme.Muted, 0, 4);
    private readonly Chip _countChip = new("0 yêu cầu", Theme.TextSecondary, Theme.BackgroundSubtle);
    private readonly ModernButton _btnHeaderReload;

    // Left Section (Master — Request List)
    private readonly CardPanel _listCard = new() { Dock = DockStyle.Fill, CornerRadius = 12, Padding = new Padding(16, 14, 16, 14), Margin = Padding.Empty, BackColor = Theme.Surface, BorderColor = Theme.Border };
    private readonly DataGridView _grid = new();
    private readonly Panel _emptyPanel = new() { Dock = DockStyle.Fill, BackColor = Color.Empty, Visible = false };
    private readonly Panel _gridContainer = new() { Dock = DockStyle.Fill, BackColor = Color.Empty };
    private readonly FlowLayoutPanel _pagination = new() { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Empty, Padding = new Padding(0, 6, 0, 0) };
    private readonly ModernButton _btnPrev;
    private readonly ModernButton _btnNext;
    private readonly Label _lblPageInfo = CustomerUi.Text("Trang 1", 9f, false, Theme.Muted, 8, 4);

    // Right Section (Detail — Review & Decision)
    private readonly Panel _detailScroll = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Empty };
    private readonly CardPanel _noSelectionCard = new() { CornerRadius = 12, Padding = new Padding(24), Margin = Padding.Empty, BackColor = Theme.Surface, BorderColor = Theme.Border };
    private readonly CardPanel _detailCard = new() { CornerRadius = 12, Padding = new Padding(20, 16, 20, 16), Margin = Padding.Empty, BackColor = Theme.Surface, BorderColor = Theme.Border, Visible = false };

    // Detail Controls
    private readonly Label _detailTitle = CustomerUi.Text("Chi tiết yêu cầu", 13.5f, true, Theme.Text, 0, 4);
    private readonly Chip _detailChip = new("Chờ duyệt", Theme.Warning, Theme.WarningLight);
    private readonly Label _valCustomer = CustomerUi.Text("", 10.5f, true, Theme.Text, 0, 2);
    private readonly Label _valEmail = CustomerUi.Text("", 9.2f, false, Theme.TextSecondary, 0, 2);
    private readonly Label _valAccount = CustomerUi.Text("", 8.8f, false, Theme.Muted, 0, 6);
    private readonly Label _valReference = CustomerUi.Text("", 9.5f, true, Theme.Primitives.Terracotta500, 0, 4);
    private readonly Label _valEvidence = CustomerUi.Text("", 9.2f, false, Theme.Text, 0, 4);
    private readonly Label _valStudioCustomer = CustomerUi.Text("", 9.2f, true, Theme.Muted, 0, 4);

    // Review Inputs
    private readonly TextBox _customerIdBox = new() { PlaceholderText = "Ví dụ: 1042", MaxLength = 18 };
    private readonly TextBox _reasonBox = new() { PlaceholderText = "Ghi chú đối chiếu hồ sơ hoặc lý do quyết định...", MaxLength = 500 };
    private readonly ModernButton _btnApprove;
    private readonly ModernButton _btnReject;
    private readonly ModernButton _btnRevoke;
    private readonly Label _lblActionFeedback = CustomerUi.Text("", 9.2f, true, Theme.Danger, 6, 4);

    public DataGridView Grid => _grid;
    public TextBox CustomerIdBox => _customerIdBox;
    public TextBox ReasonBox => _reasonBox;

    public StaffLinkReviewWorkspace(Func<IdentityRequest, Task<IdentityResponse>> send, Action<IdentityResponse> show, Func<Func<Task>, Task> execute)
    {
        _send = send;
        _show = show;
        _execute = execute;

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(24, 16, 24, 16);

        _btnHeaderReload = AtelierUi.Action("Tải lại danh sách", false, IconKind.History);
        _btnHeaderReload.Click += async (_, _) => await _execute(ReloadAsync);

        _btnPrev = AtelierUi.Action("← Trang trước", false, IconKind.None);
        _btnPrev.Height = 34;
        _btnPrev.Click += async (_, _) => await _execute(PrevPageAsync);

        _btnNext = AtelierUi.Action("Trang tiếp →", false, IconKind.None);
        _btnNext.Height = 34;
        _btnNext.Click += async (_, _) => await _execute(NextPageAsync);

        _btnApprove = AtelierUi.Action("Duyệt liên kết", true, IconKind.Check);
        _btnApprove.Click += async (_, _) => await _execute(ApproveAsync);

        _btnReject = AtelierUi.Action("Từ chối", false, IconKind.Close);
        _btnReject.ForeColor = Theme.Danger;
        _btnReject.BorderColor = Theme.DangerLight;
        _btnReject.Click += async (_, _) => await _execute(RejectAsync);

        _btnRevoke = AtelierUi.Action("Thu hồi liên kết", false, IconKind.Trash);
        _btnRevoke.ForeColor = Theme.Danger;
        _btnRevoke.BorderColor = Theme.DangerLight;
        _btnRevoke.Click += async (_, _) => await _execute(RevokeAsync);

        BuildLayout();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        _beforeLinkId = null;
        _reviewHistory.Clear();
        _currentPageIndex = 1;
        await LoadReviewsAsync();
    }

    public void Reset()
    {
        _initialized = false;
        _updatingGrid = true;
        try
        {
            _reviews.Clear();
            _selected = null;
            _beforeLinkId = null;
            _reviewHistory.Clear();
            _currentPageIndex = 1;
            _grid.DataSource = null;
            _countChip.UpdateStyle("0 yêu cầu", Theme.TextSecondary, Theme.BackgroundSubtle);
            _customerIdBox.Clear();
            _reasonBox.Clear();
            _lblActionFeedback.Text = "";
            _emptyPanel.Visible = true;
            _emptyPanel.BringToFront();
            _grid.Visible = false;
            _pagination.Visible = false;
            SelectReview(null);
        }
        finally
        {
            _updatingGrid = false;
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Top Action Bar
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Empty, Margin = Padding.Empty };
        var titleFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Empty,
            Padding = new Padding(0, 6, 0, 0)
        };
        titleFlow.Controls.Add(CustomerUi.Text("HÀNG ĐỢI ĐỐI CHIẾU HỒ SƠ", 9.8f, true, Theme.Text, 0, 0));
        titleFlow.Controls.Add(CustomerUi.Text("· Nhân sự đối chiếu trực tiếp hồ sơ trước khi duyệt", 8.8f, false, Theme.Muted, 2, 0));

        var actionFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Empty,
            Padding = new Padding(0, 0, 0, 0)
        };
        actionFlow.Controls.Add(_btnHeaderReload);
        actionFlow.Controls.Add(_countChip);

        header.Controls.Add(titleFlow);
        header.Controls.Add(actionFlow);
        root.Controls.Add(header, 0, 0);

        // Content Master-Detail
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildLeftSection();
        BuildRightSection();

        content.Controls.Add(_listCard, 0, 0);
        content.Controls.Add(_detailScroll, 1, 0);
        root.Controls.Add(content, 0, 1);

        Controls.Add(root);
    }

    private void BuildLeftSection()
    {
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Empty };
        var listTitle = CustomerUi.Text("DANH SÁCH YÊU CẦU LIÊN KẾT", 10.5f, true, Theme.Text, 0, 2);
        var listSub = CustomerUi.Text("Chọn một yêu cầu để xem thông tin và đối chiếu hồ sơ studio", 8.8f, false, Theme.Muted, 0, 6);
        headerPanel.Controls.Add(listSub);
        headerPanel.Controls.Add(listTitle);
        listSub.Top = 20;

        ConfigureGrid();
        BuildEmptyState();

        _gridContainer.Controls.Add(_emptyPanel);
        _gridContainer.Controls.Add(_grid);

        // Pagination
        _pagination.Controls.Add(_btnPrev);
        _pagination.Controls.Add(_lblPageInfo);
        _pagination.Controls.Add(_btnNext);

        _listCard.Controls.Add(headerPanel);
        _listCard.Controls.Add(_pagination);
        _listCard.Controls.Add(_gridContainer);
        _gridContainer.BringToFront();
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Theme.Surface;
        _grid.BorderStyle = BorderStyle.None;
        _grid.GridColor = Theme.BorderSubtle;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.RowTemplate.Height = 44;

        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 36;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.SurfaceWarm,
            ForeColor = Theme.TextSecondary,
            Font = Theme.Font(8.8f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0)
        };

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Font = Theme.Font(9.2f),
            SelectionBackColor = Theme.PrimaryLight,
            SelectionForeColor = Theme.PrimaryPressed,
            Padding = new Padding(6, 0, 6, 0)
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.SurfaceWarm,
            ForeColor = Theme.Text,
            Font = Theme.Font(9.2f),
            SelectionBackColor = Theme.PrimaryLight,
            SelectionForeColor = Theme.PrimaryPressed,
            Padding = new Padding(6, 0, 6, 0)
        };

        var colId = new DataGridViewTextBoxColumn
        {
            Name = "Id",
            DataPropertyName = "Id",
            HeaderText = "Mã YC",
            Width = 60,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        var colState = new DataGridViewTextBoxColumn
        {
            Name = "State",
            DataPropertyName = "State",
            HeaderText = "Trạng thái",
            Width = 96,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        var colCustomer = new DataGridViewTextBoxColumn
        {
            Name = "FullName",
            DataPropertyName = "FullName",
            HeaderText = "Khách hàng",
            Width = 120
        };

        var colEmail = new DataGridViewTextBoxColumn
        {
            Name = "LoginEmail",
            DataPropertyName = "LoginEmail",
            HeaderText = "Email đăng nhập",
            Width = 135
        };

        var colReference = new DataGridViewTextBoxColumn
        {
            Name = "Reference",
            DataPropertyName = "Reference",
            HeaderText = "Mã t.chiếu",
            Width = 95
        };

        var colCustomerId = new DataGridViewTextBoxColumn
        {
            Name = "CustomerId",
            DataPropertyName = "CustomerId",
            HeaderText = "Mã KH",
            Width = 68,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        };

        var colEvidence = new DataGridViewTextBoxColumn
        {
            Name = "Evidence",
            DataPropertyName = "Evidence",
            HeaderText = "Ghi chú đối chiếu",
            MinimumWidth = 110,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };

        _grid.Columns.AddRange(colId, colState, colCustomer, colEmail, colReference, colCustomerId, colEvidence);

        // Custom chip painting for State column
        _grid.CellPainting += (s, e) =>
        {
            if (e.Graphics is not null && e.RowIndex >= 0 && e.ColumnIndex == colState.Index && e.Value is string state)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                var (text, fore, back) = ReviewStateStyle(state);
                var pillRect = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + (e.CellBounds.Height - 22) / 2, e.CellBounds.Width - 8, 22);
                using var path = CustomerUi.Round(pillRect, 8);
                using var fill = new SolidBrush(back);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(fill, path);
                using var font = Theme.Font(8.0f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, text, font, pillRect, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                e.Handled = true;
            }
        };

        _grid.SelectionChanged += (_, _) =>
        {
            if (_updatingGrid) return;
            try
            {
                if (_grid.Rows.Count > 0 && _grid.CurrentRow is { Index: >= 0 } row && row.Index < _reviews.Count)
                    SelectReview((row.DataBoundItem as LinkReviewDto) ?? _reviews[row.Index]);
                else
                    SelectReview(null);
            }
            catch
            {
                SelectReview(null);
            }
        };

        _grid.CellClick += (_, e) =>
        {
            if (_updatingGrid) return;
            if (e.RowIndex >= 0 && e.RowIndex < _reviews.Count)
                SelectReview(_reviews[e.RowIndex]);
        };
    }

    private void BuildEmptyState()
    {
        var box = new Panel { Dock = DockStyle.Fill, BackColor = Color.Empty };
        box.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var cx = box.Width / 2f;
            var cy = box.Height / 2f - 40;
            VectorIcons.Draw(e.Graphics, IconKind.UserCheck, new RectangleF(cx - 24, cy - 24, 48, 48), Theme.Primitives.Stone400, 1.8f);
        };

        var emptyTitle = CustomerUi.Text("Không có yêu cầu cần duyệt", 13f, true, Theme.Text, 0, 4);
        var emptyDesc = CustomerUi.Text("Hiện tại không có yêu cầu liên kết hồ sơ nào trong hàng đợi.\nKhi khách hàng gửi yêu cầu liên kết từ tài khoản, thông tin sẽ hiển thị tại đây.", 9.2f, false, Theme.Muted, 0, 16);
        emptyDesc.TextAlign = ContentAlignment.TopCenter;
        var reloadBtn = AtelierUi.Action("Tải lại danh sách", false, IconKind.History);
        reloadBtn.Click += async (_, _) => await _execute(ReloadAsync);

        void PositionElements()
        {
            if (box.Width <= 0 || box.Height <= 0) return;
            emptyTitle.Location = new Point(Math.Max(20, (box.Width - emptyTitle.PreferredWidth) / 2), box.Height / 2 + 10);
            emptyDesc.Location = new Point(Math.Max(20, (box.Width - emptyDesc.PreferredWidth) / 2), emptyTitle.Bottom + 6);
            reloadBtn.Location = new Point(Math.Max(20, (box.Width - reloadBtn.Width) / 2), emptyDesc.Bottom + 12);
            box.Invalidate();
        }

        box.Resize += (_, _) => PositionElements();
        _emptyPanel.VisibleChanged += (_, _) => { if (_emptyPanel.Visible) PositionElements(); };

        box.Controls.Add(emptyTitle);
        box.Controls.Add(emptyDesc);
        box.Controls.Add(reloadBtn);
        _emptyPanel.Controls.Add(box);
    }

    private void BuildRightSection()
    {
        // No Selection State
        _noSelectionCard.Dock = DockStyle.Fill;
        var noSelBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.Empty };
        noSelBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var cx = noSelBox.Width / 2f;
            var cy = noSelBox.Height / 2f - 30;
            VectorIcons.Draw(e.Graphics, IconKind.Users, new RectangleF(cx - 20, cy - 20, 40, 40), Theme.Primitives.Stone400, 1.8f);
        };
        var noSelTitle = CustomerUi.Text("Chưa chọn yêu cầu", 12.5f, true, Theme.Text, 0, 4);
        var noSelDesc = CustomerUi.Text("Vui lòng chọn một yêu cầu từ danh sách bên trái để đối chiếu thông tin và thực hiện phê duyệt.", 9.2f, false, Theme.Muted, 0, 8);
        noSelBox.Resize += (_, _) =>
        {
            noSelTitle.Location = new Point(Math.Max(16, (noSelBox.Width - noSelTitle.PreferredWidth) / 2), noSelBox.Height / 2 + 16);
            noSelDesc.Location = new Point(Math.Max(16, (noSelBox.Width - noSelDesc.PreferredWidth) / 2), noSelTitle.Bottom + 6);
        };
        noSelBox.Controls.Add(noSelTitle);
        noSelBox.Controls.Add(noSelDesc);
        _noSelectionCard.Controls.Add(noSelBox);

        // Detail & Actions Card
        _detailCard.Dock = DockStyle.Top;
        _detailCard.AutoSize = true;
        _detailCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Empty,
            Padding = Padding.Empty
        };

        // Header with ID + Chip
        var headerRow = CustomerUi.Row(false);
        headerRow.Controls.Add(_detailTitle);
        headerRow.Controls.Add(_detailChip);
        flow.Controls.Add(headerRow);

        // Section 1: Customer & Account Info
        flow.Controls.Add(CustomerUi.Text("THÔNG TIN TÀI KHOẢN KHÁCH HÀNG", 8.8f, true, Theme.TextSecondary, 10, 4));
        flow.Controls.Add(_valCustomer);
        flow.Controls.Add(_valEmail);
        flow.Controls.Add(_valAccount);

        // Reference Info
        flow.Controls.Add(CustomerUi.Text("THAM CHIẾU KHÁCH CUNG CẤP", 8.8f, true, Theme.TextSecondary, 8, 2));
        flow.Controls.Add(_valReference);

        flow.Controls.Add(CustomerUi.Text("THÔNG TIN / BẰNG CHỨNG ĐỐI CHIẾU", 8.8f, true, Theme.TextSecondary, 6, 2));
        var evidenceBox = new Panel
        {
            BackColor = Theme.SurfaceWarm,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 2, 0, 6),
            Width = 420,
            AutoSize = true
        };
        evidenceBox.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, evidenceBox.Width - 1, evidenceBox.Height - 1);
        };
        evidenceBox.Controls.Add(_valEvidence);
        flow.Controls.Add(evidenceBox);

        flow.Controls.Add(CustomerUi.Text("TRẠNG THÁI HỒ SƠ STUDIO HIỆN TẠI", 8.8f, true, Theme.TextSecondary, 6, 2));
        flow.Controls.Add(_valStudioCustomer);

        // Divider
        var divider = new Panel { Height = 1, BackColor = Theme.Border, Margin = new Padding(0, 8, 0, 8), Width = 420 };
        flow.Controls.Add(divider);

        // Section 2: Verification Policy Reminder
        var policyPanel = new Panel
        {
            BackColor = Theme.WarningLight,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 0, 8),
            Width = 420,
            AutoSize = true
        };
        policyPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.WarningBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, policyPanel.Width - 1, policyPanel.Height - 1);
        };
        var policyText = CustomerUi.Text("⚠️ Nguyên tắc đối chiếu: Nhân viên phải tra cứu hồ sơ trực tiếp tại phần mềm quản trị studio nội bộ. Tuyệt đối không tự động duyệt dựa trên trùng khớp email hoặc số điện thoại.", 8.8f, false, Theme.Warning, 0, 0);
        policyText.MaximumSize = new Size(390, 0);
        policyPanel.Controls.Add(policyText);
        flow.Controls.Add(policyPanel);

        // Section 3: Review Inputs
        flow.Controls.Add(CustomerUi.Text("ĐỐI CHIẾU & QUYẾT ĐỊNH LIÊN KẾT", 8.8f, true, Theme.TextSecondary, 4, 3));

        _customerIdBox.Font = Theme.Font(10);
        _customerIdBox.BorderStyle = BorderStyle.None;
        _customerIdBox.ForeColor = Theme.Text;
        var custField = AtelierInput.CreateVectorInputField("MÃ KHÁCH HÀNG STUDIO (KHACHHANGID) *", _customerIdBox, IconKind.Database);
        custField.Width = 420;
        custField.Height = 68;
        custField.Margin = new Padding(0, 2, 0, 2);
        flow.Controls.Add(custField);
        flow.Controls.Add(CustomerUi.Text("Bắt buộc khi duyệt. Nhập số nguyên định danh khách hàng trong CSDL studio.", 8.2f, false, Theme.Muted, 0, 6));

        _reasonBox.Font = Theme.Font(9.5f);
        _reasonBox.BorderStyle = BorderStyle.None;
        _reasonBox.ForeColor = Theme.Text;
        var reasonField = AtelierInput.CreateVectorInputField("LÝ DO / GHI CHÚ ĐỐI CHIẾU *", _reasonBox, IconKind.Info);
        reasonField.Width = 420;
        reasonField.Height = 68;
        reasonField.Margin = new Padding(0, 2, 0, 2);
        flow.Controls.Add(reasonField);
        flow.Controls.Add(CustomerUi.Text("Bắt buộc cho mọi quyết định (tối đa 500 ký tự). Ghi nhận bằng chứng đã đối chiếu.", 8.2f, false, Theme.Muted, 0, 8));

        // Decision Action Buttons
        var actionRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            BackColor = Color.Empty,
            Margin = new Padding(0, 6, 0, 6),
            Padding = Padding.Empty
        };
        actionRow.Controls.Add(_btnApprove);
        actionRow.Controls.Add(_btnReject);
        actionRow.Controls.Add(_btnRevoke);
        flow.Controls.Add(actionRow);

        _lblActionFeedback.Visible = false;
        flow.Controls.Add(_lblActionFeedback);

        _detailCard.Controls.Add(flow);

        _detailScroll.Controls.Add(_detailCard);
        _detailScroll.Controls.Add(_noSelectionCard);

        _detailScroll.Resize += (_, _) =>
        {
            var availW = Math.Max(260, _detailScroll.ClientSize.Width - _detailScroll.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8);
            _noSelectionCard.Width = availW;
            _detailCard.Width = availW;
            flow.Width = availW;
            evidenceBox.Width = availW - 8;
            divider.Width = availW - 8;
            policyPanel.Width = availW - 8;
            policyText.MaximumSize = new Size(availW - 32, 0);
            custField.Width = availW - 8;
            reasonField.Width = availW - 8;
        };
    }

    private async Task LoadReviewsAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var response = await _send(new(IdentityCommand.ListLinkReviews) { BeforeLinkId = _beforeLinkId });
            if (IsDisposed || Disposing) return;

            _show(response);
            _reviews.Clear();
            if (response.Reviews is { Count: > 0 } list)
                _reviews.AddRange(list);

            _updatingGrid = true;
            try
            {
                _grid.DataSource = null;
                _grid.DataSource = _reviews;
            }
            finally
            {
                _updatingGrid = false;
            }

            _countChip.UpdateStyle($"{_reviews.Count} yêu cầu", Theme.TextSecondary, Theme.BackgroundSubtle);

            var hasRows = _reviews.Count > 0;
            _emptyPanel.Visible = !hasRows;
            _grid.Visible = hasRows;
            _pagination.Visible = hasRows;
            if (!hasRows)
            {
                _emptyPanel.BringToFront();
            }

            _btnPrev.Enabled = _reviewHistory.Count > 0;
            _btnNext.Enabled = _reviews.Count >= 25;
            _lblPageInfo.Text = $"Trang {_currentPageIndex}  ·  {_reviews.Count} yêu cầu";

            if (hasRows)
            {
                _grid.ClearSelection();
                _grid.Rows[0].Selected = true;
                SelectReview(_reviews[0]);
            }
            else
            {
                SelectReview(null);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static (string Text, Color Fore, Color Back) ReviewStateStyle(string? state) => state?.Trim().ToUpperInvariant() switch
    {
        "PENDING" => ("Chờ duyệt", Theme.Warning, Theme.WarningLight),
        "APPROVED" => ("Đã duyệt", Theme.Success, Theme.SuccessLight),
        "REJECTED" => ("Từ chối", Theme.Danger, Theme.DangerLight),
        "REVOKED" => ("Thu hồi", Theme.Muted, Theme.BackgroundSubtle),
        _ => (state ?? "Không rõ", Theme.Muted, Theme.BackgroundSubtle)
    };

    private void SelectReview(LinkReviewDto? review)
    {
        if (InvokeRequired) { BeginInvoke(() => SelectReview(review)); return; }

        _selected = review;
        _lblActionFeedback.Visible = false;

        if (review is null)
        {
            _noSelectionCard.Visible = true;
            _detailCard.Visible = false;
            _customerIdBox.Clear();
            _reasonBox.Clear();
            return;
        }

        _noSelectionCard.Visible = false;
        _detailCard.Visible = true;

        _detailTitle.Text = $"Yêu cầu #{review.Id}";
        var (chipText, chipFore, chipBack) = ReviewStateStyle(review.State);
        _detailChip.UpdateStyle(chipText, chipFore, chipBack);

        _valCustomer.Text = string.IsNullOrWhiteSpace(review.FullName) ? "(Chưa cập nhật tên)" : review.FullName;
        _valEmail.Text = $"Email đăng nhập: {review.LoginEmail}";
        _valAccount.Text = $"Mã tài khoản: ACC-{review.AccountId}";
        _valReference.Text = string.IsNullOrWhiteSpace(review.Reference) ? "Không cung cấp tham chiếu" : review.Reference;
        _valEvidence.Text = string.IsNullOrWhiteSpace(review.Evidence) ? "Khách hàng không cung cấp thêm bằng chứng." : review.Evidence;

        if (review.CustomerId.HasValue)
        {
            _valStudioCustomer.Text = $"✓ Đã gán KhachHangId: {review.CustomerId.Value}";
            _valStudioCustomer.ForeColor = Theme.Success;
            _customerIdBox.Text = review.CustomerId.Value.ToString();
        }
        else
        {
            _valStudioCustomer.Text = "Chưa gán KhachHangId studio";
            _valStudioCustomer.ForeColor = Theme.Muted;
            _customerIdBox.Clear();
        }

        _reasonBox.Clear();

        // Button state matrix based on Backend transition rules:
        // PENDING -> Approve or Reject
        // APPROVED -> Revoke
        // REJECTED / REVOKED -> Final states (no further transitions)
        var isPending = string.Equals(review.State, "PENDING", StringComparison.OrdinalIgnoreCase);
        var isApproved = string.Equals(review.State, "APPROVED", StringComparison.OrdinalIgnoreCase);

        _btnApprove.Enabled = isPending;
        _btnReject.Enabled = isPending;
        _btnRevoke.Enabled = isApproved;
    }

    private void UpdateSelectionView()
    {
        SelectReview(_selected);
    }

    private async Task ApproveAsync()
    {
        if (_selected is null) return;
        if (!string.Equals(_selected.State, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            SetFeedback("Chỉ có thể duyệt yêu cầu đang ở trạng thái 'Chờ duyệt'.", true);
            return;
        }

        var custIdStr = _customerIdBox.Text.Trim();
        if (!long.TryParse(custIdStr, out var custId) || custId <= 0)
        {
            SetFeedback("Vui lòng nhập Mã Khách Hàng studio (KhachHangId) hợp lệ (số nguyên dương) trước khi duyệt.", true);
            _customerIdBox.Focus();
            return;
        }

        var reason = _reasonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetFeedback("Vui lòng nhập lý do / bằng chứng đã đối chiếu trước khi duyệt liên kết.", true);
            _reasonBox.Focus();
            return;
        }

        var confirmMsg = $"Xác nhận DUYỆT liên kết tài khoản cho khách hàng:\n\n" +
                         $"• Họ tên: {_selected.FullName}\n" +
                         $"• Email: {_selected.LoginEmail}\n" +
                         $"• Gán KhachHangId: {custId}\n" +
                         $"• Lý do đối chiếu: {reason}\n\n" +
                         $"Sau khi duyệt, khách hàng sẽ có quyền xem lịch sử studio tương ứng.";

        if (!AtelierUi.Confirm(this, "Xác nhận duyệt liên kết hồ sơ", confirmMsg, "Duyệt liên kết"))
            return;

        var response = await _send(new(IdentityCommand.ApproveLink)
        {
            LinkId = _selected.Id,
            CustomerId = custId,
            Reason = reason
        });

        _show(response);
        if (response.Success)
        {
            SetFeedback("Duyệt liên kết thành công.", false);
            await LoadReviewsAsync();
        }
        else
        {
            SetFeedback(response.Message, true);
        }
    }

    private async Task RejectAsync()
    {
        if (_selected is null) return;
        if (!string.Equals(_selected.State, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            SetFeedback("Chỉ có thể từ chối yêu cầu đang ở trạng thái 'Chờ duyệt'.", true);
            return;
        }

        var reason = _reasonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetFeedback("Vui lòng nhập lý do từ chối yêu cầu liên kết.", true);
            _reasonBox.Focus();
            return;
        }

        var confirmMsg = $"Xác nhận TỪ CHỐI yêu cầu liên kết #{_selected.Id} của khách hàng '{_selected.FullName}' ({_selected.LoginEmail})?\n\n" +
                         $"Lý do từ chối: {reason}";

        if (!AtelierUi.Confirm(this, "Xác nhận từ chối liên kết", confirmMsg, "Từ chối yêu cầu"))
            return;

        var response = await _send(new(IdentityCommand.RejectLink)
        {
            LinkId = _selected.Id,
            Reason = reason
        });

        _show(response);
        if (response.Success)
        {
            SetFeedback("Đã từ chối yêu cầu liên kết.", false);
            await LoadReviewsAsync();
        }
        else
        {
            SetFeedback(response.Message, true);
        }
    }

    private async Task RevokeAsync()
    {
        if (_selected is null) return;
        if (!string.Equals(_selected.State, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            SetFeedback("Chỉ có thể thu hồi yêu cầu đã được duyệt trước đó.", true);
            return;
        }

        var reason = _reasonBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetFeedback("Vui lòng nhập lý do thu hồi liên kết hồ sơ.", true);
            _reasonBox.Focus();
            return;
        }

        var confirmMsg = $"Xác nhận THU HỒI liên kết hồ sơ của yêu cầu #{_selected.Id} ({_selected.FullName})?\n\n" +
                         $"Sau khi thu hồi, khách hàng sẽ không còn quyền truy cập dữ liệu lịch sử studio.\n" +
                         $"Lý do thu hồi: {reason}";

        if (!AtelierUi.Confirm(this, "Xác nhận thu hồi liên kết", confirmMsg, "Thu hồi liên kết"))
            return;

        var response = await _send(new(IdentityCommand.RevokeLink)
        {
            LinkId = _selected.Id,
            Reason = reason
        });

        _show(response);
        if (response.Success)
        {
            SetFeedback("Đã thu hồi liên kết hồ sơ thành công.", false);
            await LoadReviewsAsync();
        }
        else
        {
            SetFeedback(response.Message, true);
        }
    }

    private async Task NextPageAsync()
    {
        if (_reviews.Count == 0) return;
        var last = _reviews[_reviews.Count - 1];
        _reviewHistory.Push(_beforeLinkId);
        _beforeLinkId = last.Id;
        _currentPageIndex++;
        await LoadReviewsAsync();
    }

    private async Task PrevPageAsync()
    {
        if (_reviewHistory.TryPop(out var cursor))
        {
            _beforeLinkId = cursor;
            _currentPageIndex = Math.Max(1, _currentPageIndex - 1);
            await LoadReviewsAsync();
        }
    }

    private void SetFeedback(string message, bool isError)
    {
        _lblActionFeedback.Text = message;
        _lblActionFeedback.ForeColor = isError ? Theme.Danger : Theme.Success;
        _lblActionFeedback.Visible = !string.IsNullOrWhiteSpace(message);
    }
}
