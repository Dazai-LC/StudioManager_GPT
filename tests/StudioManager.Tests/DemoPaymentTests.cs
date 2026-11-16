using System.Reflection;
using System.Text.Json;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.CustomerApi;
using StudioManager.Domain;
using Xunit;

namespace StudioManager.Tests;

// Application/transport regressions with test doubles. These do NOT establish live SQL acceptance.
public sealed class DemoPaymentTests
{
    internal static async Task<(AppFacade App,FinanceDouble Store)> Fixture(decimal total=1000m)
    {
        var repository=DispatchProxy.Create<IStudioRepository,FinanceDouble>();var store=(FinanceDouble)(object)repository;store.Total=total;
        store.Accounts[1]=new(1,null,"cashier",InternalSecurityRegressionTests.TestHasher.Encode("CashierPass1"),VaiTro.NhanVien,true,false,"Nhân viên thử");
        var app=new AppFacade(repository,new InternalSecurityRegressionTests.TestHasher(),new InternalSecurityRegressionTests.MutableClock());
        Assert.True((await app.Auth.LoginAsync("cashier","CashierPass1")).Success);return(app,store);
    }
    internal static PaymentInput Input(string method="CASH",string purpose="DAT_COC",decimal amount=200m,Guid? key=null)
        =>new(1,purpose,amount,"Kiểm thử mô phỏng"){PhuongThucThu=method,YeuCauThuId=key??Guid.NewGuid()};

