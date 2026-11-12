using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using SportShopAdmin.BLL;
using SportShopAdmin.DAL;
using SportShopAdmin.Utils;

namespace SportShopAdmin.GUI
{
    /// <summary>UC 1.2.1.1 - Đăng nhập (Quản trị viên - Desktop)</summary>
    public class frmDangNhap : Form
    {
        private readonly TaiKhoanBLL taiKhoanBLL = new TaiKhoanBLL();
        private readonly TextBox txtServer;
        private readonly TextBox txtUser;
        private readonly TextBox txtPass;
        private readonly CheckBox chkShow;
        private readonly Button btnLogin;
        private readonly Button btnExit;
        private readonly Label lblHint;
        private readonly LinkLabel lnkTaoLai;

        public frmDangNhap()
        {
            Text = "Đăng nhập - Sport Shop Admin";
            Font = UiHelper.BaseFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 450);
            BackColor = Color.White;

            var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = UiHelper.Primary };
            header.Controls.Add(new Label
            {
                Text = "SPORT SHOP\nPhân hệ Quản trị",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            });

            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(24, 20, 24, 0) };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            txtServer = UiHelper.AddField(table, "Máy chủ SQL:", new TextBox());
            txtUser = UiHelper.AddField(table, "Tên đăng nhập:", new TextBox());
            txtPass = UiHelper.AddField(table, "Mật khẩu:", new TextBox { UseSystemPasswordChar = true });
            chkShow = UiHelper.AddField(table, "", new CheckBox { Text = "Hiện mật khẩu", AutoSize = true });
            chkShow.CheckedChanged += (s, e) => txtPass.UseSystemPasswordChar = !chkShow.Checked;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(150, 14, 0, 0) };
            btnLogin = UiHelper.MakeButton("Đăng nhập", UiHelper.Primary, 120);
            btnExit = UiHelper.MakeButton("Thoát", UiHelper.Gray, 100);
            buttons.Controls.Add(btnLogin);
            buttons.Controls.Add(btnExit);

            lblHint = new Label
            {
                Text = HintMacDinh,
                Dock = DockStyle.Bottom,
                Height = 30,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lnkTaoLai = new LinkLabel
            {
                Text = "Tạo lại CSDL mẫu (xóa dữ liệu hiện tại)",
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lnkTaoLai.LinkClicked += LnkTaoLai_LinkClicked;

            Controls.Add(lnkTaoLai);
            Controls.Add(lblHint);
            Controls.Add(buttons);
            Controls.Add(table);
            Controls.Add(header);

            AcceptButton = btnLogin;
            CancelButton = btnExit;
            btnLogin.Click += BtnLogin_Click;
            btnExit.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            try { txtServer.Text = DBConnect.GetServerName(); }
            catch (Exception ex) { txtServer.Text = "."; UiHelper.Warn(this, ex.Message); }
            txtUser.Text = "admin";
            Shown += (s, e) => txtPass.Focus();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string server = txtServer.Text.Trim();
            if (server == "") { UiHelper.Warn(this, "Vui lòng nhập tên máy chủ SQL Server."); return; }

            // 1) Kết nối máy chủ (tự dò nếu tên nhập vào không dùng được)
            // 2) Tự tạo CSDL SportShopDB + dữ liệu mẫu nếu chưa có
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
        /// Dò máy chủ SQL, lưu vào App.config và đảm bảo có CSDL SportShopDB.
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
                    BaoTrangThai("Đang tạo lại CSDL SportShopDB...");
                    DatabaseInitializer.CreateDatabase(server);
                    UiHelper.Info(this, "Đã tạo lại CSDL SportShopDB với dữ liệu mẫu trên máy chủ \"" + server + "\".");
                }
                else
                {
                    BaoTrangThai("Đang kiểm tra CSDL SportShopDB...");
                    if (DatabaseInitializer.EnsureDatabase(server))
                        UiHelper.Info(this, "Chưa có CSDL SportShopDB nên chương trình đã tự tạo (11 bảng + dữ liệu mẫu)\n" +
                            "trên máy chủ \"" + server + "\".\n\nĐăng nhập: admin / admin123");
                }

                DBConnect.TestConnection();
                return server;
            }
            catch (SqlException ex)
            {
                UiHelper.Warn(this, "Lỗi khi kết nối / tạo CSDL SportShopDB trên máy chủ \"" + server + "\".\n\n" +
                    "Có thể chạy tay file Database\\SportShopDB.sql trong SSMS.\n\nChi tiết: " + ex.Message);
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
            if (server == "") { UiHelper.Warn(this, "Vui lòng nhập tên máy chủ SQL Server."); return; }
            if (!UiHelper.Confirm(this, "Xóa toàn bộ dữ liệu hiện tại và tạo lại CSDL SportShopDB với dữ liệu mẫu?"))
                return;
            ChuanBiCSDL(server, true);
        }
    }
}
