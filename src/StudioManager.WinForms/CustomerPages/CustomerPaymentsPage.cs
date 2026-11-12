using StudioManager.WinForms.Contracts;
using StudioManager.WinForms.Controls;
using StudioManager.WinForms.Simulation;

namespace StudioManager.WinForms.CustomerPages;

public sealed class CustomerPaymentsPage : UserControl
{
    private readonly CustomerSessionContext _session;
    private readonly ICustomerNavigator _nav;

    private readonly ComboBox _targetRefBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340, Font = Theme.Font(9.0f) };
    private readonly Label _targetSummaryBanner = new() { AutoSize = true, MaximumSize = new Size(460, 0), Font = Theme.Font(8.2f), ForeColor = Theme.TextSecondary };
    private readonly NumericUpDown _amountBox = new() { Minimum = 100_000, Maximum = 50_000_000, Increment = 100_000, Value = 1_000_000, ThousandsSeparator = true, Width = 200, Font = Theme.Font(9.6f) };
    private readonly CheckBox _transferConfirmChk = new()
    {
        AutoSize = false,
        Width = 440,
        Height = 40,
        Text = "Tôi xác nhận đã thực hiện chuyển khoản VietQR với đúng cú pháp bên trên (Demo)",
        Font = Theme.Font(8.7f, FontStyle.Bold),
        ForeColor = Theme.TextSecondary
    };
    private readonly TextBox _evidenceNoteBox = new() { Width = 440, Height = 54, Multiline = true, Font = Theme.Font(8.9f), PlaceholderText = "Ghi chú giờ chuyển khoản hoặc mã giao dịch ngân hàng..." };
    private readonly Label _evidenceFileLabel = new() { AutoSize = true, Text = "Chưa đính kèm ảnh chứng từ (tùy chọn)", Font = Theme.Font(8.4f), ForeColor = Theme.Muted };
    private readonly AtelierInlineAlert _leftAlert = new() { Dock = DockStyle.Top, Height = 56, Margin = new Padding(0, 0, 0, 6) };
    private readonly ModernButton _sendBtn = Theme.Button("Gửi xác nhận chờ nhân viên đối soát (Demo)", Theme.Primary, IconKind.Check);
    private readonly ModernButton _goWizardBtn = Theme.OutlineButton("Tạo yêu cầu đặt lịch ngay", Theme.Primary, IconKind.Plus);
    private string? _evidenceFilePath;
    private bool _hasEligibleTarget;
    private bool _suppressAmountSync;

    private VietQrCardControl _qrCard = null!;
    private readonly DataGridView _paymentGrid = Theme.Grid();
    private readonly Panel _detailCardHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 6, 0, 0) };
    private readonly ModernButton _viewReceiptBtn = Theme.Button("Xem trước biên nhận (A4 / K80 / K58 / PDF)", Theme.Primary, IconKind.Printer);
    private List<PaymentSubmissionDto> _payments = [];

    public CustomerPaymentsPage(CustomerSessionContext session, ICustomerNavigator nav, string? targetRefCode = null)
    {
        _session = session;
        _nav = nav;

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(20, 14, 20, 16);

        var header = new PageHeader(
            "Thanh toán VietQR & Biên nhận",
            "Hướng dẫn chuyển khoản thủ công • Quét QR minh họa KHÔNG tự động chốt lịch • Nhân viên đối soát theo chứng từ");

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

        // Left: Target selector + Non-scannable VietQR Demo Instruction + Evidence Declaration Form
        var leftCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14) };
        var leftStack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        leftStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        leftStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 276));
        leftStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _goWizardBtn.Width = 198;
        _goWizardBtn.Height = 32;
        _goWizardBtn.Visible = false;
        _goWizardBtn.Click += (_, _) => _nav.NavigateTo("wizard");

        var alertWrap = new Panel { Dock = DockStyle.Top, AutoSize = true };
        alertWrap.Controls.Add(_leftAlert);
        leftStack.Controls.Add(alertWrap, 0, 0);

        var selectorRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 2, 0, 0) };
        selectorRow.Controls.Add(new Label { AutoSize = true, Text = "Mã tham chiếu:", Font = Theme.Font(9.0f, FontStyle.Bold), ForeColor = Theme.TextSecondary, Padding = new Padding(0, 6, 8, 0) });
        PopulateTargets(targetRefCode);
        selectorRow.Controls.Add(_targetRefBox);
        selectorRow.Controls.Add(_goWizardBtn);

        var initRef = CurrentTargetCode() ?? "CHUA_CO_HO_SO";
        _qrCard = new VietQrCardControl(DesktopDemoSessionStore.BuildVietQrInstruction(initRef, _amountBox.Value))
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 8)
        };

        var submitForm = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(2, 6, 2, 2) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.Add(_targetSummaryBanner);
        flow.Controls.Add(new Label { AutoSize = true, Text = "Khai báo số tiền đã chuyển (VNĐ):", Font = Theme.Font(8.4f, FontStyle.Bold), ForeColor = Theme.TextSecondary, Margin = new Padding(0, 6, 0, 2) });
        flow.Controls.Add(_amountBox);
        flow.Controls.Add(_transferConfirmChk);

        var attachRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 6, 0, 6) };
        var pickFileBtn = Theme.OutlineButton("Chọn ảnh chứng từ (Demo)...");
        pickFileBtn.Width = 216;
        pickFileBtn.Click += (_, _) => OnPickEvidence();
        attachRow.Controls.Add(pickFileBtn);
        attachRow.Controls.Add(_evidenceFileLabel);
        flow.Controls.Add(attachRow);
        flow.Controls.Add(_evidenceNoteBox);

        _sendBtn.Width = 318;
        _sendBtn.Margin = new Padding(0, 10, 0, 0);
        _sendBtn.Click += (_, _) => OnSubmitPayment();
        flow.Controls.Add(_sendBtn);
        submitForm.Controls.Add(flow);

        leftStack.Controls.Add(selectorRow, 0, 1);
        leftStack.Controls.Add(_qrCard, 0, 2);
        leftStack.Controls.Add(submitForm, 0, 3);
        leftCard.Controls.Add(leftStack);

        // Right: Structured Transaction Grid + Structured Inspector + Multi-Format Receipt Viewer
        var rightCard = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(14) };
        var rStack = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        rStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        rStack.RowStyles.Add(new RowStyle(SizeType.Percent, 44f));
        rStack.RowStyles.Add(new RowStyle(SizeType.Percent, 56f));
        rStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var histHdr = new Label { Dock = DockStyle.Fill, Text = "LỊCH SỬ GIAO DỊCH & ĐỐI SOÁT CỦA BẠN", UseMnemonic = false, Font = Theme.MonoFont(8.0f, FontStyle.Bold), ForeColor = Theme.Muted };

        _paymentGrid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _paymentGrid.DefaultCellStyle.Font = Theme.Font(8.4f);
        _paymentGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        _paymentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã GD", FillWeight = 34f });
        _paymentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số tiền", FillWeight = 25f });
        _paymentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", FillWeight = 41f });
        _paymentGrid.SelectionChanged += (_, _) => RenderPaymentDetail();

        var btnBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        _viewReceiptBtn.Width = 310;
        _viewReceiptBtn.Click += (_, _) => OpenSelectedReceipt();
        btnBar.Controls.Add(_viewReceiptBtn);

        rStack.Controls.Add(histHdr, 0, 0);
        rStack.Controls.Add(_paymentGrid, 0, 1);
        rStack.Controls.Add(_detailCardHost, 0, 2);
        rStack.Controls.Add(btnBar, 0, 3);
        rightCard.Controls.Add(rStack);

        split.Controls.Add(leftCard, 0, 0);
        split.Controls.Add(rightCard, 1, 0);
        Controls.Add(split);
        Controls.Add(header);

        _targetRefBox.SelectedIndexChanged += (_, _) => OnTargetSelectionChanged();
        _amountBox.ValueChanged += (_, _) => UpdateQrCard();

        OnTargetSelectionChanged();
        ReloadPayments();
    }

    private void PopulateTargets(string? prefer)
    {
        _targetRefBox.Items.Clear();
        var reqs = DesktopDemoSessionStore.GetCustomerBookingRequests(_session.Profile.DemoAccountId);
        var bookings = DesktopDemoSessionStore.GetCustomerBookings(_session.Profile);

        foreach (var r in reqs.Where(x => x.TrangThai is not (BookingRequestStatus.DA_TU_CHOI or BookingRequestStatus.KHACH_HUY)))
            _targetRefBox.Items.Add($"{r.MaYeuCau} — Yêu cầu {r.TenGoi}");
        foreach (var b in bookings)
            _targetRefBox.Items.Add($"{b.MaLichChup} — Lịch {b.TenGoi}");

        _hasEligibleTarget = _targetRefBox.Items.Count > 0;
        if (!_hasEligibleTarget)
        {
            _targetRefBox.Items.Add("Chưa có yêu cầu hoặc lịch chụp thuộc tài khoản này");
            _targetRefBox.Enabled = false;
            _sendBtn.Enabled = false;
            _goWizardBtn.Visible = true;
            _leftAlert.ShowMessage(
                AtelierNotificationKind.Info,
                "Chưa có đơn đặt lịch để thanh toán",
                "Hãy gửi một yêu cầu đặt lịch mới trước khi khai báo chuyển khoản VietQR.");
        }
        else
        {
            _targetRefBox.Enabled = true;
            _sendBtn.Enabled = true;
            _goWizardBtn.Visible = false;
            _leftAlert.Clear();
        }

        var idx = 0;
        if (_hasEligibleTarget && !string.IsNullOrWhiteSpace(prefer))
        {
            for (var i = 0; i < _targetRefBox.Items.Count; i++)
            {
                if (_targetRefBox.Items[i]?.ToString()?.StartsWith(prefer, StringComparison.OrdinalIgnoreCase) == true)
                {
                    idx = i;
                    break;
                }
            }
        }
        _targetRefBox.SelectedIndex = idx;
    }

    private string? CurrentTargetCode()
    {
        if (!_hasEligibleTarget) return null;
        var raw = _targetRefBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var space = raw.IndexOf(' ');
        return space > 0 ? raw[..space] : raw;
    }

    private void OnTargetSelectionChanged()
    {
        var code = CurrentTargetCode();
        if (code is null)
        {
            _targetSummaryBanner.Text = "Chưa chọn mã tham chiếu hợp lệ.";
            UpdateQrCard();
            return;
        }

        var snap = DesktopDemoSessionStore.TryResolvePaymentTarget(code);
        if (snap is not null)
        {
            _targetSummaryBanner.Text =
                $"Đơn {snap.ReferenceCode}: Gói {snap.PackageName} ({snap.TotalOrderAmount:N0} đ) • " +
                $"Đã đối soát {snap.AlreadyConfirmedPaid:N0} đ • Còn lại {snap.RemainingBeforeThisTx:N0} đ";

            var suggested = snap.RemainingBeforeThisTx > 0
                ? (snap.AlreadyConfirmedPaid == 0 ? Math.Round(snap.TotalOrderAmount * 0.5m / 50_000m) * 50_000m : snap.RemainingBeforeThisTx)
                : 500_000m;
            _suppressAmountSync = true;
            _amountBox.Value = Math.Clamp(suggested, _amountBox.Minimum, _amountBox.Maximum);
            _suppressAmountSync = false;
        }
        else
        {
            _targetSummaryBanner.Text = $"Mã tham chiếu {code}";
        }

        UpdateQrCard();
    }

    private void UpdateQrCard()
    {
        if (_suppressAmountSync) return;
        var target = CurrentTargetCode() ?? "CHUA_CO_HO_SO";
        _qrCard.Bind(DesktopDemoSessionStore.BuildVietQrInstruction(target, _amountBox.Value));
    }

    private void OnPickEvidence()
    {
        using var ofd = new OpenFileDialog { Filter = "Ảnh chứng từ (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
        if (ofd.ShowDialog(FindForm()) != DialogResult.OK) return;
        _evidenceFilePath = ofd.FileName;
        _evidenceFileLabel.Text = "Đã chọn: " + Path.GetFileName(ofd.FileName);
    }

    private void OnSubmitPayment()
    {
        var target = CurrentTargetCode();
        if (target is null)
        {
            AtelierNotificationService.ShowWarning(FindForm(), "Tài khoản chưa có yêu cầu hoặc lịch chụp hợp lệ để gắn thanh toán.", "Chưa có mã tham chiếu");
            return;
        }
        if (!_transferConfirmChk.Checked)
        {
            AtelierNotificationService.ShowWarning(
                FindForm(),
                "Vui lòng đánh dấu xác nhận bạn đã thực hiện chuyển khoản VietQR theo đúng cú pháp trước khi gửi khai báo.",
                "Thiếu xác nhận chuyển khoản");
            return;
        }

        var sub = new PaymentSubmissionDto
        {
            MaGiaoDichDemo = DesktopDemoSessionStore.NextTransactionCode(),
            DemoCustomerId = _session.Profile.DemoAccountId,
            MaThamChieu = target,
            TargetType = target.StartsWith("LC", StringComparison.OrdinalIgnoreCase) ? "LICH_CHUP" : "YEU_CAU",
            TenKhachHang = _session.Profile.FullName,
            SoDienThoai = _session.Profile.PhoneNumber,
            PhuongThuc = PaymentMethodKind.VietQrChuyenKhoan,
            SoTienKhaiBao = _amountBox.Value,
            SoTienXacNhan = 0m,
            NoiDungChuyenKhoan = $"PHOENIX {target}",
            TrangThai = PaymentReconciliationStatus.CHO_DOI_SOAT,
            CustomerConfirmedTransfer = true,
            EvidenceFileName = _evidenceFilePath is not null ? Path.GetFileName(_evidenceFilePath) : null,
            EvidenceFilePath = _evidenceFilePath,
            EvidenceNotes = string.IsNullOrWhiteSpace(_evidenceNoteBox.Text) ? "Khách gửi xác nhận qua Cổng Khách hàng (Demo)" : _evidenceNoteBox.Text.Trim(),
            CreatedAtLabel = DateTime.Now.ToString("HH:mm dd/MM")
        };

        DesktopDemoSessionStore.SubmitCustomerPaymentEvidence(sub);
        _nav.RefreshBadgeCounts();
        ReloadPayments(sub.MaGiaoDichDemo);
        AtelierNotificationService.ShowSuccess(
            FindForm(),
            $"Đã ghi nhận khai báo thanh toán {sub.MaGiaoDichDemo} ({sub.SoTienKhaiBao:N0} đ) cho mã {target}.\nTrạng thái hiện tại: Chờ nhân viên đối soát thủ công.",
            "Đã gửi khai báo thanh toán");
    }

    private PaymentSubmissionDto? SelectedPayment()
    {
        if (_paymentGrid.CurrentRow is null) return null;
        var idx = _paymentGrid.CurrentRow.Index;
        return idx >= 0 && idx < _payments.Count ? _payments[idx] : null;
    }

    private void ReloadPayments(string? focusTxn = null)
    {
        _payments = DesktopDemoSessionStore.GetCustomerPayments(_session.Profile.DemoAccountId).ToList();
        _paymentGrid.Rows.Clear();
        foreach (var p in _payments)
        {
            _paymentGrid.Rows.Add(
                p.MaGiaoDichDemo,
                $"{p.SoTienKhaiBao:N0} đ",
                DesktopDemoSessionStore.GetPaymentStatusLabel(p.TrangThai));
        }
        if (_payments.Count > 0)
        {
            var idx = 0;
            if (!string.IsNullOrWhiteSpace(focusTxn))
            {
                var found = _payments.FindIndex(x => x.MaGiaoDichDemo == focusTxn);
                if (found >= 0) idx = found;
            }
            _paymentGrid.Rows[idx].Selected = true;
            _paymentGrid.CurrentCell = _paymentGrid.Rows[idx].Cells[0];
        }
        RenderPaymentDetail();
    }

    private void RenderPaymentDetail()
    {
        _detailCardHost.Controls.Clear();
        var p = SelectedPayment();
        var w = Math.Max(300, _detailCardHost.ClientSize.Width - 16);

        if (p is null)
        {
            var empty = new CardPanel { Size = new Size(w, 120), BackColor = Theme.Primitives.Alabaster50, Padding = new Padding(14) };
            empty.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Tài khoản này chưa có giao dịch thanh toán nào.\nNếu đã có yêu cầu/lịch chụp, bạn có thể tạo khai báo chuyển khoản minh họa ở cột bên trái.",
                Font = Theme.Font(8.9f),
                ForeColor = Theme.Muted
            });
            _detailCardHost.Controls.Add(empty);
            _viewReceiptBtn.Enabled = _hasEligibleTarget;
            return;
        }

        _viewReceiptBtn.Enabled = true;
        var statusColor = p.TrangThai switch
        {
            PaymentReconciliationStatus.DA_KHOP_THU_CONG or PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT => Theme.Success,
            PaymentReconciliationStatus.SAI_LECH_THONG_TIN or PaymentReconciliationStatus.TU_CHOI => Theme.Danger,
            _ => Theme.Warning
        };

        var snap = DesktopDemoSessionStore.TryResolvePaymentTarget(p.MaThamChieu);
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Width = w, FlowDirection = FlowDirection.TopDown, WrapContents = false };

        var card = new CardPanel
        {
            Size = new Size(w, 168),
            BackColor = Theme.Primitives.Alabaster50,
            BorderColor = Theme.BorderStrong,
            AccentStripeLeft = true,
            AccentStripeColor = statusColor,
            AccentStripeThickness = 4,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 8)
        };
        card.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = Theme.Font(8.7f),
            ForeColor = Theme.TextSecondary,
            Text =
                $"Tham chiếu:         {p.MaThamChieu} {(snap is not null ? $"({snap.PackageName} • {snap.TotalOrderAmount:N0} đ)" : "")}\n" +
                $"Nội dung CK:        {p.NoiDungChuyenKhoan}\n" +
                $"Số tiền khai báo:    {p.SoTienKhaiBao:N0} đ\n" +
                $"Xác nhận thực tế:   {p.SoTienXacNhan:N0} đ\n" +
                $"Chứng từ đính kèm: {p.EvidenceFileName ?? "Không có file ảnh"}\n" +
                $"Ghi chú khách:       {p.EvidenceNotes}"
        });
        card.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = $"{p.MaGiaoDichDemo} • {DesktopDemoSessionStore.GetPaymentStatusLabel(p.TrangThai)}",
            Font = Theme.Font(9.6f, FontStyle.Bold),
            ForeColor = statusColor
        });

        var reviewerCard = new CardPanel
        {
            Size = new Size(w, 68),
            BackColor = p.TrangThai == PaymentReconciliationStatus.SAI_LECH_THONG_TIN ? Theme.DangerLight : Theme.Surface,
            BorderColor = p.TrangThai == PaymentReconciliationStatus.SAI_LECH_THONG_TIN ? Theme.DangerBorder : Theme.Border,
            Padding = new Padding(12, 8, 12, 8)
        };
        reviewerCard.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = string.IsNullOrWhiteSpace(p.StaffReviewerNote) ? "Đang chờ nhân viên kế toán đối soát sao kê thủ công." : p.StaffReviewerNote,
            Font = Theme.Font(8.4f),
            ForeColor = Theme.TextSecondary
        });
        reviewerCard.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "PHẢN HỒI ĐỐI SOÁT TỪ STUDIO",
            Font = Theme.MonoFont(7.4f, FontStyle.Bold),
            ForeColor = Theme.Muted
        });

        stack.Controls.Add(card);
        stack.Controls.Add(reviewerCard);
        _detailCardHost.Controls.Add(stack);
    }

    private void OpenSelectedReceipt()
    {
        var p = SelectedPayment();
        ReceiptDocumentDto? doc = null;

        if (p is not null)
        {
            doc = DesktopDemoSessionStore.BuildReceiptForPayment(p.MaGiaoDichDemo, "Hệ thống Xem trước (Demo)");
        }
        else if (CurrentTargetCode() is string code)
        {
            var snap = DesktopDemoSessionStore.TryResolvePaymentTarget(code);
            if (snap is not null)
            {
                doc = DesktopDemoSessionStore.BuildReceiptPreview(
                    ReceiptFormatKind.A4Invoice,
                    snap.ReferenceCode,
                    snap.CustomerName,
                    snap.CustomerPhone,
                    snap.PackageName,
                    snap.RoomName,
                    snap.Services,
                    snap.PackagePrice,
                    snap.AlreadyConfirmedPaid,
                    "Dự kiến / Chưa ghi nhận thanh toán",
                    "Hệ thống Xem trước (Demo)");
            }
        }

        if (doc is null)
        {
            AtelierNotificationService.ShowInfo(FindForm(), "Vui lòng chọn một giao dịch hoặc đơn đặt lịch hợp lệ để xem trước biên nhận.", "Chưa có dữ liệu biên nhận");
            return;
        }

        using var dlg = new ExtendedReceiptPreviewForm(doc);
        dlg.ShowDialog(FindForm());
    }
}
