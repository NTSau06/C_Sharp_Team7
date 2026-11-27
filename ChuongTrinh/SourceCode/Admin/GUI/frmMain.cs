using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// Màn hình chính (D2): menu chức năng bên trái có icon và chia nhóm, thanh tiêu đề trên,
    /// vùng nội dung ở giữa.
    /// </summary>
    public class frmMain : Form
    {
        private readonly Panel pnlContent;
        private readonly Panel pnlNavHost;
        private readonly FlowLayoutPanel flowMenu;
        private readonly IMessageFilter menuWheelFilter;
        private readonly Label lblPage;
        private readonly Label lblBreadcrumb;
        private readonly List<SideMenuButton> menuButtons = new List<SideMenuButton>();
        private Form currentChild;
        private int menuScrollY;

        public bool DaDangXuat { get; private set; }

        public frmMain()
        {
            Text = "SportZone - Phân hệ Quản trị";
            Icon = UiHelper.AppIcon;
            Font = UiHelper.BaseFont;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1360, 820);
            MinimumSize = new Size(1200, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = UiHelper.PageBack;

            // ----- menu trái (ẩn thanh cuộn hệ thống, vẫn cuộn mượt bằng con lăn chuột)
            var pnlMenu = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = UiHelper.MenuBack };
            var pnlLogo = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = UiHelper.MenuBack };
            pnlLogo.Paint += PaintLogo;

            pnlNavHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiHelper.MenuBack
            };
            flowMenu = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Width = 250,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 8),
                AutoScroll = false,
                BackColor = UiHelper.MenuBack
            };
            pnlNavHost.Controls.Add(flowMenu);
            pnlNavHost.Resize += (s, e) => ClampMenuScroll();

            AddSection(flowMenu, "TỔNG QUAN");
            AddMenu(flowMenu, "Trang chủ", Glyph.Home, () => new frmTrangChu(this));
            AddSection(flowMenu, "HÀNG HÓA");
            AddMenu(flowMenu, "Quản lý danh mục", Glyph.List, () => new frmDanhMuc());
            AddMenu(flowMenu, "Quản lý sản phẩm", Glyph.Bag, () => new frmSanPham());
            AddMenu(flowMenu, "Quản lý tồn kho", Glyph.Box, () => new frmTonKho());
            AddMenu(flowMenu, "Nhập kho", Glyph.Download, () => new frmNhapKho(0, true));
            AddSection(flowMenu, "BÁN HÀNG");
            AddMenu(flowMenu, "Quản lý đơn hàng", Glyph.Cart, () => new frmDonHang());
            AddMenu(flowMenu, "Tài khoản khách hàng", Glyph.People, () => new frmKhachHang());
            AddMenu(flowMenu, "Mã giảm giá", Glyph.Tag, () => new frmMaGiamGia());
            AddSection(flowMenu, "BÁO CÁO");
            AddMenu(flowMenu, "Thống kê doanh thu", Glyph.Chart, () => new frmThongKe());

            // ----- khu vực người dùng cuối menu
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 160, BackColor = UiHelper.MenuBack };
            var pnlUser = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UiHelper.MenuBack };
            pnlUser.Paint += PaintUser;
            SideMenuButton btnDoiMK = MenuButton("Đổi mật khẩu", Glyph.Lock);
            btnDoiMK.Click += (s, e) => { using (var f = new frmDoiMatKhau()) f.ShowDialog(this); };
            SideMenuButton btnLogout = MenuButton("Đăng xuất", Glyph.SignOut);
            btnLogout.NormalText = Color.FromArgb(252, 165, 165);
            btnLogout.Click += (s, e) =>
            {
                if (!UiHelper.Confirm(this, "Bạn muốn đăng xuất?")) return;
                DaDangXuat = true;
                Close();
            };
            btnDoiMK.Dock = DockStyle.Top;
            btnLogout.Dock = DockStyle.Top;
            bottom.Controls.Add(btnLogout);
            bottom.Controls.Add(btnDoiMK);
            bottom.Controls.Add(pnlUser);

            pnlMenu.Controls.Add(pnlNavHost);
            pnlMenu.Controls.Add(bottom);
            pnlMenu.Controls.Add(pnlLogo);
            pnlNavHost.BringToFront();

            menuWheelFilter = new MenuWheelFilter(pnlNavHost, ScrollMenu);
            Application.AddMessageFilter(menuWheelFilter);
            FormClosed += (s, e) => Application.RemoveMessageFilter(menuWheelFilter);

            // ----- thanh tiêu đề
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(24, 8, 24, 8) };
            pnlHeader.Paint += (s, e) =>
            {
                using (var p = new Pen(UiHelper.Line)) e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };
            lblBreadcrumb = new Label { Dock = DockStyle.Top, Height = 20, ForeColor = UiHelper.Muted, Font = UiHelper.SmallFont, BackColor = Color.White };
            lblPage = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UiHelper.TextColor, TextAlign = ContentAlignment.TopLeft, BackColor = Color.White };
            var lblDate = new Label
            {
                Dock = DockStyle.Right,
                Width = 320,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = UiHelper.Muted,
                BackColor = Color.White,
                Text = UpperFirst(DateTime.Now.ToString("dddd, dd/MM/yyyy"))
            };
            pnlHeader.Controls.Add(lblPage);
            pnlHeader.Controls.Add(lblBreadcrumb);
            pnlHeader.Controls.Add(lblDate);
            lblPage.BringToFront();

            // ----- nội dung
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.PageBack, Padding = new Padding(16) };

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlMenu);
            pnlContent.BringToFront();

            Shown += (s, e) =>
            {
                ClampMenuScroll();
                if (menuButtons.Count > 0) menuButtons[0].PerformClick();
            };
        }

        private static string UpperFirst(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
        }

        private static void PaintLogo(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            UiHelper.DrawShopLogo(g, new Rectangle(18, 19, 46, 48));
            using (var f = new Font("Segoe UI", 13.5F, FontStyle.Bold))
            {
                const TextFormatFlags tf = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                Size sportSize = TextRenderer.MeasureText(g, "SPORT", f, Size.Empty, tf);
                TextRenderer.DrawText(g, "SPORT", f, new Point(72, 22), Color.White, tf);
                TextRenderer.DrawText(g, "ZONE", f, new Point(72 + sportSize.Width, 22), UiHelper.Accent, tf);
            }
            TextRenderer.DrawText(g, "Phân hệ quản trị", UiHelper.SmallFont, new Point(73, 47), Color.FromArgb(160, 178, 200));
            using (var pen = new Pen(Color.FromArgb(44, 68, 96)))
                g.DrawLine(pen, 18, 84, 232, 84);
        }

        private static void PaintUser(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(52, 78, 108))) g.DrawLine(pen, 18, 0, p.Width - 18, 0);
            string ten = Session.CurrentAdmin != null ? Session.CurrentAdmin.HoTen : "Quản trị viên";
            string[] parts = (ten ?? "").Trim().Split(' ');
            string initials = parts.Length == 0 || parts[0] == "" ? "A"
                : (parts.Length == 1 ? parts[0].Substring(0, 1) : parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
            var circle = new Rectangle(22, 14, 38, 38);
            using (var b = new SolidBrush(UiHelper.Primary)) g.FillEllipse(b, circle);
            using (var f = new Font("Segoe UI", 10F, FontStyle.Bold))
                TextRenderer.DrawText(g, initials, f, circle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (var f = new Font("Segoe UI Semibold", 10F))
                TextRenderer.DrawText(g, ten, f, new Rectangle(70, 14, p.Width - 84, 20), Color.White, TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, "Quản trị viên", UiHelper.SmallFont, new Point(70, 34), Color.FromArgb(160, 178, 200));
        }

        private static void AddSection(FlowLayoutPanel flow, string text)
        {
            flow.Controls.Add(new Label
            {
                Text = text,
                Width = 250,
                Height = 30,
                ForeColor = Color.FromArgb(122, 143, 168),
                Font = new Font("Segoe UI Semibold", 8.25F),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(24, 0, 0, 2),
                Margin = new Padding(0)
            });
        }

        private SideMenuButton MenuButton(string text, string glyph)
        {
            return new SideMenuButton
            {
                Text = text,
                Glyph = glyph,
                Width = 250,
                Height = 44,
                BackColor = UiHelper.MenuBack,
                Margin = new Padding(0),
                UseVisualStyleBackColor = false
            };
        }

        private void AddMenu(FlowLayoutPanel flow, string text, string glyph, Func<Form> factory)
        {
            SideMenuButton b = MenuButton(text, glyph);
            b.Click += (s, e) => OpenChild(b, factory());
            menuButtons.Add(b);
            flow.Controls.Add(b);
        }

        private void ScrollMenu(int wheelDelta)
        {
            if (wheelDelta == 0) return;
            int step = (int)Math.Round(-wheelDelta * 44.0 / 120.0);
            if (step == 0) step = wheelDelta < 0 ? 1 : -1;
            menuScrollY += step;
            ClampMenuScroll();
        }

        private void ClampMenuScroll()
        {
            if (pnlNavHost == null || flowMenu == null) return;
            if (flowMenu.Width != pnlNavHost.ClientSize.Width && pnlNavHost.ClientSize.Width > 0)
                flowMenu.Width = pnlNavHost.ClientSize.Width;
            int maxScroll = Math.Max(0, flowMenu.Height - pnlNavHost.ClientSize.Height);
            if (menuScrollY < 0) menuScrollY = 0;
            if (menuScrollY > maxScroll) menuScrollY = maxScroll;
            if (flowMenu.Top != -menuScrollY) flowMenu.Top = -menuScrollY;
        }

        private void EnsureMenuButtonVisible(Control button)
        {
            if (button == null || pnlNavHost == null || flowMenu == null) return;
            int viewH = pnlNavHost.ClientSize.Height;
            if (viewH <= 0) return;
            if (button.Top < menuScrollY)
                menuScrollY = button.Top - 8;
            else if (button.Bottom > menuScrollY + viewH)
                menuScrollY = button.Bottom - viewH + 8;
            ClampMenuScroll();
        }

        /// <summary>Mở chức năng theo tên mục menu (dùng cho các ô thống kê trên Trang chủ)</summary>
        public void OpenMenu(string text)
        {
            SideMenuButton b = menuButtons.Find(x => x.Text.Trim() == text);
            if (b != null) b.PerformClick();
        }

        private void OpenChild(SideMenuButton source, Form child)
        {
            if (currentChild != null)
            {
                currentChild.Close();
                currentChild.Dispose();
            }
            foreach (SideMenuButton b in menuButtons) b.Selected = b == source;
            EnsureMenuButtonVisible(source);

            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(child);
            currentChild = child;
            lblPage.Text = child.Text;
            lblBreadcrumb.Text = "SportZone Admin  ›  " + source.Text;
            child.Show();
        }

        /// <summary>Bắt sự kiện con lăn chuột khi trỏ vào vùng menu trái (kể cả khi focus đang ở vùng nội dung).</summary>
        private sealed class MenuWheelFilter : IMessageFilter
        {
            private const int WM_MOUSEWHEEL = 0x020A;
            private readonly Control host;
            private readonly Action<int> onScroll;

            public MenuWheelFilter(Control host, Action<int> onScroll)
            {
                this.host = host;
                this.onScroll = onScroll;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL || host.IsDisposed || !host.Visible) return false;
                Form top = host.FindForm();
                if (top == null || Form.ActiveForm != top) return false;
                if (!host.RectangleToScreen(host.ClientRectangle).Contains(Cursor.Position)) return false;
                int delta = unchecked((short)(((long)m.WParam >> 16) & 0xFFFF));
                onScroll(delta);
                return true;
            }
        }
    }

    /// <summary>Đổi mật khẩu quản trị viên</summary>
    public class frmDoiMatKhau : Form
    {
        public frmDoiMatKhau()
        {
            Text = "Đổi mật khẩu";
            Font = UiHelper.BaseFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(440, 230);

            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            TextBox txtCu = UiHelper.AddField(table, "Mật khẩu hiện tại:", new TextBox { UseSystemPasswordChar = true });
            TextBox txtMoi = UiHelper.AddField(table, "Mật khẩu mới:", new TextBox { UseSystemPasswordChar = true });
            TextBox txtLai = UiHelper.AddField(table, "Nhập lại:", new TextBox { UseSystemPasswordChar = true });

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            Button btnHuy = UiHelper.MakeButton("Hủy", UiHelper.Gray, 90);
            Button btnLuu = UiHelper.MakeButton("Lưu", UiHelper.Primary, 90);
            buttons.Controls.Add(btnHuy);
            buttons.Controls.Add(btnLuu);
            Controls.Add(table);
            Controls.Add(buttons);
            AcceptButton = btnLuu;
            CancelButton = btnHuy;

            btnHuy.Click += (s, e) => Close();
            btnLuu.Click += (s, e) =>
            {
                bool ok = UiHelper.Try(this, () =>
                    new BLL.TaiKhoanBLL().DoiMatKhau(Session.CurrentAdmin, txtCu.Text, txtMoi.Text, txtLai.Text));
                if (ok)
                {
                    UiHelper.Info(this, "Đổi mật khẩu thành công.");
                    Close();
                }
            };
        }
    }
}
