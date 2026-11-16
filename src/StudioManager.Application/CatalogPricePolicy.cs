using System.Globalization;

namespace StudioManager.Application;

/// <summary>Validates the original catalog input before SQL decimal metadata can narrow it.</summary>
public static class CatalogPricePolicy
{
    public static string? PriceColumn(string entity) => entity switch
    {
        "GoiChup" => "GiaGoi", "DichVu" => "DonGia", _ => null
    };

    public static Result Validate(string entity, IReadOnlyDictionary<string, object?> values)
    {
        var column = PriceColumn(entity);
        // A partial edit without a price keeps the existing price; SQL still requires a price on creation.
        return column is not null && values.TryGetValue(column, out var value) && !Read(value).Success
            ? Invalid() : Result.Ok();
    }

    public static Result<decimal> Read(object? value)
    {
        decimal amount;
        switch (value)
        {
            case decimal number: amount = number; break;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                amount = Convert.ToDecimal(value, CultureInfo.InvariantCulture); break;
            case string text:
                // Retain text callers, but reject excess fractional information BEFORE decimal.TryParse,
                // which can round a long input. No thousands separators, exponents or binary floats.
                text = text.Trim();
                var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var unsigned = text.StartsWith('+') || text.StartsWith('-') ? text[1..] : text;
                var parts = unsigned.Split(separator, StringSplitOptions.None);
                if (parts.Length is < 1 or > 2 || parts[0].Length == 0 ||
                    parts.Any(p => p.Any(c => !char.IsAsciiDigit(c))) ||
                    (parts.Length == 2 && parts[1].Skip(2).Any(c => c != '0')) ||
                    !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.CurrentCulture, out amount)) return InvalidAmount();
                break;
            default: return InvalidAmount();
        }
        return BookingMoneyPolicy.FitsLedger(amount) ? Result<decimal>.Ok(amount) : InvalidAmount();
    }

    private static Result Invalid() => Result.Fail("INVALID_PRICE", Message);
    private static Result<decimal> InvalidAmount() => Result<decimal>.Fail("INVALID_PRICE", Message);
    private const string Message = "Giá phải không âm, chính xác đến 0,01 đ và trong giới hạn lưu trữ; không tự làm tròn.";
}
