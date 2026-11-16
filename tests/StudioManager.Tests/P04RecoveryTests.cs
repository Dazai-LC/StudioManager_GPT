using System.Reflection;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Domain;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.Tests;

public sealed class P04RecoveryTests
{
    [Theory]
    [InlineData("123.44", "1.25", "154.30")]
    [InlineData("0.01", "1", "0.01")]
    [InlineData("0.10", "0.10", "0.01")]
    [InlineData("0", "0.01", "0")]
    public void FractionalQuantities_AreAcceptedOnlyWithExactCentExtensions(string price, string quantity, string expected)
    {
        var result = BookingMoneyPolicy.LineTotal(Parse(price), Parse(quantity));
        Assert.True(result.Success); Assert.Equal(Parse(expected), result.Data);
    }

    [Theory]
    [InlineData("123.45", "1.25")]
    [InlineData("0.01", "0.49")]
    [InlineData("0.01", "0.50")]
    [InlineData("0.01", "0.51")]
    [InlineData("1.001", "1")]
    public void EitherSideOfRoundingBoundary_IsRejectedNeverRounded(string price, string quantity)
        => Assert.Equal("PRICE_PRECISION", BookingMoneyPolicy.LineTotal(Parse(price), Parse(quantity)).Code);

    [Fact]
    public void LedgerMaximumAndOverflow_AreRejectedWithoutChangingHistoricalFormula()
    {
        Assert.True(BookingMoneyPolicy.QuoteTotal(BookingMoneyPolicy.LedgerMaximum, []).Success);
        Assert.Equal("PRICE_LIMIT", BookingMoneyPolicy.QuoteTotal(BookingMoneyPolicy.LedgerMaximum,
            [new(1, "Test", "unit", 0.01m, 1)]).Code);
        Assert.Equal("PRICE_LIMIT", BookingMoneyPolicy.LineTotal(decimal.MaxValue, 1000).Code);
        Assert.Equal(154.3125m, BusinessRules.TamTinh(0, [(1.25m, 123.45m)])); // No historical rounding.
    }

