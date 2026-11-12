using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace SportShopWeb.Utils
{
    /// <summary>Hàm tiện ích dùng trong View / Controller</summary>
    public static class WebHelper
    {
        public static readonly CultureInfo VN = new CultureInfo("vi-VN");

        /// <summary>1250000 => "1.250.000 ₫"</summary>
        public static string Tien(decimal v) => v.ToString("N0", VN) + " ₫";

        /// <summary>Đường dẫn ảnh sản phẩm trong wwwroot/images, null nếu không có ảnh</summary>
        public static string AnhSanPham(string hinhAnh)
        {
            return string.IsNullOrWhiteSpace(hinhAnh) ? null : "/images/" + Uri.EscapeDataString(hinhAnh.Trim());
        }

        /// <summary>Chữ viết tắt hiển thị khi sản phẩm chưa có ảnh</summary>
        public static string VietTat(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return "SP";
            string[] parts = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string s = parts[0].Substring(0, 1) + (parts.Length > 1 ? parts[1].Substring(0, 1) : "");
            return s.ToUpper(VN);
        }

        /// <summary>Màu nền ảnh tạm theo danh mục</summary>
        public static string MauDanhMuc(int maDM)
        {
            string[] mau = { "#0f766e", "#2563eb", "#c2410c", "#7c3aed", "#be123c", "#0369a1", "#15803d", "#a16207" };
            return mau[Math.Abs(maDM) % mau.Length];
        }

        /// <summary>Biểu tượng (emoji) theo tên danh mục</summary>
        public static string IconDanhMuc(string tenDM)
        {
            string t = (tenDM ?? "").ToLower(VN);
            if (t.Contains("bóng đá")) return "⚽";
            if (t.Contains("cầu lông")) return "🏸";
            if (t.Contains("bóng rổ")) return "🏀";
            if (t.Contains("gym") || t.Contains("fitness")) return "🏋️";
            if (t.Contains("chạy")) return "🏃";
            if (t.Contains("bơi")) return "🏊";
            if (t.Contains("bóng bàn")) return "🏓";
            if (t.Contains("tennis") || t.Contains("quần vợt")) return "🎾";
            if (t.Contains("bóng chuyền")) return "🏐";
            return "🏅";
        }

        /// <summary>
        /// Khung ảnh sản phẩm: nền màu theo danh mục + chữ viết tắt; nếu có file ảnh trong wwwroot/images thì phủ ảnh lên
        /// (ảnh lỗi/không tồn tại sẽ tự ẩn, chỉ còn khung chữ).
        /// </summary>
        public static IHtmlContent Anh(string hinhAnh, string tenSP, int maDM, string css = "")
        {
            var enc = HtmlEncoder.Default;
            string url = AnhSanPham(hinhAnh);
            string html = "<div class=\"thumb " + enc.Encode(css) + "\" style=\"--c:" + MauDanhMuc(maDM) + "\">" +
                          "<span class=\"thumb-txt\">" + enc.Encode(VietTat(tenSP)) + "</span>";
            if (url != null)
                html += "<img src=\"" + enc.Encode(url) + "\" alt=\"" + enc.Encode(tenSP ?? "") + "\" loading=\"lazy\" onerror=\"this.remove()\" />";
            html += "</div>";
            return new HtmlString(html);
        }

        // ------------------------------------------------------------------ tài khoản đăng nhập
        public const string ClaimMaKH = "MaKH";

        public static int? MaKH(this ClaimsPrincipal user)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated) return null;
            string v = user.FindFirst(ClaimMaKH)?.Value;
            return int.TryParse(v, out int id) ? id : (int?)null;
        }

        public static string HoTen(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.GivenName)?.Value ?? user?.Identity?.Name ?? "";
        }
    }
}
