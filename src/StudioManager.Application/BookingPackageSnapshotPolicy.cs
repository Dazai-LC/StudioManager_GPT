namespace StudioManager.Application;

public sealed record BookingPackageSnapshot(int PackageId, string Name, decimal Price);

/// <summary>
/// Selects the agreed package snapshot independently of schedule edits. The repository
/// supplies the persisted snapshot and the authoritative catalog quote under its transaction.
/// An accepted client price is only an expectation to compare, never a source of pricing.
/// </summary>
public static class BookingPackageSnapshotPolicy
{
    public static Result ValidateIntent(BookingPackageSnapshot current, int requestedPackageId, PackageQuoteAcceptance? acceptance)
    {
        if (acceptance is null)
            return requestedPackageId == current.PackageId
                ? Result.Ok()
                : Result.Fail("QUOTE_ACCEPTANCE_REQUIRED", "Đổi gói chụp cần xác nhận báo giá mới và nhập lý do.");

        if (acceptance.ExpectedOriginalPackageId != current.PackageId || acceptance.ExpectedOriginalPrice != current.Price)
            return Result.Fail("STALE_BOOKING_QUOTE", "Giá hoặc gói đã chốt của lịch vừa thay đổi. Vui lòng tải lại lịch và xác nhận báo giá.");
        if (string.IsNullOrWhiteSpace(acceptance.Reason) || acceptance.Reason.Trim().Length > 500)
            return Result.Fail("QUOTE_REASON_REQUIRED", "Vui lòng nhập lý do đổi gói hoặc chấp nhận báo giá mới, tối đa 500 ký tự.");
        if (acceptance.AcceptedCatalogPrice < 0)
            return Result.Fail("INVALID_QUOTE", "Báo giá gói chụp không hợp lệ.");
        return Result.Ok();
    }

    public static Result<BookingPackageSnapshot> Resolve(
        BookingPackageSnapshot current,
        int requestedPackageId,
        PackageQuoteAcceptance? acceptance,
        BookingPackageSnapshot? authoritativeQuote,
        bool isPackageActive)
    {
        var validation = ValidateIntent(current, requestedPackageId, acceptance);
        if (!validation.Success)
            return Result<BookingPackageSnapshot>.Fail(validation.Code, validation.Message);

        // No catalog state or price is consulted for an unchanged historical agreement.
        if (acceptance is null)
            return Result<BookingPackageSnapshot>.Ok(current);

        if (authoritativeQuote is null || authoritativeQuote.PackageId != requestedPackageId || !isPackageActive)
            return Result<BookingPackageSnapshot>.Fail("INACTIVE_PACKAGE", "Gói chụp không còn áp dụng. Vui lòng chọn lại gói.");
        if (authoritativeQuote.Price != acceptance.AcceptedCatalogPrice)
            return Result<BookingPackageSnapshot>.Fail("STALE_CATALOG_QUOTE", "Giá gói chụp vừa thay đổi. Vui lòng tải lại báo giá và xác nhận lại.");
        return Result<BookingPackageSnapshot>.Ok(authoritativeQuote);
    }
}
