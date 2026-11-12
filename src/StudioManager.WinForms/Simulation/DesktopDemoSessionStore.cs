using StudioManager.WinForms.Contracts;

namespace StudioManager.WinForms.Simulation;

/// <summary>
/// Shared in-memory Demo session store bridging Customer Desktop UX and Staff/Admin
/// Frontend Workspaces (Yêu cầu đặt lịch & Đối soát thanh toán) during a desktop run.
/// Explicitly non-persistent: zero writes to SQL Server.
/// </summary>
public static class DesktopDemoSessionStore
{
    private static readonly object SyncLock = new();
    private static bool _seeded;

    private static readonly List<CustomerDemoProfileDto> DemoProfiles = [];
    private static readonly List<CustomerBookingRequestDto> BookingRequests = [];
    private static readonly List<CustomerMockBookingDto> MockBookings = [];
    private static readonly List<PaymentSubmissionDto> PaymentSubmissions = [];
    private static readonly List<CustomerPhotoAlbumDto> PhotoAlbums = [];
    private static readonly List<StudioNotificationDto> Notifications = [];

    public static CustomerSessionContext? CurrentCustomerSession { get; private set; }

    public static void EnsureSeeded()
    {
        lock (SyncLock)
        {
            if (_seeded) return;
            SeedBaselineDataLocked();
            _seeded = true;
        }
    }

    internal static void ResetForTests()
    {
        lock (SyncLock)
        {
            DemoProfiles.Clear();
            BookingRequests.Clear();
            MockBookings.Clear();
            PaymentSubmissions.Clear();
            PhotoAlbums.Clear();
            Notifications.Clear();
            CurrentCustomerSession = null;
            SeedBaselineDataLocked();
            _seeded = true;
        }
    }

