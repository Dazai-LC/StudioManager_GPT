using StudioManager.Application;
using Xunit;

namespace StudioManager.Tests;

public sealed class BookingPackageSnapshotTests
{
    private static readonly BookingPackageSnapshot Agreed = new(5, "Gói đã thỏa thuận", 1_000_000m);

    [Theory]
    [InlineData(0)]
    [InlineData(1_500_000)]
    [InlineData(900_000)]
    public void ScheduleOnlyEdit_PreservesAgreedNameAndPrice_WhenCatalogChanges(int catalogPrice)
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 5, null, new(5, "Tên gói mới", catalogPrice), true);

        Assert.True(result.Success);
        Assert.Equal(Agreed, result.Data);
    }

    [Fact]
    public void ScheduleOnlyEdit_KeepsAgreement_WhenOriginalPackageIsNoLongerSold()
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 5, null, null, false);

        Assert.True(result.Success);
        Assert.Equal(Agreed, result.Data);
    }

    [Fact]
    public void ChangingPackage_RequiresExplicitQuoteAcceptance()
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, null, new(6, "Gói khác", 2_000_000m), true);

        Assert.False(result.Success);
        Assert.Equal("QUOTE_ACCEPTANCE_REQUIRED", result.Code);
        Assert.Null(result.Data);
    }

    [Fact]
    public void AcceptedPackageChange_UsesAuthoritativeQuote()
    {
        var offered = new BookingPackageSnapshot(6, "Gói mới", 2_000_000m);
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(5, 1_000_000m, 2_000_000m, "Khách chọn gói mới"), offered, true);

        Assert.True(result.Success);
        Assert.Equal(offered, result.Data);
    }

    [Fact]
    public void SamePackageRequotation_RequiresExplicitAcceptanceAndUsesNewQuote()
    {
        var offered = new BookingPackageSnapshot(5, "Gói báo giá lại", 1_200_000m);
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 5, new(5, 1_000_000m, 1_200_000m, "Khách đồng ý báo giá lại"), offered, true);

        Assert.True(result.Success);
        Assert.Equal(offered, result.Data);
    }

    [Theory]
    [InlineData(4, 1_000_000)]
    [InlineData(5, 900_000)]
    public void ConcurrentChangeToOriginalAgreement_RejectsStaleAcceptance(int originalPackageId, int originalPrice)
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(originalPackageId, originalPrice, 2_000_000m, "Đổi gói"), new(6, "Gói mới", 2_000_000m), true);

        Assert.False(result.Success);
        Assert.Equal("STALE_BOOKING_QUOTE", result.Code);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ConcurrentCatalogPriceChange_RejectsStaleQuoteInsteadOfSilentlyChargingNewPrice()
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(5, 1_000_000m, 2_000_000m, "Đổi gói"), new(6, "Gói mới", 2_100_000m), true);

        Assert.False(result.Success);
        Assert.Equal("STALE_CATALOG_QUOTE", result.Code);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExplicitRequotation_RequiresAuditableReason(string? reason)
    {
        var result = BookingPackageSnapshotPolicy.ValidateIntent(Agreed, 6, new(5, 1_000_000m, 2_000_000m, reason!));

        Assert.False(result.Success);
        Assert.Equal("QUOTE_REASON_REQUIRED", result.Code);
    }

    [Fact]
    public void ExplicitRequotation_RejectsReasonThatExceedsAuditColumn()
    {
        var result = BookingPackageSnapshotPolicy.ValidateIntent(Agreed, 6, new(5, 1_000_000m, 2_000_000m, new string('x', 501)));

        Assert.False(result.Success);
        Assert.Equal("QUOTE_REASON_REQUIRED", result.Code);
    }

    [Fact]
    public void ExplicitQuote_CanAcceptZeroPrice()
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(5, 1_000_000m, 0m, "Gói miễn phí được duyệt"), new(6, "Gói miễn phí", 0m), true);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Data!.Price);
    }

    [Fact]
    public void ExplicitQuote_RejectsNegativeAcceptedPrice()
    {
        var result = BookingPackageSnapshotPolicy.ValidateIntent(Agreed, 6, new(5, 1_000_000m, -1m, "Đổi gói"));

        Assert.False(result.Success);
        Assert.Equal("INVALID_QUOTE", result.Code);
    }

    [Fact]
    public void ExplicitQuote_RequiresAuthoritativeCatalogRecord()
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(5, 1_000_000m, 2_000_000m, "Đổi gói"), null, true);

        Assert.False(result.Success);
        Assert.Equal("INACTIVE_PACKAGE", result.Code);
        Assert.Null(result.Data);
    }

    [Theory]
    [InlineData(false, 6)]
    [InlineData(true, 7)]
    public void ExplicitQuote_RejectsUnavailableOrWrongPackage(bool active, int quotedPackageId)
    {
        var result = BookingPackageSnapshotPolicy.Resolve(Agreed, 6, new(5, 1_000_000m, 2_000_000m, "Đổi gói"), new(quotedPackageId, "Gói mới", 2_000_000m), active);

        Assert.False(result.Success);
        Assert.Equal("INACTIVE_PACKAGE", result.Code);
    }
}