    [Fact]
    public void DiscountRemainderAndCollection_AreExactAndCannotOvercollect()
    {
        var total = BusinessRules.TongThanhToan(BusinessRules.TamTinh(100m, [(1.25m, 123.44m)]), 0.11m);
        var booking = new LichChup(1, "TEST", 1, "Test", 1, "Test", 100m, DateTime.Today,
            DateTime.Today.AddHours(1), 1, "Test", 1, "Test", TrangThaiLich.DaDatLich, 0.11m, "Test", null, 154.30m, 254.18m);
        Assert.Equal(254.19m, total); Assert.Equal(0.01m, booking.ConLai);
        Assert.True(BookingMoneyPolicy.ValidateCollection(0.01m, booking.ConLai).Success);
        Assert.Equal("LIMIT", BookingMoneyPolicy.ValidateCollection(0.02m, booking.ConLai).Code);
        Assert.Equal("INVALID_PRECISION", BookingMoneyPolicy.ValidateCollection(0.001m, booking.ConLai).Code);
        Assert.Equal("LIMIT", BookingMoneyPolicy.ValidateCollection(0.01m, 0.005m).Code);
        Assert.Equal(0m, (booking with { DaThu = 254.19m }).ConLai);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10000000000000000")]
    public async Task NewFinancialWrites_RejectUnrepresentableInputsBeforeSqlOrAuthorizationReads(string value)
    {
        var amount = Parse(value); var admin = new UserSession(1, "test", "Test", VaiTro.QuanTriVien);
        Assert.Equal("INVALID_PRECISION", (await new FinanceService(null!).ReceiveAsync(new(1, "DAT_COC", amount, null), admin)).Code);
        Assert.Equal("INVALID_PRECISION", (await new FinanceService(null!).RefundAsync(new(1, amount, "Test reason", null), admin)).Code);
        var sql = new SqlStudioRepository(""); // Any SQL connection would fail; these guards must return before opening one.
        Assert.Equal("INVALID_PRECISION", (await sql.AddPaymentAsync(new(1, "DAT_COC", amount, null), admin)).Code);
        Assert.Equal("INVALID_PRECISION", (await sql.AddRefundAsync(new(1, amount, "Test reason", null), admin)).Code);
        Assert.Equal("INVALID_PRECISION", (await sql.SetDiscountAsync(1, amount, "Test reason", admin)).Code);
    }

    [Fact]
    public async Task UnsettleableQuote_CannotBePreviewedOrApprovedFromStoredSnapshot()
    {
        var f = await BookingRequestFixture.Create(); var c = f.Connection();
        await c.Identity.LoginAsync("a@example.test", "CustomerPass1");
        f.Services[1] = f.Services[1] with { Price = 123.45m };
        Assert.Equal("PRICE_PRECISION", (await c.Service.PreviewAsync(f.Draft())).Code);
        Assert.Empty(f.Requests);
        f.Services[1] = f.Services[1] with { Price = 123.44m };
        var draft = f.Draft(); await c.Service.PreviewAsync(draft);
        var id = (await c.Service.SubmitAsync(draft.SubmissionKey)).Data;
        var stored = f.Requests[id];
        f.Requests[id] = stored with { Quote = stored.Quote with { Addons = [stored.Quote.Addons[0] with { UnitPrice = 123.45m }] } };
        var s = f.Connection(); await s.Identity.StaffLoginAsync("staff", "CustomerPass1");
        Assert.Equal("PRICE_PRECISION", (await s.Service.ReviewAsync(id, true, new(1, 1, stored.Quote.Estimate, ""), "Kiểm tra phần lẻ")).Code);
        Assert.Empty(f.Bookings); Assert.Equal(BookingRequestState.Pending, f.Requests[id].State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyStaffEdit_PreservesOwnRequestHistory_AndCustomerContinuity(bool reassign)
    {
        var f = await BookingRequestFixture.Create(); var c = f.Connection(); var s = f.Connection();
        await c.Identity.LoginAsync("a@example.test", "CustomerPass1"); await s.Identity.StaffLoginAsync("staff", "CustomerPass1");
        var draft = f.Draft(); await c.Service.PreviewAsync(draft); var id = (await c.Service.SubmitAsync(draft.SubmissionKey)).Data;
        Assert.True((await s.Service.ReviewAsync(id, true, new(1, 1, f.Requests[id].Quote.Estimate, ""), "Duyệt yêu cầu hợp lệ")).Success);
        var repository = DispatchProxy.Create<IStudioRepository, LegacyBookingBridge>();
        var bridge = (LegacyBookingBridge)(object)repository; bridge.Fixture = f;
        var actor = f.Identity.Directory.Accounts.Values.First(a => a.TenDangNhap == "staff"); bridge.Accounts[actor.Id] = actor;
        var app = new AppFacade(repository, new Pbkdf2PasswordHasher(), f.Identity.Clock);
        Assert.True((await app.Auth.LoginAsync("staff", "CustomerPass1")).Success);
        var current = await app.Bookings.GetAsync(1); Assert.NotNull(current);
        var input = new BookingInput(reassign ? 41 : current.KhachHangId, current.GoiChupId, current.BatDau.AddDays(1),
            current.KetThuc.AddDays(1), current.NhiepAnhGiaId, current.PhongChupId, "Sửa từ nghiệp vụ nhân viên cũ");
        Assert.True((await app.Bookings.UpdateAsync(1, input, null, app.Session!)).Success);
        var own = await c.Service.OfficialAsync(false, id);
        Assert.Equal(!reassign, own.Success);
        if (reassign) Assert.Equal("NOT_FOUND", own.Code);
        else Assert.Equal(StudioBookingTime.FromLegacyLocal(input.BatDau), own.Data!.StartUtc);
        Assert.True((await s.Service.OfficialAsync(true, id)).Success);
        var history = await c.Service.GetAsync(false, id);
        Assert.True(history.Success); Assert.Equal(BookingRequestState.Approved, history.Data!.State);
        Assert.Equal(2, history.Data.History.Count); Assert.Equal(current.KhachHangId, f.ResolvedCustomers[id]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApprovalRacingPendingWithdrawal_HasExactlyOneTerminalOutcome_InFakeTransactionContract(bool cancelFirst)
    {
        var f = await BookingRequestFixture.Create(); var c = f.Connection(); var s = f.Connection();
        await c.Identity.LoginAsync("a@example.test", "CustomerPass1"); await s.Identity.StaffLoginAsync("staff", "CustomerPass1");
        var draft = f.Draft(); await c.Service.PreviewAsync(draft); var id = (await c.Service.SubmitAsync(draft.SubmissionKey)).Data;
        Task<Result> Approve() => s.Service.ReviewAsync(id, true, new(1, 1, f.Requests[id].Quote.Estimate, ""), "Duyệt đồng thời với rút");
        Task<Result> Cancel() => c.Service.CancelAsync(id, "Khách rút yêu cầu đồng thời");
        Task<Result>[] operations = cancelFirst ? [Cancel(), Approve()] : [Approve(), Cancel()];
        var results = await Task.WhenAll(operations);
        Assert.Single(results, r => r.Success); Assert.Single(results, r => r.Code == "STATE_CHANGED");
        var final = f.Requests[id]; Assert.Equal(2, final.History.Count);
        Assert.Equal(final.State == BookingRequestState.Approved ? 1 : 0, f.Bookings.Count);
        Assert.Equal(final.State == BookingRequestState.Approved ? 1 : 0, f.CreatedCustomers.Count);
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    public class LegacyBookingBridge : InternalSecurityRegressionTests.RepositoryDouble
    {
        internal BookingRequestFixture Fixture = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IStudioRepository.GetBookingAsync))
            {
                var b = Fixture.Bookings[(long)args![0]!];
                return Task.FromResult<LichChup?>(new(b.Id, "LC-TEST", b.Customer, "Test", b.Quote.Package.Id, b.Quote.Package.Name,
                    b.Quote.Package.Price, StudioBookingTime.ToLegacyLocal(b.Start), StudioBookingTime.ToLegacyLocal(b.End),
                    b.Photographer, "Test", b.Room, "Test", TrangThaiLich.DaDatLich, 0, null, null));
            }
            if (method.Name == nameof(IStudioRepository.UpdateBookingAsync))
            {
                var id = (long)args![0]!; var input = (BookingInput)args[1]!; var b = Fixture.Bookings[id];
                Fixture.Bookings[id] = b with { Customer = input.KhachHangId, Start = StudioBookingTime.FromLegacyLocal(input.BatDau),
                    End = StudioBookingTime.FromLegacyLocal(input.KetThuc), Room = input.PhongChupId, Photographer = input.NhiepAnhGiaId };
                return Task.FromResult(Result.Ok());
            }
            return base.Invoke(method, args);
        }
    }
}
