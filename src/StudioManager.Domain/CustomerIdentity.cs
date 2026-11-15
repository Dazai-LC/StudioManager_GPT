namespace StudioManager.Domain;

public enum CustomerLinkState { Pending, Approved, Rejected, Revoked }
public enum CustomerLinkDecision { Approve, Reject, Revoke }

public sealed record CustomerContact(string FullName, string Phone, string Email, string Address);
public sealed record CustomerAccount(long Id, string LoginEmail, string PasswordHash, Guid SecurityVersion,
    bool Active, CustomerContact Contact)
{
    public override string ToString() => "CustomerAccount { Credentials = [private] }";
}
public sealed record CustomerCredential(long AccountId, Guid SecurityVersion, string PasswordHash)
{
    public override string ToString() => "CustomerCredential { Credentials = [private] }";
}
public sealed record CustomerReviewer(int AccountId, VaiTro Role, string PasswordHash)
{
    public override string ToString() => "CustomerReviewer { Credentials = [private] }";
}
public sealed record CustomerProfile(string LoginEmail, CustomerContact Contact);
public sealed record CustomerLink(long Id, CustomerLinkState State, string Reference, DateTime RequestedAtUtc,
    string? LinkedCustomerCode);
public sealed record CustomerLinkReview(long Id, long CustomerAccountId, string LoginEmail, string FullName,
    string Reference, string Evidence, CustomerLinkState State, long? KhachHangId);
public sealed record OwnedCustomerContact(string Code, string FullName, string Phone, string? Email, string? Address);
