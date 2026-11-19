using StudioManager.Domain;
using StudioManager.WinForms.Controls;
using Xunit;

namespace StudioManager.WinForms.Tests;

public sealed class BookingSelectionTests
{
    [Fact]
    public void InitialLoadAndFocus_DoNotChooseTheFirstBooking() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Grid.Focus();
        System.Windows.Forms.Application.DoEvents();
        fixture.AssertNoTarget();
        foreach (var name in new[] { "CancelAsync", "AdvanceAsync" }) BookingUi.Await(BookingUi.InvokeTask(fixture.Page, name));
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "PaymentAsync", false));
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "PaymentAsync", true));
        Assert.Equal(0, fixture.Repository.BookingReads);
        Assert.Empty(fixture.Notifications);
    });

    [Fact]
    public void RefreshAndReordering_RetainTheDeliberateBigintBookingId() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        fixture.Repository.Order = [BookingUiFixture.B, BookingUiFixture.A];
        fixture.Reload();
        Assert.Equal(BookingUiFixture.B, fixture.Selected?.Id);
        Assert.Contains("TEST-B", BookingUi.Field<Label>(fixture.Page, "_inspCode").Text);
        Assert.True(BookingUi.Field<ModernButton>(fixture.Page, "_cancel").Enabled);
        Assert.True(BookingUi.Field<ModernButton>(fixture.Page, "_pay").Enabled);
        Assert.Single(fixture.Grid.SelectedRows.Cast<DataGridViewRow>());
    });

    [Fact]
    public void FilteringAndRemoval_ClearSelectionWithoutChoosingAReplacement() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        BookingUi.Field<SearchInput>(fixture.Page, "_search").Text = "TEST";
        fixture.Reload(); // Even if B is still visible, a new query starts with no action target.
        fixture.AssertNoTarget();
        fixture.Click(BookingUiFixture.B);
        fixture.Repository.Order = [BookingUiFixture.A];
        fixture.Reload();
        fixture.AssertNoTarget();
        Assert.Equal(BookingUiFixture.A, fixture.Grid.Rows[0].Cells["Id"].Value);
    });

    [Fact]
    public void ProgrammaticCurrencyChanges_DoNotAdoptAnotherCustomer() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        fixture.Grid.CurrentCell = fixture.Grid.Rows[0].Cells["Ma"];
        System.Windows.Forms.Application.DoEvents();
        fixture.AssertNoTarget();
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Empty(fixture.Repository.Cancellations);
        Assert.Empty(fixture.Notifications);
    });

    [Fact]
    public void ExplicitKeyboardNavigation_CanSelectABookingByItsId() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Grid.CurrentCell = fixture.Grid.Rows[1].Cells["Ma"];
        BookingUi.Invoke(fixture.Grid, "OnKeyUp", new KeyEventArgs(Keys.Down));
        Assert.Equal(BookingUiFixture.B, fixture.Selected?.Id);
    });

    [Fact]
    public void PendingRefresh_DisablesEverySelectedBookingAction() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var pending = new TaskCompletionSource<IReadOnlyList<LichChup>>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Repository.Search = _ => pending.Task;
        var load = BookingUi.InvokeTask(fixture.Page, "LoadAsync", true);
        Assert.Null(fixture.Selected);
        Assert.False(fixture.Grid.Enabled);
        foreach (var name in new[] { "_cancel", "_refund", "_pay", "_edit", "_progress", "_detail" })
            Assert.False(BookingUi.Field<ModernButton>(fixture.Page, name).Enabled);
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "CancelAsync"));
        Assert.Equal(0, fixture.Repository.BookingReads);
        pending.SetResult(fixture.Repository.Rows);
        BookingUi.Await(load);
        Assert.Equal(BookingUiFixture.B, fixture.Selected?.Id);
    });

    [Fact]
    public void OlderSearchCompletion_CannotReplaceTheNewerFilterResults() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        var old = new TaskCompletionSource<IReadOnlyList<LichChup>>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Repository.Search = _ => old.Task;
        var first = BookingUi.InvokeTask(fixture.Page, "LoadAsync", true);
        BookingUi.Field<SearchInput>(fixture.Page, "_search").Text = "TEST-A";
        fixture.Repository.Search = _ => Task.FromResult<IReadOnlyList<LichChup>>([fixture.Repository.Bookings[BookingUiFixture.A]]);
        BookingUi.Await(BookingUi.InvokeTask(fixture.Page, "LoadAsync", true));
        old.SetResult([fixture.Repository.Bookings[BookingUiFixture.B]]);
        BookingUi.Await(first);
        Assert.Single(fixture.Grid.Rows.Cast<DataGridViewRow>());
        Assert.Equal(BookingUiFixture.A, fixture.Grid.Rows[0].Cells["Id"].Value);
        fixture.AssertNoTarget();
    });

    [Fact]
    public void FailedRefresh_ClearsStaleRowsAndActionTargets() => BookingUi.Run(() =>
    {
        using var fixture = new BookingUiFixture();
        fixture.Click(BookingUiFixture.B);
        fixture.Repository.Search = _ => Task.FromException<IReadOnlyList<LichChup>>(new IOException("Offline read failure"));
        fixture.Reload();
        fixture.AssertNoTarget();
        Assert.Empty(fixture.Grid.Rows.Cast<DataGridViewRow>());
        Assert.Equal("Không thể thực hiện", Assert.Single(fixture.Notifications).Title);
    });
}
