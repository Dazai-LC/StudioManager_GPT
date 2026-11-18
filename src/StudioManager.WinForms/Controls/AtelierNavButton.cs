using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace StudioManager.WinForms.Controls;

// Shared rendering extracted from the approved customer sidebar.
public sealed class AtelierNavButton : Panel
    {
        private readonly string _text;
        private readonly IconKind _icon;
        private bool _selected;
        private bool _hover;

        public bool IsSelected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public AtelierNavButton(string text, IconKind icon)
        {
            _text = text;
            Text = text; AccessibleName = text; AccessibleRole = AccessibleRole.PushButton; TabStop = true;
            _icon = icon;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            MouseEnter += (_, _) => { _hover = true; Invalidate(); };
            MouseLeave += (_, _) => { _hover = false; Invalidate(); };
            KeyDown += (_, e) => { if (e.KeyCode is Keys.Enter or Keys.Space) { e.SuppressKeyPress = true; OnClick(EventArgs.Empty); } };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Sidebar);

            var rect = new RectangleF(1, 1, Math.Max(2, Width - 2), Math.Max(2, Height - 2));
            using var path = Theme.RoundedRectangle(rect, 8f);
            if (_selected)
            {
                using var fill = new SolidBrush(Color.FromArgb(46, 72, 58));
                g.FillPath(fill, path);
                using var stripe = new SolidBrush(Theme.Primitives.Sage200);
                g.FillRectangle(stripe, 2, 8, 3, Height - 16);
            }
            else if (_hover)
            {
                using var fill = new SolidBrush(Theme.SidebarHover);
                g.FillPath(fill, path);
            }

            var iconCol = _selected ? Theme.Primitives.Sage200 : Theme.SidebarMuted;
            var txtCol = _selected ? Color.White : Theme.SidebarText;
            VectorIcons.Draw(g, _icon, new RectangleF(14, (Height - 16) / 2f, 16, 16), iconCol, 1.75f);
            using var font = Theme.Font(9.2f, _selected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, _text, font, new Rectangle(38, 0, Math.Max(40, Width - 44), Height), txtCol,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }
