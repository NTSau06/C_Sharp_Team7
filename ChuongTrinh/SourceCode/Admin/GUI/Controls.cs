using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// Mã biểu tượng của font "Segoe MDL2 Assets" (có sẵn trên Windows 10/11) - dùng làm icon, không cần file ảnh.
    /// </summary>
    public static class Glyph
    {
        public const string Home = "";
        public const string List = "";
        public const string Bag = "";
        public const string Box = "";
        public const string Download = "";
        public const string Cart = "";
        public const string People = "";
        public const string User = "";
        public const string Tag = "";
        public const string Chart = "";
        public const string Lock = "";
        public const string SignOut = "";
        public const string Search = "";
        public const string Refresh = "";
        public const string Add = "";
        public const string Save = "";
        public const string Delete = "";
        public const string Edit = "";
        public const string Calendar = "";
        public const string Money = "";
        public const string Warning = "";
        public const string Eye = "";
        public const string Check = "";
        public const string Document = "";
        public const string Database = "";
        public const string Undo = "";
        public const string Cancel = "";
    }

    /// <summary>Nút phẳng bo góc, có icon, đổi màu khi rê chuột / nhấn. Outline = nút phụ nền trắng viền xám.</summary>
    public class FlatButton : Button
    {
        private bool hover;
        private bool down;

        public string Glyph { get; set; }
        public bool Outline { get; set; }
        public int Radius { get; set; }

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Radius = 8;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(UiHelper.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill, border, fore;
            if (Outline)
            {
                fill = down ? UiHelper.Line : hover ? UiHelper.PageBack : Color.White;
                border = hover ? UiHelper.Gray : UiHelper.Line;
                fore = Enabled ? UiHelper.TextColor : UiHelper.Muted;
            }
            else
            {
                Color baseColor = Enabled ? BackColor : UiHelper.Blend(BackColor, Color.White, 0.5);
                fill = !Enabled ? baseColor : down ? UiHelper.Darken(baseColor, 0.18) : hover ? UiHelper.Darken(baseColor, 0.09) : baseColor;
                border = fill;
                fore = ForeColor;
            }

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiHelper.RoundedRect(r, Radius))
            using (var b = new SolidBrush(fill))
            using (var p = new Pen(border))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            Size ts = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags);
            bool showIcon = !string.IsNullOrEmpty(Glyph) && ts.Width + 34 <= Width;
            int iconW = showIcon ? 22 : 0;
            int x = (Width - ts.Width - iconW) / 2;
            if (showIcon)
                TextRenderer.DrawText(g, Glyph, UiHelper.IconFont(10F), new Rectangle(x, 0, 18, Height), fore,
                    flags | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + iconW, 0, ts.Width + 4, Height), fore, flags);

            if (Focused && ShowFocusCues)
            {
                var fr = new Rectangle(2, 2, Width - 5, Height - 5);
                using (GraphicsPath path = UiHelper.RoundedRect(fr, Math.Max(2, Radius - 2)))
                using (var p = new Pen(Color.FromArgb(160, Outline ? UiHelper.Primary : Color.White)) { DashStyle = DashStyle.Dot })
                    g.DrawPath(p, path);
            }
        }
    }

    /// <summary>Mục menu bên trái: icon + chữ; mục đang chọn được tô sáng và có vạch nhấn màu vàng.</summary>
    public class SideMenuButton : Button
    {
        private bool hover;
        private bool selected;

        public string Glyph { get; set; }
        public Color NormalText { get; set; }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        public SideMenuButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            NormalText = Color.FromArgb(199, 210, 224);
            Font = new Font("Segoe UI", 10.5F);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(UiHelper.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (selected || hover)
            {
                Color fill = selected ? UiHelper.Primary : Color.FromArgb(38, 66, 96);
                using (GraphicsPath path = UiHelper.RoundedRect(new Rectangle(10, 3, Width - 21, Height - 7), 8))
                using (var b = new SolidBrush(fill))
                    g.FillPath(b, path);
            }
            if (selected)
            {
                using (GraphicsPath bar = UiHelper.RoundedRect(new Rectangle(10, Height / 2 - 10, 4, 20), 2))
                using (var b = new SolidBrush(UiHelper.Accent))
                    g.FillPath(b, bar);
            }

            Color fore = selected ? Color.White : (hover ? Color.White : NormalText);
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
                                          TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (!string.IsNullOrEmpty(Glyph))
                TextRenderer.DrawText(g, Glyph, UiHelper.IconFont(12F), new Rectangle(26, 0, 24, Height), fore,
                    flags | TextFormatFlags.HorizontalCenter);
            using (var f = new Font(Font, selected ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(g, Text, f, new Rectangle(60, 0, Width - 70, Height), fore, flags);
        }
    }

    /// <summary>Thẻ (card) nền trắng bo góc, viền mảnh - khung chứa nội dung.</summary>
    public class CardPanel : Panel
    {
        public int Radius { get; set; }
        public Color BorderColor { get; set; }

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
            Padding = new Padding(14);
            Radius = 10;
            BorderColor = UiHelper.Line;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(UiHelper.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UiHelper.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (var b = new SolidBrush(BackColor))
            using (var p = new Pen(BorderColor))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }
    }

    /// <summary>Ô nhập có icon bên trái, viền bo góc đổi màu khi đang nhập; tùy chọn nút ẩn/hiện mật khẩu.</summary>
    public class InputBox : Panel
    {
        private readonly Label icon;
        private readonly Label toggle;
        private bool focused;

        public TextBox TextBox { get; private set; }

        public InputBox(string glyph, TextBox textBox, bool passwordToggle = false)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
            Height = 46;
            Cursor = Cursors.IBeam;

            TextBox = textBox;
            TextBox.BorderStyle = BorderStyle.None;
            TextBox.BackColor = Color.White;
            TextBox.ForeColor = UiHelper.TextColor;
            TextBox.Font = new Font("Segoe UI", 11F);
            TextBox.Enter += (s, e) => { focused = true; icon.ForeColor = UiHelper.Primary; Invalidate(); };
            TextBox.Leave += (s, e) => { focused = false; icon.ForeColor = UiHelper.Muted; Invalidate(); };

            icon = new Label
            {
                Text = glyph,
                Font = UiHelper.IconFont(12F),
                ForeColor = UiHelper.Muted,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.IBeam
            };
            icon.Click += (s, e) => TextBox.Focus();
            Click += (s, e) => TextBox.Focus();
            Controls.Add(icon);
            Controls.Add(TextBox);

            if (passwordToggle)
            {
                toggle = new Label
                {
                    Text = Glyph.Eye,
                    Font = UiHelper.IconFont(12F),
                    ForeColor = UiHelper.Muted,
                    BackColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };
                new ToolTip().SetToolTip(toggle, "Hiện / ẩn mật khẩu");
                toggle.Click += (s, e) =>
                {
                    TextBox.UseSystemPasswordChar = !TextBox.UseSystemPasswordChar;
                    toggle.ForeColor = TextBox.UseSystemPasswordChar ? UiHelper.Muted : UiHelper.Primary;
                    TextBox.Focus();
                };
                Controls.Add(toggle);
            }
            LayoutInner();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        private void LayoutInner()
        {
            if (icon == null) return;
            icon.SetBounds(2, 2, 40, Height - 4);
            int right = toggle != null ? 42 : 12;
            if (toggle != null) toggle.SetBounds(Width - 42, 2, 40, Height - 4);
            TextBox.SetBounds(44, (Height - TextBox.PreferredHeight) / 2 + 1, Math.Max(10, Width - 44 - right), TextBox.PreferredHeight);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(UiHelper.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UiHelper.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var b = new SolidBrush(Color.White))
            using (var p = new Pen(focused ? UiHelper.Primary : UiHelper.Line, focused ? 1.6F : 1F))
            {
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }
    }

    /// <summary>Ô tròn chứa icon, nền là màu nhạt của màu chủ đề (dùng trong thẻ số liệu).</summary>
    public class IconBadge : Control
    {
        public string Glyph { get; set; }
        public Color Color { get; set; }

        public IconBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Size = new Size(46, 46);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(UiHelper.ParentBack(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Math.Min(Width, Height) - 1;
            using (var b = new SolidBrush(UiHelper.Blend(Color, Color.White, 0.86)))
                g.FillEllipse(b, 0, 0, d, d);
            TextRenderer.DrawText(g, Glyph, UiHelper.IconFont(14F), new Rectangle(0, 0, d, d), Color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