    [Theory]
    [InlineData("CASH","DAT_COC")]
    [InlineData("CASH","BO_SUNG")]
    [InlineData("CASH","CON_LAI")]
    [InlineData("BANK_TRANSFER_DEMO","DAT_COC")]
    [InlineData("BANK_TRANSFER_DEMO","BO_SUNG")]
    [InlineData("BANK_TRANSFER_DEMO","CON_LAI")]
    public async Task Collection_PreservesMethodPurposeAndActualStoredIdentity(string method,string purpose)
    {
        var(app,store)=await Fixture();var result=await app.Finance.CollectAsync(Input(method,purpose),app.Session!);
        Assert.True(result.Success);var saved=Assert.Single(store.Rows.Values);
        Assert.Equal(saved.Id,result.Data!.Id);Assert.Equal(saved.Code,result.Data.Code);
        Assert.Equal(method,saved.Method);Assert.Equal(purpose,saved.Purpose);Assert.Equal(800m,saved.BalanceAfter);
        var reloaded=await app.Finance.ReceiptAsync(1,saved.Id);
        Assert.Equal(saved,reloaded.Data);
        var serialized=JsonSerializer.Deserialize<PaymentReceipt>(JsonSerializer.Serialize(saved));
        Assert.Equal(method,serialized!.Method);Assert.Equal(saved.Code,serialized.Code);
    }
    [Fact]
    public async Task DepositPartialAndRemaining_RejectOvercollectionWithoutAddingRows()
    {
        var(app,store)=await Fixture();
        Assert.True((await app.Finance.CollectAsync(Input(amount:200m),app.Session!)).Success);
        Assert.True((await app.Finance.CollectAsync(Input(purpose:"BO_SUNG",amount:100.01m),app.Session!)).Success);
        var remaining=(await app.Finance.PaymentsAsync(1)).Data!;
        Assert.Equal(699.99m,remaining.Remaining);
        Assert.Equal("LIMIT",(await app.Finance.CollectAsync(Input(amount:700m),app.Session!)).Code);
        Assert.Equal(2,store.Rows.Count);
        Assert.True((await app.Finance.CollectAsync(Input("BANK_TRANSFER_DEMO","CON_LAI",699.99m),app.Session!)).Success);
        Assert.Equal(0,(await app.Finance.PaymentsAsync(1)).Data!.Remaining);
        Assert.Equal(3,store.Rows.Count);
    }
    [Fact]
    public async Task SameConfirmedActionKey_ReturnsSameReceipt_NotAnotherPayment()
    {
        var(app,store)=await Fixture();var input=Input();
        var first=await app.Finance.CollectAsync(input,app.Session!);var again=await app.Finance.CollectAsync(input,app.Session!);
        Assert.True(first.Success && again.Success);Assert.Equal(first.Data!.Id,again.Data!.Id);Assert.Single(store.Rows);
        Assert.Equal("POSTING_KEY_REUSED",(await app.Finance.CollectAsync(input with{SoTien=201m},app.Session!)).Code);
        Assert.Single(store.Rows);
    }
    [Fact]
    public async Task DoubleClickWhilePosting_IsRefusedBeforeSecondRepositoryWrite()
    {
        var(app,store)=await Fixture();store.Hold=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first=app.Finance.CollectAsync(Input(),app.Session!);await store.Started.Task;
        Assert.Equal("BUSY",(await app.Finance.CollectAsync(Input(),app.Session!)).Code);
        Assert.Equal(1,store.Calls);store.Hold.SetResult();Assert.True((await first).Success);Assert.Single(store.Rows);
    }
    [Fact]
    public async Task LostAcknowledgement_RequiresAuthoritativeReload_AndNeverAutomaticallyRetries()
    {
        var(app,store)=await Fixture();store.LoseAcknowledgement=true;
        var result=await app.Finance.CollectAsync(Input(),app.Session!);
        Assert.Equal("OUTCOME_UNKNOWN",result.Code);Assert.DoesNotContain("sensitive-sentinel",result.Message);
        Assert.Single(store.Rows);
        Assert.Equal("RELOAD_REQUIRED",(await app.Finance.CollectAsync(Input(),app.Session!)).Code);Assert.Equal(1,store.Calls);
        Assert.Equal(200m,(await app.Finance.PaymentsAsync(1)).Data!.Collected);
        var saved=Assert.Single(store.Rows.Values);Assert.Equal(saved.Code,(await app.Finance.ReceiptAsync(1,saved.Id)).Data!.Code);
        Assert.Single(store.Rows); // Reload/receipt retrieval did not post the uncertain write again.
    }
    [Fact]
    public async Task ForgedOrSignedOutSessionCannotCollectOrReadReceipts()
    {
        var(app,store)=await Fixture();
        Assert.Equal("SESSION_INVALID",(await app.Finance.CollectAsync(Input(),new UserSession(1,"cashier","Fake",VaiTro.QuanTriVien))).Code);
        app.Auth.SignOut();Assert.Equal("SESSION_INVALID",(await app.Finance.PaymentsAsync(1)).Code);
        Assert.Equal("SESSION_INVALID",(await app.Finance.ReceiptAsync(1,1)).Code);Assert.Empty(store.Rows);Assert.Equal(0,store.Calls);
    }
    [Theory]
    [InlineData("REAL_BANK")]
    [InlineData("")]
    public async Task UnapprovedMethodIsRejectedBeforeRepository(string method)
    {
        var result=await new FinanceService(null!).CollectAsync(Input(method),null!);Assert.Equal("INVALID_METHOD",result.Code);
    }
    [Theory]
    [InlineData("0.001")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("1e2")]
    [InlineData("1,234.56")]
    [InlineData("1 000")]
    [InlineData("10000000000000000")]
    public void RawAmountNeverSilentlyRoundsOrParsesGrouping(string value)=>Assert.False(PaymentPolicy.TryReadAmount(value,out _));
    [Theory]
    [InlineData("0.01",0.01)]
    [InlineData("123,45",123.45)]
    [InlineData("123.45",123.45)]
    public void ExactCentInputIsAccepted(string value,double expected)
    {Assert.True(PaymentPolicy.TryReadAmount(value,out var actual));Assert.Equal((decimal)expected,actual);}

    [Fact]
    public void ReceiptReprintUsesStoredIdentityDiscountAndBalance_WithoutDemoRecalculation()
    {
        var receipt=new PaymentReceiptDto(73,"TT-PERSISTED-73","LC-TEST","Khách thử","0900000000","Gói chốt",100m,
            [new("Dịch vụ","bộ",123.44m,1.25m)],0.11m,254.19m,254.18m,"CASH","DAT_COC",new(2026,11,16,9,30,0),"Nhân viên thử",0.01m,true,null);
        var text=ReceiptText.Format(receipt);Assert.Contains("TT-PERSISTED-73",text);Assert.Contains("154,30 đ",text);
        Assert.Contains("254,19 đ",text);Assert.Contains("0,11 đ",text);Assert.Contains("0,01 đ",text);
        Assert.Equal(text,ReceiptText.Format(receipt));Assert.Contains("Không phải hóa đơn thuế",text);
        var historical=receipt with{HasSnapshot=false,PackagePrice=null,Services=[],Discount=null,BookingTotal=null,BalanceAfter=null,Method=null};
        var old=ReceiptText.Format(historical);Assert.Contains("chưa được lưu",old);Assert.DoesNotContain("Giảm giá:",old);
        Assert.Contains(receipt.Code,old);Assert.Contains("Chưa lưu phương thức",old);
    }
    [Fact]
    public void SnapshotArithmeticRejectsSubcentAndInconsistentTotals()
    {
        var s=new PaymentReceiptSnapshot(1,"Test","0900000000","LC","Gói",100m,[new("DV","bộ",123.44m,1.25m)],0.11m,254.19m,0.01m,"Staff");
        Assert.True(PaymentPolicy.ValidSnapshot(s));Assert.False(PaymentPolicy.ValidSnapshot(s with{BookingTotal=254.30m}));
        Assert.False(PaymentPolicy.ValidSnapshot(s with{Discount=0.001m}));
    }

    public class FinanceDouble : InternalSecurityRegressionTests.RepositoryDouble
    {
        public decimal Total=1000m;
        public int Calls;
        public bool LoseAcknowledgement;
        public TaskCompletionSource? Hold;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly Dictionary<Guid,PaymentReceipt> Rows=[];
        private readonly Dictionary<Guid,PaymentInput> _inputs=[];
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var a=args!;
            return method!.Name switch
            {
                "PostPaymentAsync"=>Post((PaymentInput)a[0]!),
                "GetPaymentSummaryAsync"=>Task.FromResult<BookingPaymentSummary?>(new(1,"LC-TEST","DA_DAT_LICH",Total,Rows.Values.Sum(p=>p.Amount),0,
                    Total-Rows.Values.Sum(p=>p.Amount),Rows.Values.Select(p=>new PaymentTransaction(p.Id,p.Code,p.Purpose,p.Amount,p.Method,p.PaidAt,true)).ToArray(),null)),
                "GetPaymentReceiptAsync"=>Task.FromResult(Rows.Values.SingleOrDefault(p=>p.BookingId==(long)a[0]! && p.Id==(long)a[1]!)),
                _=>base.Invoke(method,args)
            };
        }
        private async Task<Result<PaymentReceipt>> Post(PaymentInput input)
        {
            Calls++;Started.TrySetResult();if(Hold is not null)await Hold.Task;
            if(Rows.TryGetValue(input.YeuCauThuId,out var existing))return _inputs[input.YeuCauThuId]==input
                ?Result<PaymentReceipt>.Ok(existing):Result<PaymentReceipt>.Fail("POSTING_KEY_REUSED","");
            var validation=BookingMoneyPolicy.ValidateCollection(input.SoTien,Total-Rows.Values.Sum(p=>p.Amount));
            if(!validation.Success)return Result<PaymentReceipt>.Fail(validation.Code,validation.Message);
            var id=Rows.Count+1;var receipt=new PaymentReceipt(id,"TT-STORED-"+id,1,"LC-TEST","Khách thử","0900000000","Gói",Total,[],0,Total,input.SoTien,
                input.PhuongThucThu,input.LoaiThu,new(2026,11,16,9,30,0),"Nhân viên thử",Total-Rows.Values.Sum(p=>p.Amount)-input.SoTien,true,input.GhiChu);
            Rows[input.YeuCauThuId]=receipt;_inputs[input.YeuCauThuId]=input;
            if(LoseAcknowledgement)throw new IOException("sensitive-sentinel-must-not-leak");
            return Result<PaymentReceipt>.Ok(receipt);
        }
    }
}
