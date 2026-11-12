namespace StudioManager.WinForms.Contracts;

/// <summary>
/// Active surface entry mode selected on the desktop login doorway.
/// Keeps SQL Server Staff/Admin workspace isolated from Customer Demo UX.
/// </summary>
public enum AccessMode
{
    StaffAdminSql = 0,
    CustomerDemo = 1
}

/// <summary>
/// Five identity-linking lifecycle states previewed in Customer Desktop UX
/// prior to Codex SQL persistence and verification implementation.
/// </summary>
public enum CustomerIdentityLinkState
{
    Unlinked = 0,
    PendingVerification = 1,
    VerifiedOwnership = 2,
    RejectedVerification = 3,
    AuthorizedManualLink = 4
}

public sealed record CustomerDemoProfileDto(
    string DemoAccountId,
    string? LinkedCustomerCode,
    string FullName,
    string PhoneNumber,
    string Email,
    string PreferredContactChannel,
    string Address,
    string Note,
    CustomerIdentityLinkState LinkState,
    string LinkStatusHeadline,
    string LinkStatusDescription,
    string LastUpdatedLabel,
    bool IsSamplePersona);

public sealed record CustomerLoginRequestDto(
    string Identifier,
    string Password,
    bool RememberIdentifier);

public sealed record CustomerRegistrationRequestDto(
    string FullName,
    string PhoneNumber,
    string Email,
    string Password,
    string ConfirmPassword,
    string? ExistingBookingReference,
    bool AcceptTerms);

public sealed record CustomerRecoveryRequestDto(
    string RegisteredEmailOrPhone,
    string FullNameForVerification,
    string? ReferenceBookingCode,
    string? VerificationNote);

public sealed record CustomerPasswordChangeDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);

public sealed record IdentityLinkEvaluationDto(
    CustomerIdentityLinkState State,
    string StateLabel,
    string SummaryTitle,
    string Explanation,
    string NextRecommendedAction,
    bool CanViewExistingBookings,
    string? CandidateCustomerCode);

public sealed class CustomerSessionContext
{
    public required string SessionId { get; init; }
    public required CustomerDemoProfileDto Profile { get; set; }
    public string ActiveRoleLabel => "Khách hàng";
    public bool IsDemoMode => true;
    public DateTime SignedInAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Canonical security role recognized by the Unified Login doorway and Codex backend adapter.
/// Photographers operate under <see cref="Staff"/> security role with position specialization.
/// </summary>
public enum UserSecurityRole
{
    Admin = 0,
    Staff = 1,
    Customer = 2
}

/// <summary>
/// Target desktop shell opened immediately after backend or demo authentication succeeds.
/// </summary>
public enum TargetDesktopShell
{
    InternalOperationsMainForm = 0,
    CustomerDesktopMainForm = 1
}

public sealed record AuthenticatedPrincipal(
    string AccountId,
    string DisplayName,
    UserSecurityRole SecurityRole,
    bool IsPhotographerPosition,
    bool MustChangePassword,
    bool IsDemoSession);

public sealed record AuthRoutingDecision(
    bool IsAllowed,
    TargetDesktopShell TargetShell,
    string? BlockReason,
    AuthenticatedPrincipal? Principal);

/// <summary>
/// Unified authentication contract ready for Codex SQL Server implementation.
/// One desktop login form authenticates Staff, Admin, Photographer, and Customer accounts
/// without exposing a client-side role toggle.
/// </summary>
public interface IUnifiedAuthenticationService
{
    Task<AuthRoutingDecision> AuthenticateAsync(string identifier, string password, CancellationToken ct = default);
}

public static class UnifiedAuthRouter
{
    public static AuthRoutingDecision ResolveRoute(AuthenticatedPrincipal principal)
    {
        return principal.SecurityRole switch
        {
            UserSecurityRole.Admin => new(true, TargetDesktopShell.InternalOperationsMainForm, null, principal),
            UserSecurityRole.Staff => new(true, TargetDesktopShell.InternalOperationsMainForm, null, principal),
            UserSecurityRole.Customer => new(true, TargetDesktopShell.CustomerDesktopMainForm, null, principal),
            _ => new(false, TargetDesktopShell.InternalOperationsMainForm, "Vai trò tài khoản chưa được phân quyền hợp lệ.", null)
        };
    }
}

