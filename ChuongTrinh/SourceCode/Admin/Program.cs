using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using SportZoneAdmin.GUI;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Định dạng số / ngày kiểu Việt Nam: 1.250.000 - 31/12/2026
            var vi = new CultureInfo("vi-VN");
            Thread.CurrentThread.CurrentCulture = vi;
            CultureInfo.DefaultThreadCurrentCulture = vi;

            Application.ThreadException += (s, e) =>
                MessageBox.Show("Đã xảy ra lỗi không mong muốn:\n" + e.Exception.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            while (true)
            {
                using (var login = new frmDangNhap())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }

                var main = new frmMain();
                Application.Run(main);
                if (!main.DaDangXuat) return;
                Session.CurrentAdmin = null;
            }
        }
    }
}
