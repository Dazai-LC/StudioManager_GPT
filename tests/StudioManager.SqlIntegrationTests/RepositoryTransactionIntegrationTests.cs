using StudioManager.Application;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

[CollectionDefinition("Isolated SQL mutation tests", DisableParallelization = true)]
public sealed class IsolatedSqlMutationCollection { }

[Collection("Isolated SQL mutation tests")]
public sealed class RepositoryTransactionIntegrationTests
{
    [SqlServerWriteFact]
    public async Task TimeEdit_PreservesAgreedPackageSnapshotDespiteCatalogPriceChange()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var result = await fixture.Repository.UpdateBookingAsync(fixture.BookingId,
            fixture.BookingInput(new DateTime(2099, 1, 2, 8, 0, 0)), null, fixture.Actor);

        Assert.True(result.Success, result.Code);
        var booking = await fixture.Repository.GetBookingAsync(fixture.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(SqlMutationFixture.HistoricalPrice, booking.GiaGoiChot);
        Assert.Equal(SqlMutationFixture.HistoricalPackageName, booking.TenGoiChot);
        Assert.Equal(new DateTime(2099, 1, 2, 8, 0, 0), booking.BatDau);
    }

    [SqlServerWriteFact]
    public async Task PackageChange_WithoutExplicitQuote_IsRejectedWithoutChangingSnapshot()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var input = fixture.BookingInput(new DateTime(2099, 1, 2, 8, 0, 0)) with { GoiChupId = fixture.ReplacementPackageId };
        var result = await fixture.Repository.UpdateBookingAsync(fixture.BookingId, input, "ordinary edit", fixture.Actor);

        Assert.False(result.Success);
        var booking = await fixture.Repository.GetBookingAsync(fixture.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(fixture.PackageId, booking.GoiChupId);
        Assert.Equal(SqlMutationFixture.HistoricalPrice, booking.GiaGoiChot);
    }

    [SqlServerWriteFact]
    public async Task PackageChange_WithStaleCatalogQuote_IsRejectedWithoutChangingSnapshot()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var input = fixture.BookingInput(new DateTime(2099, 1, 2, 8, 0, 0)) with
        {
            GoiChupId = fixture.ReplacementPackageId,
            PackageQuote = new(fixture.PackageId, SqlMutationFixture.HistoricalPrice, 249m, "Customer accepted a stale price")
        };
        var result = await fixture.Repository.UpdateBookingAsync(fixture.BookingId, input, "explicit quote", fixture.Actor);

        Assert.False(result.Success);
        var booking = await fixture.Repository.GetBookingAsync(fixture.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(fixture.PackageId, booking.GoiChupId);
        Assert.Equal(SqlMutationFixture.HistoricalPrice, booking.GiaGoiChot);
    }

    [SqlServerWriteFact]
    public async Task PackageChange_WithAcceptedCurrentQuote_ChangesSnapshotAndCreatesDistinctAudit()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var input = fixture.BookingInput(new DateTime(2099, 1, 2, 8, 0, 0)) with
        {
            GoiChupId = fixture.ReplacementPackageId,
            PackageQuote = new(fixture.PackageId, SqlMutationFixture.HistoricalPrice, 250m, "Customer explicitly accepted the package change")
        };
        var result = await fixture.Repository.UpdateBookingAsync(fixture.BookingId, input, "explicit quote", fixture.Actor);

        Assert.True(result.Success, result.Code);
        var booking = await fixture.Repository.GetBookingAsync(fixture.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(fixture.ReplacementPackageId, booking.GoiChupId);
        Assert.Equal(250m, booking.GiaGoiChot);
        Assert.Equal(1, await fixture.CountAsync("package-audits"));
    }