    public static CustomerSessionContext SignInDemoCustomer(string identifier)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var trimmed = identifier.Trim();
            var digits = trimmed.Replace(" ", "");
            var matched = DemoProfiles.FirstOrDefault(p =>
                p.Email.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                p.PhoneNumber.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                p.PhoneNumber.Replace(" ", "").Equals(digits, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                matched = DemoProfiles[0];
            }

            CurrentCustomerSession = new CustomerSessionContext
            {
                SessionId = $"DEMO-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Profile = matched
            };
            return CurrentCustomerSession;
        }
    }

    public static CustomerSessionContext RegisterDemoCustomer(CustomerRegistrationRequestDto dto)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var linkState = string.IsNullOrWhiteSpace(dto.ExistingBookingReference)
                ? CustomerIdentityLinkState.Unlinked
                : CustomerIdentityLinkState.PendingVerification;

            var profile = new CustomerDemoProfileDto(
                DemoAccountId: $"KH_DEMO_{DemoProfiles.Count + 1:000}",
                LinkedCustomerCode: string.IsNullOrWhiteSpace(dto.ExistingBookingReference) ? null : "KH000001",
                FullName: dto.FullName.Trim(),
                PhoneNumber: dto.PhoneNumber.Trim(),
                Email: dto.Email.Trim(),
                PreferredContactChannel: "Điện thoại & Email",
                Address: "TP. Hồ Chí Minh (hồ sơ Demo)",
                Note: string.IsNullOrWhiteSpace(dto.ExistingBookingReference)
                    ? "Tài khoản khách hàng mới đăng ký trong chế độ mô phỏng Desktop."
                    : $"Đã gửi yêu cầu liên kết tham chiếu mã {dto.ExistingBookingReference.Trim()}.",
                LinkState: linkState,
                LinkStatusHeadline: GetLinkStateHeadline(linkState),
                LinkStatusDescription: GetLinkStateDescription(linkState),
                LastUpdatedLabel: DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                IsSamplePersona: false);

            DemoProfiles.Add(profile);
            CurrentCustomerSession = new CustomerSessionContext
            {
                SessionId = $"DEMO-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Profile = profile
            };

            Notifications.Insert(0, new StudioNotificationDto(
                $"NTF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                "Tài khoản khách hàng (Demo) đã được khởi tạo",
                "Bạn đang sử dụng giao diện mô phỏng khách hàng. Yêu cầu đặt lịch mới sẽ hiển thị đồng bộ trên màn hình nhân viên trong cùng phiên chạy.",
                "Hệ thống Demo",
                DateTime.Now.ToString("HH:mm dd/MM"),
                false,
                "overview",
                profile.DemoAccountId));

            return CurrentCustomerSession;
        }
    }

    public static void SignOutCustomer() => CurrentCustomerSession = null;

    public static void UpdateCurrentProfile(CustomerDemoProfileDto updated)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var idx = DemoProfiles.FindIndex(p => p.DemoAccountId == updated.DemoAccountId);
            if (idx >= 0) DemoProfiles[idx] = updated;
            if (CurrentCustomerSession?.Profile.DemoAccountId == updated.DemoAccountId)
                CurrentCustomerSession.Profile = updated;
        }
    }

    public static IdentityLinkEvaluationDto PreviewIdentityLinkState(CustomerIdentityLinkState state, string? referenceCode)
    {
        var refLabel = string.IsNullOrWhiteSpace(referenceCode) ? "LC000001 / KH000001" : referenceCode.Trim();
        return state switch
        {
            CustomerIdentityLinkState.Unlinked => new(
                state, "Chưa liên kết hồ sơ",
                "Tài khoản chưa gắn với mã khách hàng studio",
                "Bạn vẫn xem được gói chụp, phòng chụp và gửi yêu cầu đặt lịch mới; lịch cũ cần được xác minh quyền sở hữu trước khi hiển thị.",
                "Gửi thông tin đối chiếu (SĐT + mã lịch chụp cũ) hoặc nhờ lễ tân tại quầy xác minh.",
                false, null),

            CustomerIdentityLinkState.PendingVerification => new(
                state, "Đang chờ xác minh quyền sở hữu",
                $"Đang chờ đối chiếu tham chiếu {refLabel}",
                "Yêu cầu liên kết đã được ghi nhận ở giao diện. Studio tuyệt đối không tự động gộp hồ sơ chỉ qua số điện thoại nếu chưa đối chiếu.",
                "Chờ nhân viên lễ tân xác nhận thông tin trong ca làm việc.",
                false, "KH000001"),

            CustomerIdentityLinkState.VerifiedOwnership => new(
                state, "Đã xác minh quyền sở hữu",
                "Liên kết hồ sơ khách hàng hợp lệ (KH000001)",
                "Khi backend Codex triển khai bước xác minh OTP/phiên chính thức, trạng thái này cho phép hiển thị lịch chụp, biên nhận và album thuộc chủ sở hữu.",
                "Có thể xem lịch sử buổi chụp và album đã giao trong phạm vi chủ sở hữu.",
                true, "KH000001"),

            CustomerIdentityLinkState.RejectedVerification => new(
                state, "Từ chối liên kết",
                "Thông tin đối chiếu chưa trùng khớp hồ sơ studio",
                "Tên khách hàng, số điện thoại hoặc mã tham chiếu không khớp với dữ liệu lịch chụp đang lưu. Quyền xem lịch cũ bị chặn an toàn.",
                "Kiểm tra lại mã lịch chụp hoặc liên hệ trực tiếp quầy tiếp nhận Atelier Lumière.",
                false, null),

            CustomerIdentityLinkState.AuthorizedManualLink => new(
                state, "Nhân viên hỗ trợ liên kết thủ công",
                "Được Quản trị/Tiếp nhận phê duyệt liên kết có kiểm soát",
                "Áp dụng khi khách đổi số điện thoại hoặc quên email cũ và đã xuất trình giấy tờ / đối chiếu trực tiếp với nhân viên studio.",
                "Đã mở quyền truy cập hồ sơ theo nhật ký đối chiếu của nhân sự.",
                true, "KH000001"),

            _ => new(state, "Không xác định", "", "", "", false, null)
        };
    }

    public static IReadOnlyList<CustomerBookingRequestDto> GetAllBookingRequests()
    {
        EnsureSeeded();
        lock (SyncLock) return BookingRequests.ToList();
    }

    public static IReadOnlyList<CustomerBookingRequestDto> GetCustomerBookingRequests(string demoCustomerId)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            return BookingRequests
                .Where(x => string.Equals(x.DemoCustomerId, demoCustomerId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.MaYeuCau)
                .ToList();
        }
    }

    public static CustomerBookingRequestDto SubmitCustomerBookingRequest(CustomerBookingRequestDto request)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            BookingRequests.Insert(0, request);
            Notifications.Insert(0, new StudioNotificationDto(
                $"NTF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                $"Đã tiếp nhận yêu cầu {request.MaYeuCau}",
                $"Gói {request.TenGoi} • Mong muốn {request.NgayChupMongMuon:dd/MM/yyyy} ({request.KhungGioMongMuon}). Nhân sự studio sẽ kiểm tra lịch thực tế.",
                "Yêu cầu đặt lịch",
                DateTime.Now.ToString("HH:mm dd/MM"),
                false,
                "requests",
                request.DemoCustomerId));
            return request;
        }
    }

    public static void UpdateRequestFromStaff(
        string maYeuCau,
        BookingRequestStatus newStatus,
        string actorName,
        string customerFeedback,
        string internalNote,
        string? photographerCode,
        string? photographerName,
        string? roomCode,
        string? roomName,
        string conflictSummary,
        bool hasConflictWarning)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var req = BookingRequests.FirstOrDefault(x => x.MaYeuCau == maYeuCau);
            if (req is null) return;

            req.TrangThai = newStatus;
            req.PhanHoiChoKhach = customerFeedback.Trim();
            req.GhiChuNoiBo = internalNote.Trim();
            req.SuggestedPhotographerCode = photographerCode;
            req.SuggestedPhotographerName = photographerName;
            req.AssignedRoomCode = roomCode;
            req.AssignedRoomName = roomName;
            req.ConflictCheckSummary = conflictSummary;
            req.HasConflictWarning = hasConflictWarning;

            if (newStatus == BookingRequestStatus.DA_XAC_NHAN)
            {
                if (string.IsNullOrWhiteSpace(req.LinkedBookingCode))
                {
                    var seq = MockBookings.Count + 10;
                    string candidate;
                    do
                    {
                        candidate = $"LCDEMO-{DateTime.Now:yyyyMM}-{seq:000}";
                        seq++;
                    } while (MockBookings.Any(b => string.Equals(b.MaLichChup, candidate, StringComparison.OrdinalIgnoreCase)));
                    req.LinkedBookingCode = candidate;
                }

                UpsertLinkedBookingLocked(req);
            }

            req.Timeline.Insert(0, new BookingRequestTimelineEventDto(
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                actorName,
                GetStatusVietnameseLabel(newStatus),
                string.IsNullOrWhiteSpace(customerFeedback) ? conflictSummary : customerFeedback.Trim(),
                newStatus));

            Notifications.Insert(0, new StudioNotificationDto(
                $"NTF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                $"Cập nhật yêu cầu {req.MaYeuCau}: {GetStatusVietnameseLabel(newStatus)}",
                string.IsNullOrWhiteSpace(req.LinkedBookingCode)
                    ? (string.IsNullOrWhiteSpace(req.PhanHoiChoKhach) ? conflictSummary : req.PhanHoiChoKhach)
                    : $"Đã tạo lịch chụp {req.LinkedBookingCode} tại {req.AssignedRoomName ?? req.TenPhongUuTien ?? "phòng được phân bổ"}.",
                "Điều phối lịch",
                DateTime.Now.ToString("HH:mm dd/MM"),
                false,
                newStatus == BookingRequestStatus.DA_XAC_NHAN ? "bookings" : "requests",
                req.DemoCustomerId));
        }
    }

    public static bool CancelRequestByCustomer(string maYeuCau, string reason)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var req = BookingRequests.FirstOrDefault(x => x.MaYeuCau == maYeuCau);
            if (req is null) return false;
            if (req.TrangThai != BookingRequestStatus.CHO_XU_LY && req.TrangThai != BookingRequestStatus.CAN_BO_SUNG)
                return false;

            req.TrangThai = BookingRequestStatus.KHACH_HUY;
            req.PhanHoiChoKhach = $"Khách hàng chủ động hủy: {reason}";
            req.Timeline.Insert(0, new BookingRequestTimelineEventDto(
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                req.TenKhachHang + " (Khách hàng)",
                "Hủy yêu cầu đặt lịch",
                reason,
                BookingRequestStatus.KHACH_HUY));
            return true;
        }
    }

    public static IReadOnlyList<CustomerMockBookingDto> GetCustomerBookings(CustomerDemoProfileDto profile)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var owned = MockBookings
                .Where(x => string.Equals(x.DemoCustomerId, profile.DemoAccountId, StringComparison.OrdinalIgnoreCase));

            if (profile.LinkState is CustomerIdentityLinkState.VerifiedOwnership or CustomerIdentityLinkState.AuthorizedManualLink)
            {
                return owned.OrderByDescending(x => x.NgayChup).ToList();
            }

            // Strict ownership boundary: unlinked / pending / rejected accounts cannot view
            // pre-existing legacy studio records (MaYeuCauGoc == null); only bookings newly
            // confirmed from requests belonging to this account itself are shown.
            return owned
                .Where(x => !string.IsNullOrWhiteSpace(x.MaYeuCauGoc))
                .OrderByDescending(x => x.NgayChup)
                .ToList();
        }
    }

    public static IReadOnlyList<PaymentSubmissionDto> GetAllPaymentSubmissions()
    {
        EnsureSeeded();
        lock (SyncLock) return PaymentSubmissions.ToList();
    }

    public static IReadOnlyList<PaymentSubmissionDto> GetCustomerPayments(string demoCustomerId)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            return PaymentSubmissions
                .Where(x => string.Equals(x.DemoCustomerId, demoCustomerId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public static PaymentSubmissionDto SubmitCustomerPaymentEvidence(PaymentSubmissionDto submission)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            PaymentSubmissions.Insert(0, submission);
            Notifications.Insert(0, new StudioNotificationDto(
                $"NTF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                $"Đã ghi nhận khai báo thanh toán {submission.MaGiaoDichDemo}",
                $"Số tiền {submission.SoTienKhaiBao:N0} đ cho {submission.MaThamChieu} đang ở trạng thái Chờ nhân viên đối soát thủ công.",
                "Thanh toán VietQR",
                DateTime.Now.ToString("HH:mm dd/MM"),
                false,
                "payments",
                submission.DemoCustomerId));
            return submission;
        }
    }

    public static void UpdatePaymentReconciliationFromStaff(
        string maGiaoDich,
        PaymentReconciliationStatus status,
        decimal confirmedAmount,
        string reviewerNote)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var tx = PaymentSubmissions.FirstOrDefault(x => x.MaGiaoDichDemo == maGiaoDich);
            if (tx is null) return;
            tx.TrangThai = status;
            tx.SoTienXacNhan = confirmedAmount;
            tx.StaffReviewerNote = reviewerNote.Trim();
            tx.ReconciledAtLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            SyncLinkedBookingPaymentTotalsLocked(tx.MaThamChieu);
        }
    }

    public static CustomerDemoProfileDto SubmitCustomerIdentityVerificationRequest(
        string demoAccountId,
        CustomerRecoveryRequestDto request)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var idx = DemoProfiles.FindIndex(p => string.Equals(p.DemoAccountId, demoAccountId, StringComparison.OrdinalIgnoreCase));
            var baseProfile = idx >= 0 ? DemoProfiles[idx] : CurrentCustomerSession?.Profile ?? DemoProfiles[0];

            var refText = string.IsNullOrWhiteSpace(request.ReferenceBookingCode)
                ? "chưa cung cấp mã lịch cũ"
                : request.ReferenceBookingCode.Trim();
            var updated = baseProfile with
            {
                LinkState = CustomerIdentityLinkState.PendingVerification,
                LinkStatusHeadline = GetLinkStateHeadline(CustomerIdentityLinkState.PendingVerification),
                LinkStatusDescription =
                    $"Đã tiếp nhận yêu cầu đối chiếu cho liên hệ '{request.RegisteredEmailOrPhone.Trim()}' (tham chiếu: {refText}). " +
                    "Nhân sự Lễ tân/Quản trị sẽ kiểm tra khớp tên, SĐT và chứng từ trước khi mở lịch sử cũ.",
                Note = string.IsNullOrWhiteSpace(request.VerificationNote)
                    ? $"Yêu cầu đối chiếu hồ sơ ({refText})."
                    : request.VerificationNote.Trim(),
                LastUpdatedLabel = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            if (idx >= 0) DemoProfiles[idx] = updated;
            if (CurrentCustomerSession?.Profile.DemoAccountId == updated.DemoAccountId)
            {
                CurrentCustomerSession.Profile = updated;
            }

            Notifications.Insert(0, new StudioNotificationDto(
                $"NTF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                "Đã gửi yêu cầu xác minh hồ sơ cũ",
                $"Tham chiếu {refText} đang ở trạng thái Chờ nhân sự đối chiếu. Hệ thống không tự động gộp lịch sử.",
                "Bảo mật hồ sơ",
                DateTime.Now.ToString("HH:mm dd/MM"),
                false,
                "profile",
                updated.DemoAccountId));

            return updated;
        }
    }

    public static IReadOnlyList<CustomerPhotoAlbumDto> GetCustomerAlbums(CustomerDemoProfileDto profile)
    {
        EnsureSeeded();
        if (profile.LinkState is not (CustomerIdentityLinkState.VerifiedOwnership or CustomerIdentityLinkState.AuthorizedManualLink))
            return [];

        lock (SyncLock)
        {
            return PhotoAlbums
                .Where(x => string.Equals(x.DemoCustomerId, profile.DemoAccountId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public static PaymentTargetSnapshot? TryResolvePaymentTarget(string referenceCode)
    {
        EnsureSeeded();
        if (string.IsNullOrWhiteSpace(referenceCode)) return null;

        lock (SyncLock)
        {
            return TryResolvePaymentTargetLocked(referenceCode.Trim());
        }
    }

    public static ReceiptDocumentDto? BuildReceiptForPayment(
        string maGiaoDichDemo,
        string staffIssuerName,
        ReceiptFormatKind format = ReceiptFormatKind.A4Invoice)
    {
        EnsureSeeded();
        lock (SyncLock)
        {
            var tx = PaymentSubmissions.FirstOrDefault(x => string.Equals(x.MaGiaoDichDemo, maGiaoDichDemo, StringComparison.OrdinalIgnoreCase));
            if (tx is null) return null;

            var target = TryResolvePaymentTargetLocked(tx.MaThamChieu);
            var isConfirmed = tx.TrangThai is PaymentReconciliationStatus.DA_KHOP_THU_CONG or PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT;
            var paidForReceipt = isConfirmed
                ? Math.Max(tx.SoTienXacNhan, 0m)
                : tx.SoTienXacNhan;

            var methodText = tx.PhuongThuc switch
            {
                PaymentMethodKind.TienMatTaiQuay => "Tiền mặt tại quầy",
                PaymentMethodKind.VietQrChuyenKhoan => "VietQR chuyển khoản",
                _ => "Chuyển khoản thủ công"
            };
            var statusTag = isConfirmed
                ? $"{methodText} • {GetPaymentStatusLabel(tx.TrangThai)} ({tx.SoTienXacNhan:N0} đ)"
                : $"{methodText} • {GetPaymentStatusLabel(tx.TrangThai)} (Khai báo {tx.SoTienKhaiBao:N0} đ / Đã khớp {tx.SoTienXacNhan:N0} đ)";

            if (target is null)
            {
                return BuildReceiptPreview(
                    format,
                    tx.MaThamChieu,
                    tx.TenKhachHang,
                    tx.SoDienThoai,
                    $"Thanh toán tham chiếu {tx.MaThamChieu}",
                    "Theo hồ sơ đối chiếu",
                    [],
                    Math.Max(tx.SoTienKhaiBao, tx.SoTienXacNhan),
                    paidForReceipt,
                    statusTag,
                    staffIssuerName);
            }

            var effectivePaid = Math.Max(target.AlreadyConfirmedPaid, paidForReceipt);
            return BuildReceiptPreview(
                format,
                target.ReferenceCode,
                target.CustomerName,
                target.CustomerPhone,
                target.PackageName,
                target.RoomName,
                target.Services,
                target.PackagePrice,
                effectivePaid,
                statusTag,
                staffIssuerName);
        }
    }

    private static PaymentTargetSnapshot? TryResolvePaymentTargetLocked(string referenceCode)
    {
        var booking = MockBookings.FirstOrDefault(b =>
            string.Equals(b.MaLichChup, referenceCode, StringComparison.OrdinalIgnoreCase));
        if (booking is not null)
        {
            var reqForPhone = BookingRequests.FirstOrDefault(r =>
                string.Equals(r.MaYeuCau, booking.MaYeuCauGoc, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.DemoCustomerId, booking.DemoCustomerId, StringComparison.OrdinalIgnoreCase));
            var profile = DemoProfiles.FirstOrDefault(p =>
                string.Equals(p.DemoAccountId, booking.DemoCustomerId, StringComparison.OrdinalIgnoreCase));
            var phone = reqForPhone?.SoDienThoai ?? profile?.PhoneNumber ?? "Chưa cập nhật";
            var svcTotal = booking.SelectedServices.Sum(s => s.ThanhTien);
            var pkgPrice = Math.Max(0m, booking.TongGiaTri - svcTotal);

            return new PaymentTargetSnapshot(
                booking.MaLichChup,
                "LICH_CHUP",
                booking.TenKhachHang,
                phone,
                booking.TenGoi,
                booking.TenPhong,
                booking.SelectedServices.ToList(),
                pkgPrice,
                booking.TongGiaTri,
                booking.DaThanhToan,
                Math.Max(0m, booking.TongGiaTri - booking.DaThanhToan));
        }

        var req = BookingRequests.FirstOrDefault(r =>
            string.Equals(r.MaYeuCau, referenceCode, StringComparison.OrdinalIgnoreCase));
        if (req is not null)
        {
            var confirmed = SumConfirmedPaymentsLocked(req.MaYeuCau, req.LinkedBookingCode);
            var room = string.IsNullOrWhiteSpace(req.AssignedRoomName)
                ? (string.IsNullOrWhiteSpace(req.TenPhongUuTien) ? "Sẽ phân bổ khi xác nhận" : req.TenPhongUuTien)
                : req.AssignedRoomName;
            return new PaymentTargetSnapshot(
                req.MaYeuCau,
                "YEU_CAU",
                req.TenKhachHang,
                req.SoDienThoai,
                req.TenGoi,
                room,
                req.ExtraServices.ToList(),
                req.TamTinhGoi,
                req.TongTamTinh,
                confirmed,
                Math.Max(0m, req.TongTamTinh - confirmed));
        }

        return null;
    }

    private static decimal SumConfirmedPaymentsLocked(string? requestCode, string? bookingCode)
    {
        return PaymentSubmissions
            .Where(t => t.TrangThai is PaymentReconciliationStatus.DA_KHOP_THU_CONG or PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT)
            .Where(t =>
                (!string.IsNullOrWhiteSpace(requestCode) && string.Equals(t.MaThamChieu, requestCode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(bookingCode) && string.Equals(t.MaThamChieu, bookingCode, StringComparison.OrdinalIgnoreCase)))
            .Sum(t => t.SoTienXacNhan > 0 ? t.SoTienXacNhan : t.SoTienKhaiBao);
    }

    private static void UpsertLinkedBookingLocked(CustomerBookingRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.LinkedBookingCode)) return;

        var confirmedPaid = SumConfirmedPaymentsLocked(req.MaYeuCau, req.LinkedBookingCode);
        var statusText = confirmedPaid > 0 ? "Đã cọc • Chờ buổi chụp" : "Đã xác nhận • Chờ đặt cọc";
        var stepIdx = confirmedPaid > 0 ? 2 : 1;

        var existingIdx = MockBookings.FindIndex(b =>
            string.Equals(b.MaLichChup, req.LinkedBookingCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.MaYeuCauGoc, req.MaYeuCau, StringComparison.OrdinalIgnoreCase));

        var booking = new CustomerMockBookingDto
        {
            MaLichChup = req.LinkedBookingCode,
            DemoCustomerId = req.DemoCustomerId,
            MaYeuCauGoc = req.MaYeuCau,
            TenKhachHang = req.TenKhachHang,
            MaGoiChup = req.MaGoiChup,
            TenGoi = req.TenGoi,
            MaPhong = string.IsNullOrWhiteSpace(req.AssignedRoomCode) ? (req.MaPhongUuTien ?? "P0001") : req.AssignedRoomCode,
            TenPhong = string.IsNullOrWhiteSpace(req.AssignedRoomName) ? (req.TenPhongUuTien ?? "Phòng Ánh Sáng") : req.AssignedRoomName,
            MaNhiepAnhGia = string.IsNullOrWhiteSpace(req.SuggestedPhotographerCode) ? "NV000004" : req.SuggestedPhotographerCode,
            TenNhiepAnhGia = string.IsNullOrWhiteSpace(req.SuggestedPhotographerName) ? "Phạm Gia Huy" : req.SuggestedPhotographerName,
            NgayChup = req.NgayChupMongMuon,
            KhungGio = req.KhungGioMongMuon,
            TrangThaiLich = statusText,
            PipelineStageLabel = confirmedPaid > 0 ? "Đã xác nhận & Đã đặt cọc" : "Đã xác nhận từ yêu cầu",
            PipelineStepIndex = stepIdx,
            TongGiaTri = req.TongTamTinh,
            DaThanhToan = confirmedPaid,
            SelectedServices = req.ExtraServices.ToList(),
            GhiChu = string.IsNullOrWhiteSpace(req.PhanHoiChoKhach)
                ? $"Chuyển đổi từ yêu cầu {req.MaYeuCau}."
                : req.PhanHoiChoKhach,
            DeliveryStageNote = "Lịch chụp đã được xác nhận. Sau buổi chụp, album sẽ được cập nhật tại mục Album ảnh."
        };

        if (existingIdx >= 0)
        {
            MockBookings[existingIdx] = booking;
        }
        else
        {
            MockBookings.Insert(0, booking);
        }
    }

    private static void SyncLinkedBookingPaymentTotalsLocked(string referenceCode)
    {
        var req = BookingRequests.FirstOrDefault(r =>
            string.Equals(r.MaYeuCau, referenceCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.LinkedBookingCode, referenceCode, StringComparison.OrdinalIgnoreCase));

        var matchingBookings = MockBookings.Where(b =>
            string.Equals(b.MaLichChup, referenceCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.MaYeuCauGoc, referenceCode, StringComparison.OrdinalIgnoreCase) ||
            (req is not null && string.Equals(b.MaLichChup, req.LinkedBookingCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var b in matchingBookings)
        {
            var paid = SumConfirmedPaymentsLocked(b.MaYeuCauGoc, b.MaLichChup);
            b.DaThanhToan = paid;
            if (b.PipelineStepIndex <= 2)
            {
                if (paid >= b.TongGiaTri && b.TongGiaTri > 0)
                {
                    b.TrangThaiLich = "Đã thanh toán đủ • Chờ buổi chụp";
                    b.PipelineStageLabel = "Đã xác nhận & Đã thanh toán";
                    b.PipelineStepIndex = 2;
                }
                else if (paid > 0)
                {
                    b.TrangThaiLich = "Đã cọc • Chờ buổi chụp";
                    b.PipelineStageLabel = "Đã xác nhận & Đã đặt cọc";
                    b.PipelineStepIndex = 2;
                }
                else
                {
                    b.TrangThaiLich = "Đã xác nhận • Chờ đặt cọc";
                    b.PipelineStageLabel = "Đã xác nhận từ yêu cầu";
                    b.PipelineStepIndex = 1;
                }
            }
        }
    }

    public static IReadOnlyList<StudioNotificationDto> GetNotifications(string? demoCustomerId = null)
    {
        EnsureSeeded();
        var targetId = demoCustomerId ?? CurrentCustomerSession?.Profile.DemoAccountId;
        lock (SyncLock)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                return Notifications.ToList();

            return Notifications
                .Where(n => string.IsNullOrWhiteSpace(n.DemoCustomerId) ||
                            string.Equals(n.DemoCustomerId, targetId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public static string NextRequestCode()
    {
        EnsureSeeded();
        lock (SyncLock) return $"REQ-{DateTime.Now:yyyyMM}-{BookingRequests.Count + 1:000}";
    }

    public static string NextTransactionCode()
    {
        EnsureSeeded();
        lock (SyncLock) return $"TXN-{DateTime.Now:yyyyMM}-{PaymentSubmissions.Count + 1:000}";
    }

    public static VietQrInstructionDto BuildVietQrInstruction(string referenceCode, decimal amount) => new(
        BankDisplayName: "Ngân hàng TMCP Á Châu (ACB — Minh họa Demo)",
        AccountHolder: "ATELIER LUMIERE STUDIO (DEMO)",
        MaskedAccountNumber: "1908 •••• 6868",
        TransferSyntax: $"PHOENIX {referenceCode}",
        SuggestedAmount: amount,
        ReferenceCode: referenceCode,
        DisclaimerText: "Lưu ý quan trọng: Quét VietQR KHÔNG tự động xác nhận thanh toán hay chốt lịch chụp. Sau khi chuyển khoản, vui lòng xác nhận để nhân viên đối soát thủ công.");

    public static ReceiptDocumentDto BuildReceiptPreview(
        ReceiptFormatKind format,
        string referenceCode,
        string customerName,
        string customerPhone,
        string packageName,
        string roomName,
        IReadOnlyList<BookingRequestServiceLineDto> services,
        decimal packagePrice,
        decimal paidAmount,
        string paymentMethodLabel,
        string staffIssuerName)
    {
        var items = new List<ReceiptLineItemDto>
        {
            new(packageName, "Gói chụp", 1, packagePrice, packagePrice)
        };
        items.AddRange(services.Select(s => new ReceiptLineItemDto(s.TenDichVu, s.DonViTinh, s.SoLuong, s.DonGia, s.ThanhTien)));
        var subtotal = items.Sum(x => x.LineTotal);
        return new ReceiptDocumentDto(
            ReceiptNumber: $"BN-DEMO-{DateTime.Now:yyyyMMdd}-{Math.Abs(referenceCode.GetHashCode()) % 900 + 100}",
            Format: format,
            IssuedAtLabel: DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            ReferenceCode: referenceCode,
            CustomerName: customerName,
            CustomerPhone: customerPhone,
            PackageName: packageName,
            RoomName: roomName,
            LineItems: items,
            Subtotal: subtotal,
            PaidAmount: paidAmount,
            RemainingAmount: Math.Max(0, subtotal - paidAmount),
            PaymentMethodLabel: paymentMethodLabel,
            StaffIssuerName: staffIssuerName,
            FooterDisclaimer: "Biên nhận xem trước (Frontend Demo) • Sử dụng để minh họa bố cục in cho giai đoạn tích hợp Backend.");
    }

    public static string GetStatusVietnameseLabel(BookingRequestStatus status) => status switch
    {
        BookingRequestStatus.CHO_XU_LY => "Chờ xử lý",
        BookingRequestStatus.CAN_BO_SUNG => "Cần bổ sung",
        BookingRequestStatus.DA_XAC_NHAN => "Đã xác nhận",
        BookingRequestStatus.DA_TU_CHOI => "Đã từ chối",
        BookingRequestStatus.KHACH_HUY => "Khách đã hủy",
        _ => "Không xác định"
    };

    public static string GetPaymentStatusLabel(PaymentReconciliationStatus status) => status switch
    {
        PaymentReconciliationStatus.CHO_DOI_SOAT => "Chờ đối soát thủ công",
        PaymentReconciliationStatus.DA_KHOP_THU_CONG => "Đã khớp thủ công",
        PaymentReconciliationStatus.SAI_LECH_THONG_TIN => "Sai lệch số tiền / nội dung",
        PaymentReconciliationStatus.THANH_TOAN_TIEN_MAT => "Thu tiền mặt tại quầy",
        PaymentReconciliationStatus.TU_CHOI => "Từ chối / Không hợp lệ",
        _ => "Chờ xử lý"
    };

    private static string GetLinkStateHeadline(CustomerIdentityLinkState state) => state switch
    {
        CustomerIdentityLinkState.VerifiedOwnership => "Đã xác minh quyền sở hữu hồ sơ",
        CustomerIdentityLinkState.PendingVerification => "Đang chờ nhân viên đối chiếu thông tin",
        CustomerIdentityLinkState.AuthorizedManualLink => "Đã liên kết thủ công bởi Tiếp nhận/Quản trị",
        CustomerIdentityLinkState.RejectedVerification => "Từ chối liên kết do chưa khớp dữ liệu",
        _ => "Tài khoản độc lập chưa liên kết hồ sơ cũ"
    };

    private static string GetLinkStateDescription(CustomerIdentityLinkState state) => state switch
    {
        CustomerIdentityLinkState.VerifiedOwnership => "Đang ở trạng thái đã xác minh (Demo) để bạn trải nghiệm đầy đủ Lịch chụp, Thanh toán và Album ảnh.",
        CustomerIdentityLinkState.PendingVerification => "Yêu cầu đối chiếu đang chờ xác minh. Bạn vẫn gửi được yêu cầu đặt lịch mới nhưng chưa xem được lịch sử cũ.",
        CustomerIdentityLinkState.AuthorizedManualLink => "Hồ sơ được liên kết thủ công có ghi nhận người duyệt.",
        CustomerIdentityLinkState.RejectedVerification => "Thông tin chưa khớp, quyền xem hồ sơ cũ đang bị khóa.",
        _ => "Tài khoản chưa liên kết dữ liệu khách hàng tại studio."
    };

    private static void SeedBaselineDataLocked()
    {
        var customerDemo = new CustomerDemoProfileDto(
            DemoAccountId: "KH_DEMO_001",
            LinkedCustomerCode: "KH000001",
            FullName: "Nguyễn Thảo Vy",
            PhoneNumber: "0908 123 456",
            Email: "khachhang.demo@atelier.local",
            PreferredContactChannel: "Điện thoại & Email",
            Address: "Quận 3, TP. Hồ Chí Minh (Dữ liệu Demo)",
            Note: "Tài khoản khách hàng mẫu phục vụ trình diễn giao diện Desktop.",
            LinkState: CustomerIdentityLinkState.VerifiedOwnership,
            LinkStatusHeadline: GetLinkStateHeadline(CustomerIdentityLinkState.VerifiedOwnership),
            LinkStatusDescription: GetLinkStateDescription(CustomerIdentityLinkState.VerifiedOwnership),
            LastUpdatedLabel: "Phiên bản v2.2",
            IsSamplePersona: true);
        DemoProfiles.Add(customerDemo);

        var unlinkedDemo = new CustomerDemoProfileDto(
            DemoAccountId: "KH_DEMO_002",
            LinkedCustomerCode: null,
            FullName: "Trần Minh Quân (Khách mới)",
            PhoneNumber: "0912 888 777",
            Email: "khachmoi.demo@atelier.local",
            PreferredContactChannel: "Điện thoại",
            Address: "TP. Thủ Đức (Dữ liệu Demo)",
            Note: "Hồ sơ mẫu chưa liên kết dữ liệu lịch cũ.",
            LinkState: CustomerIdentityLinkState.Unlinked,
            LinkStatusHeadline: GetLinkStateHeadline(CustomerIdentityLinkState.Unlinked),
            LinkStatusDescription: GetLinkStateDescription(CustomerIdentityLinkState.Unlinked),
            LastUpdatedLabel: "Phiên bản v2.2",
            IsSamplePersona: true);
        DemoProfiles.Add(unlinkedDemo);

        var hoangAnhDemo = new CustomerDemoProfileDto(
            DemoAccountId: "KH_DEMO_003",
            LinkedCustomerCode: null,
            FullName: "Trần Hoàng Anh",
            PhoneNumber: "0933 456 789",
            Email: "hoanganh.demo@atelier.local",
            PreferredContactChannel: "Điện thoại & Email",
            Address: "Quận 7, TP. Hồ Chí Minh (Dữ liệu Demo)",
            Note: "Khách hàng độc lập phục vụ hộp thư điều phối và đối soát của nhân viên.",
            LinkState: CustomerIdentityLinkState.Unlinked,
            LinkStatusHeadline: GetLinkStateHeadline(CustomerIdentityLinkState.Unlinked),
            LinkStatusDescription: GetLinkStateDescription(CustomerIdentityLinkState.Unlinked),
            LastUpdatedLabel: "Phiên bản v2.2",
            IsSamplePersona: true);
        DemoProfiles.Add(hoangAnhDemo);

        BookingRequests.AddRange([
            new CustomerBookingRequestDto
            {
                MaYeuCau = "REQ-202611-001",
                DemoCustomerId = "KH_DEMO_001",
                TenKhachHang = "Nguyễn Thảo Vy",
                SoDienThoai = "0908 123 456",
                Email = "khachhang.demo@atelier.local",
                MaGoiChup = "GC000004",
                TenGoi = "Cặp đôi",
                NgayChupMongMuon = DateTime.Today.AddDays(4),
                KhungGioMongMuon = "09:00 - 12:00",
                ThoiLuongPhut = 180,
                MaPhongUuTien = "P0002",
                TenPhongUuTien = "Phòng Cổ Điển",
                SoNguoiDuKien = 2,
                ExtraServices =
                [
                    new("DV000001", "Trang điểm", "Người", 450_000m, 1),
                    new("DV000002", "Thuê trang phục", "Bộ", 300_000m, 2)
                ],
                TamTinhGoi = 2_500_000m,
                GhiChuKhachHang = "Mong muốn concept váy cưới trắng và vest nam tại Phòng Cổ Điển buổi sáng.",
                TrangThai = BookingRequestStatus.CHO_XU_LY,
                PhanHoiChoKhach = "Yêu cầu đang chờ bộ phận Tiếp nhận kiểm tra lịch phòng P0002 và nhiếp ảnh gia.",
                ConflictCheckSummary = "Chưa chạy kiểm tra lịch (Demo)",
                CreatedAtLabel = "10:15 hôm nay",
                Timeline =
                [
                    new("10:15 hôm nay", "Nguyễn Thảo Vy", "Gửi yêu cầu đặt lịch", "Chọn gói Cặp đôi (GC000004) + trang điểm + thuê 2 bộ trang phục.", BookingRequestStatus.CHO_XU_LY)
                ]
            },
            new CustomerBookingRequestDto
            {
                MaYeuCau = "REQ-202611-002",
                DemoCustomerId = "KH_DEMO_003",
                TenKhachHang = "Trần Hoàng Anh",
                SoDienThoai = "0933 456 789",
                Email = "hoanganh.demo@atelier.local",
                MaGoiChup = "GC000002",
                TenGoi = "Gia đình",
                NgayChupMongMuon = DateTime.Today.AddDays(6),
                KhungGioMongMuon = "14:00 - 17:00",
                ThoiLuongPhut = 180,
                MaPhongUuTien = "P0001",
                TenPhongUuTien = "Phòng Ánh Sáng",
                SoNguoiDuKien = 5,
                ExtraServices = [new("DV000004", "In album", "Cuốn", 850_000m, 1)],
                TamTinhGoi = 2_800_000m,
                GhiChuKhachHang = "Gia đình 5 thành viên có ông bà lớn tuổi, ưu tiên tầng trệt.",
                TrangThai = BookingRequestStatus.CAN_BO_SUNG,
                PhanHoiChoKhach = "Vui lòng xác nhận chiều cao/cỡ áo dài đỏ nếu gia đình muốn mượn thêm trang phục.",
                GhiChuNoiBo = "Đề xuất ghép Nhiếp ảnh gia Võ Khánh Linh (NV000005).",
                SuggestedPhotographerCode = "NV000005",
                SuggestedPhotographerName = "Võ Khánh Linh",
                AssignedRoomCode = "P0001",
                AssignedRoomName = "Phòng Ánh Sáng",
                ConflictCheckSummary = "Khả dụng dự kiến (chưa có xung đột lịch P0001 - Demo)",
                CreatedAtLabel = "Hôm qua 16:40",
                Timeline =
                [
                    new("Hôm qua 17:05", "Lễ tân Tiếp nhận", "Yêu cầu bổ sung thông tin", "Nhờ khách cung cấp thông số trang phục trước khi xác nhận lịch.", BookingRequestStatus.CAN_BO_SUNG),
                    new("Hôm qua 16:40", "Trần Hoàng Anh", "Gửi yêu cầu đặt lịch", "Đề xuất gói Gia đình (GC000002) cho 5 thành viên.", BookingRequestStatus.CHO_XU_LY)
                ]
            },
            new CustomerBookingRequestDto
            {
                MaYeuCau = "REQ-202611-003",
                DemoCustomerId = "KH_DEMO_001",
                TenKhachHang = "Nguyễn Thảo Vy",
                SoDienThoai = "0908 123 456",
                Email = "khachhang.demo@atelier.local",
                MaGoiChup = "GC000001",
                TenGoi = "Chân dung cá nhân",
                NgayChupMongMuon = DateTime.Today.AddDays(2),
                KhungGioMongMuon = "09:30 - 11:30",
                ThoiLuongPhut = 120,
                MaPhongUuTien = "P0001",
                TenPhongUuTien = "Phòng Ánh Sáng",
                SoNguoiDuKien = 1,
                ExtraServices = [new("DV000001", "Trang điểm", "Người", 450_000m, 1)],
                TamTinhGoi = 1_500_000m,
                GhiChuKhachHang = "Chụp chân dung hồ sơ cá nhân ánh sáng tự nhiên.",
                TrangThai = BookingRequestStatus.DA_XAC_NHAN,
                PhanHoiChoKhach = "Đã xác nhận điều phối Nhiếp ảnh gia Phạm Gia Huy (NV000004) tại Phòng Ánh Sáng.",
                SuggestedPhotographerCode = "NV000004",
                SuggestedPhotographerName = "Phạm Gia Huy",
                AssignedRoomCode = "P0001",
                AssignedRoomName = "Phòng Ánh Sáng",
                ConflictCheckSummary = "Không xung đột • Đã sẵn sàng chuyển đổi lịch chính thức",
                LinkedBookingCode = "LCDEMO-202611-001",
                CreatedAtLabel = "3 ngày trước",
                Timeline =
                [
                    new("2 ngày trước 09:30", "Quản trị điều phối", "Xác nhận yêu cầu", "Chuyển sang lịch LCDEMO-202611-001 • Phòng P0001 • NV000004 Phạm Gia Huy.", BookingRequestStatus.DA_XAC_NHAN),
                    new("3 ngày trước 14:20", "Nguyễn Thảo Vy", "Gửi yêu cầu đặt lịch", "Đề xuất gói Chân dung cá nhân (GC000001).", BookingRequestStatus.CHO_XU_LY)
                ]
            }
        ]);

        MockBookings.AddRange([
            new CustomerMockBookingDto
            {
                MaLichChup = "LCDEMO-202611-001",
                DemoCustomerId = "KH_DEMO_001",
                MaYeuCauGoc = "REQ-202611-003",
                TenKhachHang = "Nguyễn Thảo Vy",
                MaGoiChup = "GC000001",
                TenGoi = "Chân dung cá nhân",
                MaPhong = "P0001",
                TenPhong = "Phòng Ánh Sáng",
                MaNhiepAnhGia = "NV000004",
                TenNhiepAnhGia = "Phạm Gia Huy",
                NgayChup = DateTime.Today.AddDays(2),
                KhungGio = "09:30 - 11:30",
                TrangThaiLich = "Đã cọc • Chờ buổi chụp",
                PipelineStageLabel = "Đã xác nhận & Đã đặt cọc",
                PipelineStepIndex = 2,
                TongGiaTri = 1_950_000m,
                DaThanhToan = 1_000_000m,
                SelectedServices = [new("DV000001", "Trang điểm", "Người", 450_000m, 1)],
                GhiChu = "Đến sớm 15 phút để chuyên viên trang điểm chuẩn bị.",
                DeliveryStageNote = "Album sẽ mở sau khi hoàn tất buổi chụp và chọn ảnh."
            },
            new CustomerMockBookingDto
            {
                MaLichChup = "LCDEMO-202610-018",
                DemoCustomerId = "KH_DEMO_001",
                MaYeuCauGoc = null,
                TenKhachHang = "Nguyễn Thảo Vy",
                MaGoiChup = "GC000003",
                TenGoi = "Em bé",
                MaPhong = "P0003",
                TenPhong = "Phòng Em Bé",
                MaNhiepAnhGia = "NV000007",
                TenNhiepAnhGia = "Bùi Ngọc Mai",
                NgayChup = DateTime.Today.AddDays(-12),
                KhungGio = "10:00 - 12:30",
                TrangThaiLich = "Hoàn thành",
                PipelineStageLabel = "Đã bàn giao album",
                PipelineStepIndex = 5,
                TongGiaTri = 2_200_000m,
                DaThanhToan = 2_200_000m,
                SelectedServices = [],
                GhiChu = "Buổi chụp gia đình cùng bé Sóc tại Phòng Em Bé.",
                DeliveryStageNote = "Đã hoàn tất chỉnh sửa và bàn giao 10 ảnh thành phẩm (Demo)."
            }
        ]);

        PaymentSubmissions.AddRange([
            new PaymentSubmissionDto
            {
                MaGiaoDichDemo = "TXN-202611-001",
                DemoCustomerId = "KH_DEMO_001",
                MaThamChieu = "REQ-202611-001",
                TargetType = "YEU_CAU",
                TenKhachHang = "Nguyễn Thảo Vy",
                SoDienThoai = "0908 123 456",
                PhuongThuc = PaymentMethodKind.VietQrChuyenKhoan,
                SoTienKhaiBao = 1_000_000m,
                SoTienXacNhan = 0m,
                NoiDungChuyenKhoan = "PHOENIX REQ-202611-001",
                TrangThai = PaymentReconciliationStatus.CHO_DOI_SOAT,
                CustomerConfirmedTransfer = true,
                EvidenceFileName = "minh_chung_vietqr_demo.jpg",
                EvidenceNotes = "Khách hàng khai báo đã chuyển cọc 1,000,000 đ qua VietQR lúc 10:25.",
                CreatedAtLabel = "10:26 hôm nay"
            },
            new PaymentSubmissionDto
            {
                MaGiaoDichDemo = "TXN-202611-002",
                DemoCustomerId = "KH_DEMO_001",
                MaThamChieu = "LCDEMO-202611-001",
                TargetType = "LICH_CHUP",
                TenKhachHang = "Nguyễn Thảo Vy",
                SoDienThoai = "0908 123 456",
                PhuongThuc = PaymentMethodKind.ChuyenKhoanThuCong,
                SoTienKhaiBao = 1_000_000m,
                SoTienXacNhan = 1_000_000m,
                NoiDungChuyenKhoan = "PHOENIX LCDEMO-202611-001",
                TrangThai = PaymentReconciliationStatus.DA_KHOP_THU_CONG,
                CustomerConfirmedTransfer = true,
                StaffReviewerNote = "Nhân sự Kế toán đối chiếu sao kê ngân hàng khớp đủ 1,000,000 đ.",
                CreatedAtLabel = "2 ngày trước",
                ReconciledAtLabel = "2 ngày trước 10:15"
            },
            new PaymentSubmissionDto
            {
                MaGiaoDichDemo = "TXN-202611-003",
                DemoCustomerId = "KH_DEMO_003",
                MaThamChieu = "REQ-202611-002",
                TargetType = "YEU_CAU",
                TenKhachHang = "Trần Hoàng Anh",
                SoDienThoai = "0933 456 789",
                PhuongThuc = PaymentMethodKind.VietQrChuyenKhoan,
                SoTienKhaiBao = 900_000m,
                SoTienXacNhan = 500_000m,
                NoiDungChuyenKhoan = "CK coc chup anh gia dinh",
                TrangThai = PaymentReconciliationStatus.SAI_LECH_THONG_TIN,
                CustomerConfirmedTransfer = true,
                EvidenceNotes = "Thiếu cú pháp PHOENIX và số tiền thực nhận trên sao kê là 500,000 đ.",
                StaffReviewerNote = "Tạm giữ trạng thái sai lệch để nhân sự liên hệ khách bổ sung chứng từ.",
                CreatedAtLabel = "Hôm qua 17:10",
                ReconciledAtLabel = "Hôm qua 17:45"
            }
        ]);

        var pkg1 = VisualAssetService.GetCoverEntry("GC000001");
        var pkg3 = VisualAssetService.GetCoverEntry("GC000003");
        var res9 = VisualAssetService.GetCoverEntry("TN000009");
        PhotoAlbums.Add(new CustomerPhotoAlbumDto
        {
            AlbumId = "ALB-202610-018",
            DemoCustomerId = "KH_DEMO_001",
            MaLichChup = "LCDEMO-202610-018",
            Title = "Album Em Bé • Buổi chụp Phòng Em Bé",
            Subtitle = "Nhiếp ảnh gia Bùi Ngọc Mai • Đã bàn giao (Bản xem trước Demo)",
            DeliveryStatus = "Đã bàn giao (Demo)",
            DeliveryProgressPercent = 100,
            OwnerScopeLabel = "Chỉ hiển thị với chủ sở hữu đã xác minh (KH000001 / Nguyễn Thảo Vy)",
            UpdatedAtLabel = "5 ngày trước",
            CoverAssetId = pkg3.AssetId,
            IsDemoSample = true,
            Photos =
            [
                new("IMG-01", "Khung cảnh Phòng Em Bé", "Bối cảnh giỏ mây và ánh sáng tự nhiên (Ảnh minh họa mẫu Demo)", pkg3.ExpectedRelativePath, pkg3.ResolvedFullPath, "Shot 01 / F2.8", "10:15", true),
                new("IMG-02", "Trang phục bé chuẩn bị", "Trang phục công chúa dịu nhẹ tại studio (Ảnh minh họa mẫu Demo)", res9.ExpectedRelativePath, res9.ResolvedFullPath, "Shot 02 / F2.2", "10:35", true),
                new("IMG-03", "Góc ánh sáng chân dung mẹ & bé", "Khung ghế studio ánh sáng dịu (Ảnh minh họa mẫu Demo)", pkg1.ExpectedRelativePath, pkg1.ResolvedFullPath, "Shot 03 / F3.2", "11:10", false)
            ]
        });

        Notifications.AddRange([
            new("NTF-001", "Lịch LCDEMO-202611-001 sắp diễn ra", "Buổi chụp Chân dung cá nhân tại Phòng Ánh Sáng với Nhiếp ảnh gia Phạm Gia Huy lúc 09:30.", "Lịch hẹn", "Hôm nay", false, "bookings", "KH_DEMO_001"),
            new("NTF-002", "Giao dịch TXN-202611-001 chờ đối soát", "Khai báo chuyển khoản VietQR 1,000,000 đ cho REQ-202611-001 đang chờ nhân viên xác nhận thủ công.", "Thanh toán", "Hôm nay", false, "payments", "KH_DEMO_001"),
            new("NTF-003", "Album ALB-202610-018 đã sẵn sàng xem", "Bộ ảnh mẫu (Demo) của lịch chụp Em bé đã hoàn tất chỉnh sửa.", "Album ảnh", "5 ngày trước", true, "albums", "KH_DEMO_001")
        ]);
    }
}

