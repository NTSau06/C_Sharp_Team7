using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SportShopAdmin.Utils;

namespace SportShopAdmin.GUI
{
    /// <summary>Màn hình chính: menu bên trái, nội dung các chức năng hiển thị bên phải</summary>
    public class frmMain : Form
    {
        private readonly Panel pnlContent;
        private readonly Label lblPage;
        private readonly List<Button> menuButtons = new List<Button>();
        private Form currentChild;

        public bool DaDangXuat { get; private set; }

        public frmMain()
        {
            Text = "Sport Shop - Phân hệ Quản trị";
            Font = UiHelper.BaseFont;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1360, 820);
            MinimumSize = new Size(1200, 720);
            WindowState = FormWindowState.Maximized;

            // ----- menu trái
            var pnlMenu = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = UiHelper.MenuBack };
            var lblLogo = new Label
            {
                Text = "SPORT SHOP\nQuản trị cửa hàng",
                Dock = DockStyle.Top,
                Height = 96,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                AutoScroll = true
            };

            AddMenu(flow, "Trang chủ", () => new frmTrangChu(this));
            AddMenu(flow, "Quản lý danh mục", () => new frmDanhMuc());
            AddMenu(flow, "Quản lý sản phẩm", () => new frmSanPham());
            AddMenu(flow, "Quản lý tồn kho", () => new frmTonKho());
            AddMenu(flow, "Quản lý đơn hàng", () => new frmDonHang());
            AddMenu(flow, "Tài khoản khách hàng", () => new frmKhachHang());
            AddMenu(flow, "Mã giảm giá", () => new frmMaGiamGia());
            AddMenu(flow, "Thống kê doanh thu", () => new frmThongKe());

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 104, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            Button btnDoiMK = MenuButton("Đổi mật khẩu");
            btnDoiMK.Click += (s, e) => { using (var f = new frmDoiMatKhau()) f.ShowDialog(this); };
            Button btnLogout = MenuButton("Đăng xuất");
            btnLogout.ForeColor = Color.FromArgb(255, 160, 160);
            btnLogout.Click += (s, e) =>
            {
                if (!UiHelper.Confirm(this, "Bạn muốn đăng xuất?")) return;
                DaDangXuat = true;
                Close();
            };
            bottom.Controls.Add(btnDoiMK);
            bottom.Controls.Add(btnLogout);

            pnlMenu.Controls.Add(flow);
            pnlMenu.Controls.Add(bottom);
            pnlMenu.Controls.Add(lblLogo);
            flow.BringToFront();

            // ----- header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(16, 0, 16, 0) };
            lblPage = new Label { Dock = DockStyle.Fill, Font = UiHelper.TitleFont, ForeColor = Color.FromArgb(33, 37, 41), TextAlign = ContentAlignment.MiddleLeft };
            var lblUser = new Label
            {
                Dock = DockStyle.Right,
                Width = 420,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Text = "Xin chào, " + (Session.CurrentAdmin != null ? Session.CurrentAdmin.HoTen : "") +
                       "   |   " + DateTime.Now.ToString("dddd, dd/MM/yyyy")
            };
            pnlHeader.Controls.Add(lblPage);
            pnlHeader.Controls.Add(lblUser);
            lblPage.BringToFront();

            // ----- nội dung
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.PageBack, Padding = new Padding(12) };

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlMenu);
            pnlContent.BringToFront();

            Shown += (s, e) => { if (menuButtons.Count > 0) menuButtons[0].PerformClick(); };
        }

        private Button MenuButton(string text)
        {
            var b = new Button
            {
                Text = "   " + text,
                Width = 230,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gainsboro,
                BackColor = UiHelper.MenuBack,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10.5F),
                Margin = new Padding(0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            return b;
        }

        private void AddMenu(FlowLayoutPanel flow, string text, Func<Form> factory)
        {
            Button b = MenuButton(text);
            b.Click += (s, e) => OpenChild(b, factory());
            menuButtons.Add(b);
            flow.Controls.Add(b);
        }

        /// <summary>Mở chức năng theo vị trí menu (dùng cho các ô thống kê trên Trang chủ)</summary>
        public void OpenMenu(int index)
        {
            if (index >= 0 && index < menuButtons.Count) menuButtons[index].PerformClick();
        }

        private void OpenChild(Button source, Form child)
        {
            if (currentChild != null)
            {
                currentChild.Close();
                currentChild.Dispose();
            }
            foreach (Button b in menuButtons)
            {
                b.BackColor = UiHelper.MenuBack;
                b.ForeColor = Color.Gainsboro;
            }
            source.BackColor = UiHelper.Primary;
            source.ForeColor = Color.White;

            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(child);
            currentChild = child;
            lblPage.Text = child.Text;
            child.Show();
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
