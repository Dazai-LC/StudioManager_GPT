using StudioManager.Domain;
using StudioManager.WinForms.Controls;
using Xunit;

namespace StudioManager.WinForms.Tests;

public sealed class BookingActionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cancellation_IdentifiesTheExactBookingAndRequiresFinalConfirmation(bool approve) => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var booking = fixture.Repository.Bookings[BookingUiFixture.B];
        var confirmations = 0;
        fixture.Respond = request =>
        {
            if (request.Kind != AtelierNotificationKind.Confirmation) return DialogResult.OK;
            confirmations++;
            BookingUiFixture.AssertTarget(request, booking);
            Assert.True(request.IsDestructive);
            Assert.Equal("Hủy lịch này", request.PrimaryButtonText);
            Assert.Equal("Giữ nguyên", request.SecondaryButtonText);
            Assert.Contains("Lý do hủy: Lý do thử nghiệm", request.Message);
            return approve ? DialogResult.Yes : DialogResult.Cancel;
        };
        using var driver = Reason("Lý do thử nghiệm");
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Equal(1, driver.Handled);
        Assert.Equal(1, confirmations);
        if (approve) Assert.Equal((BookingUiFixture.B, "Lý do thử nghiệm"), Assert.Single(fixture.Repository.Cancellations));
        else Assert.Empty(fixture.Repository.Cancellations);
    });

    [Fact]
    public void CancellationReason_RemainsRequiredByTheActualPrompt() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        using var driver = new BookingDialogDriver("TextPrompt", dialog =>
        {
            dialog.AcceptButton!.PerformClick();
            Assert.Equal(DialogResult.None, dialog.DialogResult);
            Assert.Contains("Vui lòng nhập nội dung yêu cầu.", Assert.Single(fixture.Notifications).Message);
            dialog.Close();
        });
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Equal(1, driver.Handled);
        Assert.Empty(fixture.Repository.Cancellations);
        Assert.DoesNotContain(fixture.Notifications, request => request.Kind == AtelierNotificationKind.Confirmation);
    });

    [Theory]
    [InlineData("status")]
    [InlineData("customer")]
    [InlineData("time")]
    [InlineData("money")]
    [InlineData("missing")]
    public void StaleBooking_IsRejectedBeforeOpeningCancellationPrompt(string changed) => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        Change(fixture, changed);
        using var driver = new BookingDialogDriver("TextPrompt", dialog => dialog.Close());
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Equal(0, driver.Handled);
        Assert.Empty(fixture.Repository.Cancellations);
        fixture.AssertNoTarget();
        Assert.Contains(fixture.Notifications, request => request.Kind == AtelierNotificationKind.Warning);
    });

    [Theory]
    [InlineData("status")]
    [InlineData("time")]
    [InlineData("selection")]
    [InlineData("session")]
    public void ChangesDuringConfirmation_AbortCancellationInsteadOfRetargeting(string changed) => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var confirmations = 0;
        fixture.Respond = request =>
        {
            if (request.Kind != AtelierNotificationKind.Confirmation) return DialogResult.OK;
            confirmations++;
            BookingUiFixture.AssertTarget(request, fixture.Repository.Bookings[BookingUiFixture.B]);
            if (changed == "selection") fixture.Grid.CurrentCell = fixture.Grid.Rows[0].Cells["Ma"];
            else if (changed == "session") fixture.App.Auth.SignOut();
            else Change(fixture, changed);
            return DialogResult.Yes;
        };
        using var driver = Reason("Lý do thử nghiệm");
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Equal(1, confirmations);
        Assert.Empty(fixture.Repository.Cancellations);
        fixture.AssertNoTarget();
    });

    [Fact]
    public void PendingTargetRead_PreventsDuplicateAndCompetingBookingActions() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var pending = new TaskCompletionSource<LichChup?>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Repository.Read = _ => pending.Task;
        var first = BookingUi.InvokeTask(fixture.Page, "CancelAsync");
        Assert.False(BookingUi.Field<ModernButton>(fixture.Page, "_cancel").Enabled);
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "PaymentAsync", true));
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "AdvanceAsync"));
        Assert.Equal(1, fixture.Repository.BookingReads);
        using var driver = new BookingDialogDriver("TextPrompt", dialog => dialog.Close());
        pending.SetResult(fixture.Repository.Bookings[BookingUiFixture.B]);
        BookingUi.Await(first);
        Assert.Empty(fixture.Repository.Cancellations);
        Assert.Empty(fixture.Repository.Refunds);
        Assert.Empty(fixture.Repository.Advances);
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Refund_IdentifiesCustomerBookingAndAmountBeforeSubmitting(bool approve) => BookingUi.Run(() =>
    {
        using var fixture = RefundFixture();
        var booking = fixture.Repository.Bookings[BookingUiFixture.B];
        var confirmations = 0;
        fixture.Respond = request =>
        {
            if (request.Kind != AtelierNotificationKind.Confirmation) return DialogResult.OK;
            confirmations++;
            BookingUiFixture.AssertTarget(request, booking);
            Assert.True(request.IsDestructive);
            Assert.Contains("Số tiền hoàn: 20", request.Message);
            Assert.Contains("Lý do hoàn: Hoàn thử nghiệm", request.Message);
            return approve ? DialogResult.Yes : DialogResult.Cancel;
        };
        using var driver = RefundReason();
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "PaymentAsync", true));
        Assert.Equal(1, driver.Handled);
        Assert.Equal(1, confirmations);
        if (approve)
        {
            var refund = Assert.Single(fixture.Repository.Refunds);
            Assert.Equal(BookingUiFixture.B, refund.LichChupId);
            Assert.Equal(20, refund.SoTien);
            Assert.Equal("Hoàn thử nghiệm", refund.LyDo);
        }
        else Assert.Empty(fixture.Repository.Refunds);
    });

    [Fact]
    public void RefundBalanceChangedDuringConfirmation_DoesNotSendARefund() => BookingUi.Run(() =>
    {
        using var fixture = RefundFixture();
        fixture.Respond = request =>
        {
            if (request.Kind == AtelierNotificationKind.Confirmation) { Change(fixture, "refund"); return DialogResult.Yes; }
            return DialogResult.OK;
        };
        using var driver = RefundReason();
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "PaymentAsync", true));
        Assert.Equal(1, driver.Handled);
        Assert.Empty(fixture.Repository.Refunds);
        fixture.AssertNoTarget();
    });

    [Fact]
    public void StatusChangedDuringProgressConfirmation_DoesNotAdvanceADifferentStage() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var confirmations = 0;
        fixture.Respond = request =>
        {
            if (request.Kind != AtelierNotificationKind.Confirmation) return DialogResult.OK;
            confirmations++;
            BookingUiFixture.AssertTarget(request, fixture.Repository.Bookings[BookingUiFixture.B]);
            Change(fixture, "status");
            return DialogResult.Yes;
        };
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "AdvanceAsync"));
        Assert.Equal(1, confirmations);
        Assert.Empty(fixture.Repository.Advances);
        fixture.AssertNoTarget();
    });

    [Theory]
    [InlineData("unchanged")]
    [InlineData("balance")]
    [InlineData("booking")]
    [InlineData("session")]
    public void Collection_ConfirmsAFixedBookingAndRejectsStaleState(string changed) => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        var booking = fixture.Repository.Bookings[BookingUiFixture.B];
        using var payment = BookingUi.PaymentForm(fixture.App, BookingUiFixture.B);
        payment.Opacity = 0; payment.ShowInTaskbar = false; payment.Show();
        System.Windows.Forms.Application.DoEvents();
        Assert.Contains(booking.KhachHang, BookingUi.Field<Label>(payment, "_summary").Text);
        var confirmations = 0;
        fixture.Respond = request =>
        {
            if (request.Kind != AtelierNotificationKind.Confirmation) return DialogResult.OK;
            confirmations++;
            BookingUiFixture.AssertTarget(request, booking);
            if (changed == "balance") fixture.Repository.Balance = id => new(id, booking.Ma, booking.TrangThai.ToString(), 100, 20, 0, 80, [], null);
            else if (changed == "booking") Change(fixture, "status");
            else if (changed == "session") fixture.App.Auth.SignOut();
            return DialogResult.Yes;
        };
        using var driver = new BookingDialogDriver("CollectionDialog", dialog =>
        {
            BookingUi.Field<TextBox>(dialog, "_amount").Text = "20.00";
            dialog.AcceptButton!.PerformClick();
        });
        BookingUi.Await(BookingUi.InvokeTask(payment, "Run", (Func<Task>)(() => BookingUi.InvokeTask(payment, "Collect"))));
        Assert.Equal(1, driver.Handled);
        Assert.Equal(1, confirmations);
        if (changed == "unchanged")
        {
            var input = Assert.Single(fixture.Repository.Payments);
            Assert.Equal(BookingUiFixture.B, input.LichChupId);
            Assert.Equal(20, input.SoTien);
            Assert.NotEqual(Guid.Empty, input.YeuCauThuId);
        }
        else Assert.Empty(fixture.Repository.Payments);
    });

    private static BookingDialogDriver Reason(string reason) => new("TextPrompt", dialog =>
    {
        BookingUi.Field<TextBox>(dialog, "_text").Text = reason;
        dialog.AcceptButton!.PerformClick();
    });

    private static BookingDialogDriver RefundReason() => new("MoneyDialog", dialog =>
    {
        BookingUi.Field<NumericUpDown>(dialog, "_amount").Value = 20;
        BookingUi.Field<TextBox>(dialog, "_note").Text = "Hoàn thử nghiệm";
        dialog.AcceptButton!.PerformClick();
    });

    private static BookingUiFixture RefundFixture()
    {
        var fixture = new BookingUiFixture();
        var booking = fixture.Repository.Bookings[BookingUiFixture.B];
        fixture.Repository.Bookings[BookingUiFixture.B] = booking with { TrangThai = TrangThaiLich.DaHuy, DaThu = 50 };
        fixture.Reload(); fixture.Click(BookingUiFixture.B);
        Assert.True(BookingUi.Field<ModernButton>(fixture.Page, "_refund").Enabled);
        return fixture;
    }

    private static void Change(BookingUiFixture fixture, string changed)
    {
        var booking = fixture.Repository.Bookings[BookingUiFixture.B];
        if (changed == "missing") { fixture.Repository.Bookings.Remove(booking.Id); return; }
        fixture.Repository.Bookings[booking.Id] = changed switch
        {
            "status" => booking with { TrangThai = TrangThaiLich.DaChup }, // Admin could still cancel it; the UI must reject the stale confirmation.
            "customer" => booking with { KhachHangId = booking.KhachHangId + 1, KhachHang = "Khách khác thử nghiệm" },
            "time" => booking with { BatDau = booking.BatDau.AddHours(1), KetThuc = booking.KetThuc.AddHours(1) },
            "money" => booking with { DaThu = 20 },
            "refund" => booking with { DaHoan = 10 },
            _ => throw new ArgumentException("Unknown fixture change")
        };
    }
}
