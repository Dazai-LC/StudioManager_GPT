using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;

namespace StudioManager.IdentityService;

/// <summary>Only P03 commands are exposed, with no caller-supplied principal or repository entry point.</summary>
public sealed partial class IdentityDispatcher(CustomerIdentityService authority, BookingRequestService? booking = null)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public void Disconnect() { authority.Logout(); booking?.ClearPreview(); }

    public async Task<IdentityResponse> DispatchAsync(IdentityRequest request, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (request.Command is not (IdentityCommand.CustomerLogin or IdentityCommand.StaffLogin or IdentityCommand.Register or IdentityCommand.Logout)
                && authority.IsSessionExpired)
            {
                Disconnect();
                return new(false, "SESSION_EXPIRED", "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại; yêu cầu chưa được xử lý.");
            }
            if (request.Command is IdentityCommand.CustomerLogin or IdentityCommand.StaffLogin or IdentityCommand.Logout or IdentityCommand.ChangePassword)
                booking?.ClearPreview();
            if (IsBookingCommand(request.Command))
                return booking is null ? new(false, "P04_UNAVAILABLE", "Dịch vụ chưa có P04. Cần triển khai gói đã được duyệt.") : await DispatchBookingAsync(request, ct);
            switch (request.Command)
            {
                case IdentityCommand.Register:
                    return request.Contact is null ? Invalid() : Response(await authority.RegisterAsync(request.Identifier!, request.Password!,
                        request.Confirmation!, Contact(request.Contact), ct));
                case IdentityCommand.CustomerLogin:
                    var login = await authority.LoginAsync(request.Identifier!, request.Password!, ct);
                    return new(login.Success, login.Code, login.Message) { Profile = login.Data is { } p ? Profile(p) : null };
                case IdentityCommand.StaffLogin:
                    return Response(await authority.StaffLoginAsync(request.Identifier!, request.Password!, ct));
                case IdentityCommand.Logout:
                    authority.Logout(); return new(true, "OK", "Đã đăng xuất.");
                case IdentityCommand.GetMyProfile:
                    var profile = await authority.GetMyProfileAsync(ct);
                    return new(profile.Success, profile.Code, profile.Message) { Profile = profile.Data is { } value ? Profile(value) : null };
                case IdentityCommand.UpdateMyProfile:
                    return request.Contact is null ? Invalid() : Response(await authority.UpdateMyProfileAsync(Contact(request.Contact), ct));
                case IdentityCommand.ChangePassword:
                    return Response(await authority.ChangePasswordAsync(request.Password!, request.NewPassword!, request.Confirmation!, ct));
                case IdentityCommand.RequestLink:
                    return Response(await authority.RequestLinkAsync(request.Reference!, request.Evidence!, ct));
                case IdentityCommand.GetMyLink:
                    var link = await authority.GetMyLinkAsync(ct);
                    return new(link.Success, link.Code, link.Message)
                    { Link = link.Data is { } l ? new(l.Id, l.State.ToString(), l.Reference, l.RequestedAtUtc, l.LinkedCustomerCode) : null };
                case IdentityCommand.GetMyLinkedCustomer:
                    var customer = await authority.GetMyOwnedCustomerAsync(ct);
                    return new(customer.Success, customer.Code, customer.Message)
                    { LinkedCustomer = customer.Data is { } c ? new(c.Code, c.FullName, c.Phone, c.Email, c.Address) : null };
                case IdentityCommand.ListLinkReviews:
                    var reviews = await authority.ListLinkReviewsAsync(request.BeforeLinkId, ct);
                    return new(reviews.Success, reviews.Code, reviews.Message)
                    { Reviews = reviews.Data?.Select(r => new LinkReviewDto(r.Id, r.CustomerAccountId, r.LoginEmail, r.FullName,
                        r.Reference, r.Evidence, r.State.ToString(), r.KhachHangId)).ToArray() };
                case IdentityCommand.ApproveLink:
                case IdentityCommand.RejectLink:
                case IdentityCommand.RevokeLink:
                    return Response(await authority.ReviewLinkAsync(request.LinkId, request.Command switch
                    { IdentityCommand.ApproveLink => CustomerLinkDecision.Approve, IdentityCommand.RejectLink => CustomerLinkDecision.Reject,
                        _ => CustomerLinkDecision.Revoke }, request.CustomerId, request.Reason!, ct));
                default: return Invalid();
            }
        }
        catch (Exception error)
        {
            Disconnect();
            // Fixed identifiers only; never reflect SQL messages or request data into the response.
            var ownership = request.Command is IdentityCommand.RequestLink or IdentityCommand.GetMyLink or IdentityCommand.GetMyLinkedCustomer or
                IdentityCommand.ListLinkReviews or IdentityCommand.ApproveLink or IdentityCommand.RejectLink or IdentityCommand.RevokeLink;
            var stage = ownership ? error switch
            {
                SqlException { Number: 229 } => "OWNERSHIP_SQL_PERMISSION",
                SqlException { Number: -2 or 1205 or 1222 } => "OWNERSHIP_SQL_TIMEOUT",
                SqlException => "OWNERSHIP_SQL_EXECUTION",
                UnauthorizedAccessException => "OWNERSHIP_ACTOR_REVALIDATION",
                _ => "OWNERSHIP_REQUEST_HANDLER"
            } : "P03.IdentityCommand";
            if (IsBookingCommand(request.Command)) stage = "P04.BookingRequest";
            var report = SafeDiagnostics.Capture(error, stage, error is SqlException sql ? sql.Number : null);
            // A failed commit acknowledgement cannot prove rollback. Reuse the existing uncertain-outcome
            // contract for every unexpected booking mutation failure; the connection is already signed out.
            if (request.Command is IdentityCommand.SubmitBookingRequest or IdentityCommand.CancelBookingRequest or
                IdentityCommand.ApproveBookingRequest or IdentityCommand.RejectBookingRequest)
                return new(false, "OUTCOME_UNKNOWN", "Chưa xác định được kết quả thao tác. Vui lòng đăng nhập lại và tải trạng thái trước khi thực hiện tiếp; ứng dụng không tự gửi lại yêu cầu. Mã tra cứu: " + report.CorrelationId + ".");
            return new(false, "SERVICE_ERROR", ownership
                ? $"{report.Message} Mã bước: {stage}." : report.Message);
        }
        finally { _gate.Release(); }
    }
    private static CustomerContact Contact(ContactDto contact) => new(contact.FullName, contact.Phone, contact.Email, contact.Address);
    private static ProfileDto Profile(CustomerProfile profile) => new(profile.LoginEmail,
        new(profile.Contact.FullName, profile.Contact.Phone, profile.Contact.Email, profile.Contact.Address));
    private static IdentityResponse Response(Result result) => new(result.Success, result.Code, result.Message);
    private static IdentityResponse Invalid() => new(false, "INVALID_REQUEST", "Yêu cầu không hợp lệ.");
}
