using StudioManager.WinForms;
using StudioManager.WinForms.Controls;
namespace StudioManager.CustomerDesktop;

// Layout adapters around the application's existing rendering sources, not a separate theme.
internal static class PaymentUi
{
    internal static TableLayoutPanel Layout(Form form,string title,string subtitle)
    {
        form.AutoScaleDimensions=new SizeF(96,96);form.AutoScaleMode=AutoScaleMode.Dpi;
        form.BackColor=Theme.Background;form.ForeColor=Theme.Text;form.Font=Theme.Font();form.Padding=new Padding(20);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=Color.Transparent,Margin=Padding.Empty};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new PageHeader(title,subtitle){Dock=DockStyle.Fill,Margin=Padding.Empty},0,0);
        form.Controls.Add(root);return root;
    }
    internal static Control Summary(Label label)
    {
        label.Dock=DockStyle.Fill;label.Font=Theme.Font(9.2f);label.ForeColor=Theme.TextSecondary;
        label.BackColor=Color.Transparent;label.Padding=Padding.Empty;label.UseMnemonic=false;
        label.TextChanged+=(_,_)=>label.Parent?.Parent?.PerformLayout();
        return new SummaryCard(label);
    }
    private sealed class SummaryCard : CardPanel
    {
        private readonly Label _label;
        private bool _measuring;
        internal SummaryCard(Label label)
        {
            _label=label;Dock=DockStyle.Fill;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,0,0,10);Padding=new Padding(16,12,16,12);
            BackColor=Theme.SurfaceWarm;BorderColor=Theme.Border;AccentStripeColor=Theme.Primary;
            AccentStripeLeft=true;AccentStripeThickness=3;Controls.Add(label);
            label.FontChanged+=(_,_)=>PerformLayout();
            label.TextChanged+=(_,_)=>PerformLayout();
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if(_measuring || _label is null)return;
            _measuring=true;
            try
            {
                var required=Measure(Math.Max(240,Width));
                if(MinimumSize.Height!=required)MinimumSize=new Size(0,required);
            }
            finally{_measuring=false;}
        }
        private int Measure(int width)=>TextRenderer.MeasureText(string.IsNullOrEmpty(_label.Text)?" ":_label.Text,_label.Font,
            new Size(Math.Max(120,width-Padding.Horizontal),int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPadding).Height+Padding.Vertical+4;
        public override Size GetPreferredSize(Size proposedSize)
        {
            var width=proposedSize.Width>0?proposedSize.Width:Math.Max(240,Width);
            return new Size(width,Measure(width));
        }
    }
    internal static CardPanel Data(Control content)
    {
        var card=new CardPanel{Dock=DockStyle.Fill,Padding=new Padding(1),Margin=Padding.Empty,BackColor=Theme.Surface,BorderColor=Theme.Border};
        content.Dock=DockStyle.Fill;card.Controls.Add(content);return card;
    }
    internal static FlowLayoutPanel Actions()=>new()
    {
        Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,
        BackColor=Color.Transparent,Padding=Padding.Empty,Margin=new Padding(0,10,0,0)
    };
    internal static ModernButton Action(FlowLayoutPanel bar,string text,IconKind icon,bool primary=false)
    {
        var button=primary?Theme.Button(text,Theme.Primary,icon):Theme.OutlineButton(text,Theme.BorderStrong,icon);
        button.Margin=new Padding(0,4,8,4);bar.Controls.Add(button);return button;
    }
    internal static void Warn(IWin32Window owner,string text)
    {using var dialog=new AtelierDialog(AtelierNotificationKind.Warning,"StudioManager — Atelier Lumière",text);dialog.ShowDialog(owner);}
}
