using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DAL;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>UC 1.2.1.1 - Đăng nhập (Quản trị viên - Desktop)</summary>
    public class frmDangNhap : Form
    {
        private readonly TaiKhoanBLL taiKhoanBLL = new TaiKhoanBLL();
        // Tên máy chủ SQL: KHÔNG hiển thị trên form. Đọc từ App.config, tự dò nếu không kết nối được.
        private readonly TextBox txtServer = new TextBox();
        private readonly TextBox txtUser;
        private readonly TextBox txtPass;
        private readonly Button btnLogin;
        private readonly Button btnExit;
        private readonly Label lblHint;
        private readonly LinkLabel lnkTaoLai;

        public frmDangNhap()
        {
            Text = "Đăng nhập - SportZone Admin";
            Icon = UiHelper.AppIcon;
            Font = UiHelper.BaseFont;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(880, 540);
            BackColor = Color.White;

            // ----- cột trái: thương hiệu
            var brand = new Panel { Dock = DockStyle.Left, Width = 380 };
            brand.Paint += PaintBrand;

            // ----- cột phải: form đăng nhập (định vị cố định vì form không đổi kích thước)
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            const int x = 64, w = 372;

            var lblTitle = new Label { Text = "Đăng nhập", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = UiHelper.TextColor, AutoSize = true, Location = new Point(x - 3, 52) };
            var lblSub = new Label { Text = "Chào mừng trở lại! Đăng nhập để quản lý cửa hàng.", ForeColor = UiHelper.Muted, AutoSize = true, Location = new Point(x, 100) };

            var lblUser = new Label { Text = "Tên đăng nhập", Font = new Font("Segoe UI Semibold", 9.75F), ForeColor = UiHelper.TextColor, AutoSize = true, Location = new Point(x, 148) };
            txtUser = new TextBox();
            var boxUser = new InputBox(Glyph.User, txtUser) { Location = new Point(x, 172), Width = w };

            var lblPass = new Label { Text = "Mật khẩu", Font = new Font("Segoe UI Semibold", 9.75F), ForeColor = UiHelper.TextColor, AutoSize = true, Location = new Point(x, 234) };
            txtPass = new TextBox { UseSystemPasswordChar = true };
            var boxPass = new InputBox(Glyph.Lock, txtPass, true) { Location = new Point(x, 258), Width = w };

            btnLogin = UiHelper.MakeButton("Đăng nhập", UiHelper.Primary, w);
            btnLogin.Height = 46;
            btnLogin.Font = new Font("Segoe UI Semibold", 11F);
            btnLogin.Location = new Point(x, 332);
            btnExit = UiHelper.MakeButton("Thoát", UiHelper.Gray, w);
            btnExit.Height = 40;
            btnExit.Location = new Point(x, 388);

            lblHint = new Label
            {
                Text = HintMacDinh,
                ForeColor = UiHelper.Muted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(x, 444),
                Size = new Size(w, 24)
            };
            lnkTaoLai = new LinkLabel
            {
                Text = "Tạo lại CSDL mẫu (xóa dữ liệu hiện tại)",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(x, 478),
                Size = new Size(w, 28),
                LinkColor = UiHelper.Primary,
                ActiveLinkColor = UiHelper.PrimaryDark,
                // Gạch chân thường trực đè lên dấu nặng (ạ, ệ...) làm chữ trông như thiếu dấu -> chỉ gạch chân khi rê chuột
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkTaoLai.LinkClicked += LnkTaoLai_LinkClicked;

            right.Controls.AddRange(new Control[] { lblTitle, lblSub, lblUser, boxUser, lblPass, boxPass, btnLogin, btnExit, lblHint, lnkTaoLai });
            Controls.Add(right);
            Controls.Add(brand);

            AcceptButton = btnLogin;
            CancelButton = btnExit;
            btnLogin.Click += BtnLogin_Click;
            btnExit.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            try { txtServer.Text = DBConnect.GetServerName(); }
            catch (Exception ex) { txtServer.Text = "."; UiHelper.Warn(this, ex.Message); }
            txtUser.Text = "admin";
            Shown += (s, e) => txtPass.Focus();
        }

        /// <summary>Vẽ cột thương hiệu: nền chuyển màu xanh đậm, logo, khẩu hiệu và các tính năng chính</summary>
        private static void PaintBrand(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new LinearGradientBrush(p.ClientRectangle, UiHelper.PrimaryDark, UiHelper.Primary, 60F))
                g.FillRectangle(bg, p.ClientRectangle);
            // họa tiết tròn mờ
            using (var b = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
            {
                g.FillEllipse(b, p.Width - 170, -90, 280, 280);
                g.FillEllipse(b, -120, p.Height - 170, 260, 260);
            }

            UiHelper.DrawShopLogo(g, new Rectangle(42, 48, 66, 68));

            using (var f = new Font("Segoe UI", 23F, FontStyle.Bold))
            {
                const TextFormatFlags tf = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                Size sportSize = TextRenderer.MeasureText(g, "SPORT", f, Size.Empty, tf);
                TextRenderer.DrawText(g, "SPORT", f, new Point(42, 130), Color.White, tf);
                TextRenderer.DrawText(g, "ZONE", f, new Point(42 + sportSize.Width, 130), UiHelper.Accent, tf);
            }
            using (var f = new Font("Segoe UI", 11F))
                TextRenderer.DrawText(g, "Phân hệ quản trị cửa hàng\ndụng cụ thể thao chính hãng", f, new Rectangle(44, 178, p.Width - 80, 50), Color.FromArgb(203, 215, 230), TextFormatFlags.WordBreak);

            string[] features =
            {
                "Quản lý sản phẩm, tồn kho và nhập kho",
                "Xử lý đơn hàng đặt từ website",
                "Mã giảm giá và thống kê doanh thu"
            };
            int y = 268;
            foreach (string ft in features)
            {
                var circle = new Rectangle(44, y, 26, 26);
                using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillEllipse(b, circle);
                TextRenderer.DrawText(g, Glyph.Check, UiHelper.IconFont(9F), circle, UiHelper.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, ft, UiHelper.BaseFont, new Rectangle(82, y, p.Width - 110, 26), Color.White, TextFormatFlags.VerticalCenter);
                y += 42;
            }
            TextRenderer.DrawText(g, "Nhóm 07 · Trường Đại học Sài Gòn", UiHelper.SmallFont, new Point(44, p.Height - 46), Color.FromArgb(160, 178, 200));
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string server = txtServer.Text.Trim();
            if (server == "") server = ".";   // chưa cấu hình: thử máy chủ mặc định rồi tự dò

            // 1) Kết nối máy chủ (tự dò nếu tên nhập vào không dùng được)
            // 2) Tự tạo CSDL SportZoneDB + dữ liệu mẫu nếu chưa có
            server = ChuanBiCSDL(server, false);
            if (server == null) return;

            bool ok = UiHelper.Try(this, () =>
            {
                Session.CurrentAdmin = taiKhoanBLL.DangNhapQuanTri(txtUser.Text, txtPass.Text);
            });
            if (ok)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                txtPass.SelectAll();
                txtPass.Focus();
            }
        }

        private const string HintMacDinh = "Tài khoản mẫu: admin / admin123";

        private void BaoTrangThai(string text)
        {
            lblHint.Text = text;
            lblHint.Refresh();
        }

        /// <summary>
        /// Dò máy chủ SQL, lưu vào App.config và đảm bảo có CSDL SportZoneDB.
        /// Trả về tên máy chủ dùng được, hoặc null nếu thất bại (đã báo lỗi).
        /// </summary>
        private string ChuanBiCSDL(string server, bool taoLai)
        {
            Cursor = Cursors.WaitCursor;
            btnLogin.Enabled = false;
            try
            {
                BaoTrangThai("Đang kết nối máy chủ SQL \"" + server + "\"...");
                string loi;
                if (!DatabaseInitializer.CanConnectServer(server, out loi))
                {
                    BaoTrangThai("Đang tự dò máy chủ SQL Server trên máy...");
                    string found = DatabaseInitializer.FindServer(null);
                    if (found == null)
                    {
                        UiHelper.Warn(this, "Không tìm thấy SQL Server nào trên máy.\n\n" +
                            "• Mở Visual Studio Installer → Modify → Individual components → tick\n" +
                            "  \"SQL Server Express LocalDB\" rồi cài đặt, sau đó chạy lại chương trình.\n" +
                            "• Hoặc cài SQL Server Express / Developer và nhập đúng tên máy chủ.\n\n" +
                            "Chi tiết (" + server + "): " + loi);
                        return null;
                    }
                    if (!UiHelper.Confirm(this, "Không kết nối được máy chủ \"" + server + "\".\n" +
                        "Đã tìm thấy máy chủ SQL \"" + found + "\". Dùng máy chủ này?"))
                        return null;
                    server = found;
                    txtServer.Text = found;
                }

                if (!string.Equals(server, DBConnect.GetServerName(), StringComparison.OrdinalIgnoreCase))
                    DBConnect.SaveServerName(server);

                if (taoLai)
                {
                    BaoTrangThai("Đang tạo lại CSDL SportZoneDB...");
                    DatabaseInitializer.CreateDatabase(server);
                    UiHelper.Info(this, "Đã tạo lại CSDL SportZoneDB với dữ liệu mẫu trên máy chủ \"" + server + "\".");
                }
                else
                {
                    BaoTrangThai("Đang kiểm tra CSDL SportZoneDB...");
                    if (DatabaseInitializer.EnsureDatabase(server))
                        UiHelper.Info(this, "Chưa có CSDL SportZoneDB nên chương trình đã tự tạo (11 bảng + dữ liệu mẫu)\n" +
                            "trên máy chủ \"" + server + "\".\n\nĐăng nhập: admin / admin123");
                }

                BaoTrangThai("Đang cập nhật chức năng Web và thanh toán...");
                DatabaseInitializer.ApplyWebUpdates(server);
                DBConnect.TestConnection();
                return server;
            }
            catch (SqlException ex)
            {
                UiHelper.Warn(this, "Lỗi khi kết nối / tạo CSDL SportZoneDB trên máy chủ \"" + server + "\".\n\n" +
                    "Có thể chạy tay file Database\\SportZoneDB.sql trong SSMS.\n\nChi tiết: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                UiHelper.Warn(this, "Lỗi cấu hình kết nối: " + ex.Message);
                return null;
            }
            finally
            {
                BaoTrangThai(HintMacDinh);
                btnLogin.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void LnkTaoLai_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string server = txtServer.Text.Trim();
            if (server == "") server = ".";   // chưa cấu hình: thử máy chủ mặc định rồi tự dò
            if (!UiHelper.Confirm(this, "Xóa toàn bộ dữ liệu hiện tại và tạo lại CSDL SportZoneDB với dữ liệu mẫu?"))
                return;
            ChuanBiCSDL(server, true);
        }
    }
}
