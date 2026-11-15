using StudioManager.Domain;

namespace StudioManager.Application;

// Implemented only inside the trusted service. These types never cross the customer pipe.
public interface ICustomerIdentityRepository
{
    Task<CustomerAccount?> FindByLoginAsync(string normalizedEmail, CancellationToken ct = default);
    Task<CustomerAccount?> GetAccountAsync(long accountId, CancellationToken ct = default);
    Task<Result> RegisterAsync(string normalizedEmail, string hash, CustomerContact contact, CancellationToken ct = default);
    Task<Result> UpdateContactAsync(CustomerCredential actor, CustomerContact contact, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(CustomerCredential actor, string newHash, CancellationToken ct = default);
    Task<CustomerLink?> GetMyLinkAsync(CustomerCredential actor, CancellationToken ct = default);
    Task<Result> RequestLinkAsync(CustomerCredential actor, string reference, string evidence, CancellationToken ct = default);
    Task<OwnedCustomerContact?> GetOwnedCustomerAsync(CustomerCredential actor, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerLinkReview>> ListLinksAsync(CustomerReviewer reviewer, long? beforeLinkId = null, CancellationToken ct = default);
    Task<Result> ReviewLinkAsync(CustomerReviewer reviewer, long linkId, CustomerLinkDecision decision,
        long? khachHangId, string reason, CancellationToken ct = default);
}

public interface ICustomerStaffDirectory
{
    Task<TaiKhoan?> FindAsync(string username, CancellationToken ct = default);
    Task<TaiKhoan?> GetAsync(int id, CancellationToken ct = default);
}
