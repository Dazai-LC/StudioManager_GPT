using StudioManager.Application;

namespace StudioManager.WinForms.Controls;

/// <summary>Keeps invalid raw text visible; NumericUpDown must not round/clamp it before save validation.</summary>
internal sealed class CatalogPriceInput : NumericUpDown
{
    internal CatalogPriceInput()
    {
        DecimalPlaces = 2;
        Maximum = BookingMoneyPolicy.LedgerMaximum;
        ThousandsSeparator = false;
    }

    internal Result<decimal> ReadAmount() => CatalogPricePolicy.Read(Text);

    protected override void ValidateEditText()
    {
        if (ReadAmount().Success) base.ValidateEditText();
    }

    public override void UpButton() { if (ReadAmount().Success) base.UpButton(); }
    public override void DownButton() { if (ReadAmount().Success) base.DownButton(); }
}