    [SqlServerWriteFact]
    public async Task ConcurrentPayments_CannotExceedBookingTotalAndCommitMatchingAudit()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var input = new PaymentInput(fixture.BookingId, "DAT_COC", 80m, "P02 concurrency test");
        var results = await Task.WhenAll(
            fixture.Repository.AddPaymentAsync(input, fixture.Actor),
            fixture.Repository.AddPaymentAsync(input, fixture.Actor));

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => !result.Success);
        Assert.Equal(1, await fixture.CountAsync("payments"));
        Assert.Equal(1, await fixture.CountAsync("payment-audits"));
        var booking = await fixture.Repository.GetBookingAsync(fixture.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(80m, booking.DaThu);
        Assert.Equal(20m, booking.ConLai);
    }

    [SqlServerWriteFact]
    public async Task ConflictingConcurrentCreation_CommitsOnlyOneBookingAndItsAudit()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var input = fixture.BookingInput(new DateTime(2099, 1, 3, 8, 0, 0));
        async Task<bool> TryCreateAsync()
        {
            try { await fixture.Repository.CreateBookingAsync(input, fixture.Actor); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(TryCreateAsync(), TryCreateAsync());

        Assert.Single(results, success => success);
        Assert.Equal(2, await fixture.CountAsync("bookings")); // Fixture schedule + exactly one new schedule.
        Assert.Equal(1, await fixture.CountAsync("create-audits"));
    }

    [SqlServerWriteFact]
    public async Task LockedActor_IsRejectedByApplicationBeforePaymentPersistence()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        await fixture.LockActorAsync();
        var result = await fixture.Application.Finance.ReceiveAsync(new(fixture.BookingId, "DAT_COC", 10m, null), fixture.Actor);

        Assert.False(result.Success);
        Assert.Equal(0, await fixture.CountAsync("payments"));
        Assert.Equal(0, await fixture.CountAsync("payment-audits"));
    }

    [SqlServerWriteFact]
    public async Task PasswordCompareAndSwap_WithWrongExpectedHash_DoesNotAlterCredentials()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var hasher = new Pbkdf2PasswordHasher();
        var result = await fixture.Repository.ChangeOwnPasswordAsync(fixture.ActorId,
            hasher.Hash("A-different-expected-password-5"), hasher.Hash("P02-New-Test-Password-7!"));

        Assert.False(result.Success);
        var account = await fixture.Repository.GetAccountByIdAsync(fixture.ActorId);
        Assert.NotNull(account);
        Assert.True(hasher.Verify(SqlMutationFixture.InitialPassword, account.MatKhauHash));
    }

    [SqlServerWriteFact]
    public async Task ConcurrentPasswordChanges_OnlyOneExpectedHashCanCommit()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        var hasher = new Pbkdf2PasswordHasher();
        var first = hasher.Hash("Synthetic-New-Password-One-4!");
        var second = hasher.Hash("Synthetic-New-Password-Two-5!");
        var results = await Task.WhenAll(
            fixture.Repository.ChangeOwnPasswordAsync(fixture.ActorId, fixture.InitialHash, first),
            fixture.Repository.ChangeOwnPasswordAsync(fixture.ActorId, fixture.InitialHash, second));
        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => !result.Success);
        var account = await fixture.Repository.GetAccountByIdAsync(fixture.ActorId);
        Assert.NotNull(account);
        Assert.Equal(results[0].Success ? first : second, account.MatKhauHash);
        Assert.Equal(1, await fixture.CountAsync("password-audits"));
    }

    [SqlServerWriteFact]
    public async Task LockedActor_IsRejectedInsideRepositoryTransactionWithoutLedgerOrAuditWrites()
    {
        await using var fixture = await SqlMutationFixture.CreateAsync();
        await fixture.LockActorAsync();
        var result = await fixture.Repository.AddPaymentAsync(new(fixture.BookingId, "DAT_COC", 10m, null), fixture.Actor);
        Assert.False(result.Success);
        Assert.Equal(0, await fixture.CountAsync("payments"));
        Assert.Equal(0, await fixture.CountAsync("payment-audits"));
    }
}
