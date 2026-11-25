using System.Net;
using System.Net.Mail;
using System.Text;
using SportZoneWeb.DTO;

namespace SportZoneWeb.Utils
{
    /// <summary>
    /// Gửi email (xác nhận đơn hàng, liên hệ). Cấu hình trong appsettings.json, mục "Email".
    /// Chưa cấu hình Host => không gửi, ghi log, KHÔNG làm hỏng thao tác chính (đặt hàng vẫn thành công).
    /// </summary>
    public static class EmailService
    {
        public static string Host { get; set; }
        public static int Port { get; set; } = 587;
        public static string User { get; set; }
        public static string Password { get; set; }
        public static string From { get; set; }
        public static string FromName { get; set; } = "SportZone";
        public static bool EnableSsl { get; set; } = true;
        /// <summary>Địa chỉ nhận form Liên hệ</summary>
        public static string ShopEmail { get; set; }
        public static ILogger Logger { get; set; }

        public static bool DaCauHinh => !string.IsNullOrWhiteSpace(Host);

        public static void Nap(IConfiguration cfg)
        {
            IConfigurationSection s = cfg.GetSection("Email");
            Host = s["Host"];
            Port = s.GetValue("Port", 587);
            User = s["User"];
            Password = s["Password"];
            From = s["From"];
            FromName = s["FromName"] ?? "SportZone";
            EnableSsl = s.GetValue("EnableSsl", true);
            ShopEmail = s["ShopEmail"];
        }

        /// <summary>Gửi nền (không chặn request, không ném lỗi). Trả về false nếu chưa cấu hình SMTP.</summary>
        public static bool GuiNen(string to, string subject, string htmlBody, string replyTo = null)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;
            if (!DaCauHinh)
            {
                Logger?.LogWarning("Chưa cấu hình Email:Host trong appsettings.json - bỏ qua email \"{Subject}\" gửi tới {To}", subject, to);
                return false;
            }
            _ = Task.Run(() =>
            {
                try { Gui(to, subject, htmlBody, replyTo); }
                catch (Exception ex) { Logger?.LogError(ex, "Gửi email \"{Subject}\" tới {To} thất bại", subject, to); }
            });
            return true;
        }

        private static void Gui(string to, string subject, string htmlBody, string replyTo)
        {
            string from = string.IsNullOrWhiteSpace(From) ? User : From;
            using var msg = new MailMessage
            {
                From = new MailAddress(from, FromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = htmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            msg.To.Add(to);
            if (!string.IsNullOrWhiteSpace(replyTo)) msg.ReplyToList.Add(replyTo);
            using var client = new SmtpClient(Host, Port) { EnableSsl = EnableSsl };
            if (!string.IsNullOrEmpty(User)) client.Credentials = new NetworkCredential(User, Password);
            client.Send(msg);
        }

        /// <summary>Nội dung email xác nhận đơn hàng (gửi ngay sau khi đặt hàng)</summary>
        public static string NoiDungXacNhanDon(DonHangDTO dh, string hoTen)
        {
            string E(string s) => WebUtility.HtmlEncode(s ?? "");
            var sb = new StringBuilder();
            sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;max-width:620px;margin:auto;color:#1f2933\">");
            sb.Append("<h2 style=\"color:#2C4A6B\">Cảm ơn bạn đã đặt hàng tại SportZone!</h2>");
            sb.Append("<p>Xin chào <b>").Append(E(hoTen)).Append("</b>,</p>");
            sb.Append("<p>Chúng tôi đã nhận được đơn hàng <b>#DH").Append(dh.MaDH).Append("</b> đặt lúc ")
              .Append(dh.NgayDat.ToString("HH:mm dd/MM/yyyy")).Append(". Đơn đang ở trạng thái <b>").Append(E(dh.TrangThai)).Append("</b>.</p>");
            sb.Append("<table style=\"width:100%;border-collapse:collapse\"><tr style=\"background:#f1f4f8\">")
              .Append("<th align=left style=\"padding:8px\">Sản phẩm</th><th style=\"padding:8px\">SL</th><th align=right style=\"padding:8px\">Thành tiền</th></tr>");
            foreach (ChiTietDonHangDTO ct in dh.ChiTiet)
                sb.Append("<tr><td style=\"padding:8px;border-bottom:1px solid #e5e9ef\">").Append(E(ct.TenSP)).Append("</td>")
                  .Append("<td align=center style=\"padding:8px;border-bottom:1px solid #e5e9ef\">").Append(ct.SoLuong).Append("</td>")
                  .Append("<td align=right style=\"padding:8px;border-bottom:1px solid #e5e9ef\">").Append(WebHelper.Tien(ct.ThanhTien)).Append("</td></tr>");
            if (dh.TienGiam > 0)
                sb.Append("<tr><td colspan=2 style=\"padding:8px\">Giảm giá</td><td align=right style=\"padding:8px\">- ").Append(WebHelper.Tien(dh.TienGiam)).Append("</td></tr>");
            sb.Append("<tr><td colspan=2 style=\"padding:8px\"><b>Tổng thanh toán</b></td><td align=right style=\"padding:8px\"><b>")
              .Append(WebHelper.Tien(dh.TongTien)).Append("</b></td></tr></table>");
            sb.Append("<p><b>Giao tới:</b> ").Append(E(dh.DiaChiGiao)).Append("<br/><b>Thanh toán:</b> ").Append(E(dh.TenPTTT))
              .Append(" (").Append(E(dh.TrangThaiThanhToan)).Append(")</p>");
            sb.Append("<p style=\"color:#6b7785;font-size:13px\">Bạn sẽ nhận được thông báo khi đơn hàng thay đổi trạng thái. Trân trọng, SportZone.</p></div>");
            return sb.ToString();
        }
    }
}
