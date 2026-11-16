using System.Runtime.Versioning;
using StudioManager.Application;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using Xunit;
namespace StudioManager.Tests;

public sealed class DemoCustomerPaymentTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task RealP03P04PipeFlow_ApprovesBooking_ReadsPaymentAndReceipt_DeniesBStaffAndReassignment()
    {
        var f=await BookingRequestFixture.Create();
        await using var a=new LocalIdentityPipe(f.Identity,bookingRepository:f);
        await using var b=new LocalIdentityPipe(f.Identity,bookingRepository:f);
        await using var staff=new LocalIdentityPipe(f.Identity,bookingRepository:f);
        Assert.True((await a.SendAsync(new(IdentityCommand.Register){Identifier="registered@example.test",Password="CustomerPass1",Confirmation="CustomerPass1",
            Contact=new("Khách đăng ký","0900000000","registered@example.test","Địa chỉ thử")})).Success);
        Assert.True((await a.SendAsync(new(IdentityCommand.CustomerLogin){Identifier="registered@example.test",Password="CustomerPass1"})).Success);
        Assert.True((await a.SendAsync(new(IdentityCommand.UpdateMyProfile){Contact=new("Khách đã cập nhật","0900000000","registered@example.test","Địa chỉ mới")})).Success);
        var fresh=f.Identity.Connection();Assert.True((await fresh.LoginAsync("registered@example.test","CustomerPass1")).Success);
        Assert.Equal("Địa chỉ mới",(await fresh.GetMyProfileAsync()).Data!.Contact.Address);
        Assert.True((await b.SendAsync(new(IdentityCommand.CustomerLogin){Identifier="b@example.test",Password="CustomerPass1"})).Success);
        Assert.True((await staff.SendAsync(new(IdentityCommand.StaffLogin){Identifier="staff",Password="CustomerPass1"})).Success);
        var draft=f.Draft();
        Assert.True((await a.SendAsync(new(IdentityCommand.PreviewBookingRequest){BookingDraft=new(draft.SubmissionKey,draft.PackageId,draft.StartUtc,draft.PreferredRoomId,draft.Notes,[new(1,1.25m)])})).Success);
        var submitted=await a.SendAsync(new(IdentityCommand.SubmitBookingRequest){BookingSubmissionKey=draft.SubmissionKey});
        var id=submitted.CreatedRequestId!.Value;
        Assert.False((await a.SendAsync(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=id})).Success);
        Assert.True((await staff.SendAsync(new(IdentityCommand.ApproveBookingRequest){BookingRequestId=id,BookingApproval=new(1,1,f.Requests[id].Quote.Estimate),Reason="Đủ điều kiện thử nghiệm"})).Success);
        var(app,store)=await DemoPaymentTests.Fixture(f.Requests[id].Quote.Estimate);
        var posted=await app.Finance.CollectAsync(DemoPaymentTests.Input("BANK_TRANSFER_DEMO"),app.Session!);Assert.True(posted.Success);
        var bookingId=f.Requests[id].OfficialBookingId!.Value;f.Receipts[bookingId]=[posted.Data!];
        var summary=await a.SendAsync(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=id,CustomerId=2});
        Assert.True(summary.Success);Assert.Equal(200m,summary.Payments!.Collected);Assert.Equal("BANK_TRANSFER_DEMO",Assert.Single(summary.Payments.Items).Method);
        var receipt=await a.SendAsync(new(IdentityCommand.GetMyBookingReceipt){BookingRequestId=id,PaymentId=posted.Data!.Id});
        Assert.True(receipt.Success);Assert.Equal(posted.Data.Code,receipt.Receipt!.Code);
        Assert.False((await b.SendAsync(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=id})).Success);
        Assert.False((await b.SendAsync(new(IdentityCommand.GetMyBookingReceipt){BookingRequestId=id,PaymentId=posted.Data.Id})).Success);
        Assert.False((await staff.SendAsync(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=id})).Success);
        Assert.False((await a.SendAsync(new(IdentityCommand.GetMyBookingReceipt){BookingRequestId=id,PaymentId=9999})).Success);
        f.Bookings[bookingId]=f.Bookings[bookingId] with{Customer=9999};
        Assert.False((await a.SendAsync(new(IdentityCommand.GetMyBookingPayments){BookingRequestId=id})).Success);
        Assert.False((await a.SendAsync(new(IdentityCommand.GetMyBookingReceipt){BookingRequestId=id,PaymentId=posted.Data.Id})).Success);
        Assert.True((await a.SendAsync(new(IdentityCommand.GetMyBookingRequest){BookingRequestId=id})).Success);
        Assert.Single(store.Rows);
    }

    [Fact]
    public void MigrationAndCustomerReadSqlPreserveSingleLedgerAndOwnership()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory is not null && !File.Exists(Path.Combine(directory.FullName,"StudioManager.sln")))directory=directory.Parent;
        var root=directory!.FullName;
        var migration=File.ReadAllText(Path.Combine(root,"database","08_DemoPaymentReceipts_REVIEW_ONLY.sql"));
        Assert.Contains("@OwnerApprovedExecution bit=0",migration);Assert.Contains("@ExpectedDatabaseName sysname=NULL",migration);
        Assert.DoesNotMatch(@"(?im)^\s*(USE|UPDATE|DELETE|DROP|GRANT|CREATE TABLE)\b",migration);
        Assert.Contains("ALTER TABLE dbo.ThanhToan ADD",migration);Assert.Contains("WHERE YeuCauThuId IS NOT NULL",migration);
        var read=File.ReadAllText(Path.Combine(root,"src","StudioManager.Infrastructure","SqlBookingRequestRepository.Payments.cs"));
        Assert.Contains("r.TaiKhoanKhachHangId=@Owner AND r.KhachHangId=l.KhachHangId",read);
        var payment=File.ReadAllText(Path.Combine(root,"src","StudioManager.Infrastructure","SqlStudioRepository.Payments.cs"));
        Assert.Contains("snapshot.CustomerId!=r.GetInt64(14)",payment);Assert.Contains("amount.Precision=18;amount.Scale=2",payment);
        Assert.Contains("IsolationLevel.Serializable",payment);Assert.Contains("SCOPE_IDENTITY()",payment);
    }
}
