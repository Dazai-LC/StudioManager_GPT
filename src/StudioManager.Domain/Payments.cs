namespace StudioManager.Domain;

public static class PaymentMethods
{
    public const string Cash = "CASH";
    public const string BankTransferDemo = "BANK_TRANSFER_DEMO";
    public static bool IsSupported(string? value) => value is Cash or BankTransferDemo;
}
public sealed record ReceiptServiceLine(string Name, string Unit, decimal UnitPrice, decimal Quantity);
// Written from database values inside the collection transaction, never accepted from a client.
public sealed record PaymentReceiptSnapshot(long CustomerId, string CustomerName, string Phone, string BookingCode,
    string PackageName, decimal PackagePrice, IReadOnlyList<ReceiptServiceLine> Services, decimal Discount,
    decimal BookingTotal, decimal BalanceAfter, string StaffName);
public sealed record PaymentReceipt(long Id, string Code, long BookingId, string BookingCode, string CustomerName,
    string Phone, string PackageName, decimal? PackagePrice, IReadOnlyList<ReceiptServiceLine> Services,
    decimal? Discount, decimal? BookingTotal, decimal Amount, string? Method, string Purpose, DateTime PaidAt,
    string StaffName, decimal? BalanceAfter, bool HasSnapshot, string? Note);
public sealed record PaymentTransaction(long Id, string Code, string Purpose, decimal Amount, string? Method,
    DateTime PaidAt, bool CanViewReceipt);
public sealed record BookingPaymentSummary(long BookingId, string BookingCode, string Status, decimal Total,
    decimal Collected, decimal Refunded, decimal Remaining, IReadOnlyList<PaymentTransaction> Payments, long? NextBeforeId);
