using StudioManager.Domain;

namespace StudioManager.Application;

/// <summary>
/// Owner-approved P04 settlement boundary: exact cents, never round a persisted amount.
/// Existing stored values/formulas are not rewritten. Round is used only to detect loss of precision.
/// </summary>
public static class BookingMoneyPolicy
{
    public const decimal LedgerMaximum = 9_999_999_999_999_999.99m; // decimal(18,2)
    public static bool IsExactCent(decimal amount) => decimal.Round(amount, 2) == amount;
    public static bool FitsLedger(decimal amount) => amount >= 0 && amount <= LedgerMaximum && IsExactCent(amount);

    public static Result ValidateCollection(decimal amount, decimal remaining)
        => amount <= 0 || !FitsLedger(amount)
            ? Result.Fail("INVALID_PRECISION", "Khoản thu phải dương, chính xác đến 0,01 đ và trong giới hạn sổ thu.")
            : amount > remaining ? Result.Fail("LIMIT", $"Số tiền tối đa có thể thu là {remaining:#,0.####} đ.") : Result.Ok();

    public static Result<decimal> LineTotal(decimal unitPrice, decimal quantity)
    {
        if (quantity <= 0 || quantity > 1000 || decimal.Round(quantity, 2) != quantity)
            return BookingRequestErrors.Fail<decimal>("INVALID_QUANTITY");
        if (unitPrice < 0 || unitPrice > LedgerMaximum) return BookingRequestErrors.Fail<decimal>("PRICE_LIMIT");
        try
        {
            var total = checked(unitPrice * quantity);
            if (total > LedgerMaximum) return BookingRequestErrors.Fail<decimal>("PRICE_LIMIT");
            return !IsExactCent(unitPrice) || !IsExactCent(total)
                ? BookingRequestErrors.Fail<decimal>("PRICE_PRECISION") : Result<decimal>.Ok(total);
        }
        catch (OverflowException) { return BookingRequestErrors.Fail<decimal>("PRICE_LIMIT"); }
    }

    public static Result<decimal> QuoteTotal(decimal packagePrice, IReadOnlyList<BookingAddonSnapshot> addons)
    {
        if (packagePrice < 0 || packagePrice > LedgerMaximum) return BookingRequestErrors.Fail<decimal>("PRICE_LIMIT");
        if (!IsExactCent(packagePrice)) return BookingRequestErrors.Fail<decimal>("PRICE_PRECISION");
        var total = packagePrice;
        foreach (var addon in addons)
        {
            var line = LineTotal(addon.UnitPrice, addon.Quantity);
            if (!line.Success) return line;
            // Each line already fits; this subtraction avoids overflow without truncation.
            if (line.Data > LedgerMaximum - total) return BookingRequestErrors.Fail<decimal>("PRICE_LIMIT");
            total += line.Data;
        }
        return Result<decimal>.Ok(total);
    }
}
