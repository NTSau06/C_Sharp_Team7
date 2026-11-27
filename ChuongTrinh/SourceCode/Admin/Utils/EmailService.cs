using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace SportZoneAdmin.Utils
{
    /// <summary>
    /// Gửi email thông báo (UC 1.2.5.1 bước 5: gửi thông tin đăng nhập cho khách hàng do quản trị viên tạo).
    /// Cấu hình SMTP đọc từ appSettings (App.config): SmtpHost, SmtpPort, SmtpUser, SmtpPassword, SmtpFrom, SmtpEnableSsl.
    /// Ứng dụng Web có thể gán trực tiếp các thuộc tính tĩnh bên dưới từ appsettings.json.
    /// Nếu chưa cấu hình SmtpHost thì không gửi và trả về thông báo, không làm hỏng thao tác chính.
    /// </summary>
    public static class EmailService
    {
        public static string Host { get; set; } = Setting("SmtpHost");
        public static int Port { get; set; } = ParseInt(Setting("SmtpPort"), 587);
        public static string User { get; set; } = Setting("SmtpUser");
        public static string Password { get; set; } = Setting("SmtpPassword");
        public static string From { get; set; } = Setting("SmtpFrom");
        public static bool EnableSsl { get; set; } = !string.Equals(Setting("SmtpEnableSsl"), "false", StringComparison.OrdinalIgnoreCase);

        public static bool DaCauHinh => !string.IsNullOrWhiteSpace(Host);

        /// <summary>Gửi tên đăng nhập + mật khẩu cho khách hàng. Trả về true nếu gửi thành công; loi chứa lý do nếu không gửi được.</summary>
        public static bool GuiThongTinTaiKhoan(string email, string hoTen, string tenDangNhap, string matKhau, out string loi)
        {
            string body =
                "Xin chào " + hoTen + ",\r\n\r\n" +
                "Cửa hàng Sport Shop đã tạo tài khoản mua hàng cho bạn:\r\n" +
                "  - Tên đăng nhập: " + tenDangNhap + "\r\n" +
                "  - Mật khẩu: " + matKhau + "\r\n\r\n" +
                "Vui lòng đăng nhập website và đổi mật khẩu sau lần đăng nhập đầu tiên.\r\n\r\n" +
                "Trân trọng,\r\nSport Shop";
            return Gui(email, "[Sport Shop] Thông tin tài khoản của bạn", body, out loi);
        }

        public static bool Gui(string to, string subject, string body, out string loi)
        {
            loi = null;
            if (!DaCauHinh)
            {
                loi = "Chưa cấu hình máy chủ gửi email (SmtpHost trong App.config).";
                return false;
            }
            try
            {
                using (var msg = new MailMessage(string.IsNullOrWhiteSpace(From) ? User : From, to, subject, body))
                using (var client = new SmtpClient(Host, Port))
                {
                    client.EnableSsl = EnableSsl;
                    if (!string.IsNullOrEmpty(User))
                        client.Credentials = new NetworkCredential(User, Password);
                    client.Send(msg);
                }
                return true;
            }
            catch (Exception ex)
            {
                loi = ex.Message;
                return false;
            }
        }

        private static string Setting(string key)
        {
            try { return ConfigurationManager.AppSettings[key]; }
            catch { return null; }
        }

        private static int ParseInt(string s, int macDinh)
        {
            int v;
            return int.TryParse(s, out v) ? v : macDinh;
        }
    }
}
