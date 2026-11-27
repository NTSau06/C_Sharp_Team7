using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace SportZoneWeb.Utils
{
    /// <summary>Hàm tiện ích dùng trong View / Controller</summary>
    public static class WebHelper
    {
        public static readonly CultureInfo VN = new CultureInfo("vi-VN");

        /// <summary>1250000 => "1.250.000 ₫"</summary>
        public static string Tien(decimal v) => v.ToString("N0", VN) + " ₫";

        // ------------------------------------------------------------------ ảnh (dùng chung với Admin)
        // Ảnh sản phẩm nằm trực tiếp trong wwwroot/images; cột SanPham.HinhAnh chỉ lưu TÊN FILE.
        // Admin chép ảnh vào đúng thư mục này (App.config > ImageFolder).
        public const string ThuMucAnh = "/images/";
        /// <summary>Ảnh mặc định khi sản phẩm chưa có ảnh hoặc file ảnh lỗi / không tồn tại</summary>
        public const string AnhMacDinh = "/images/placeholders/no-image.svg";

        /// <summary>
        /// Mã JavaScript cho onerror của thẻ img: lần lỗi đầu đổi sang no-image.svg,
        /// nếu no-image.svg cũng lỗi thì gỡ thẻ img để lộ khung màu danh mục + chữ viết tắt (không bao giờ vỡ ảnh).
        /// </summary>
        public const string OnErrorAnh =
            "if(this.dataset.fb){this.remove();}else{this.dataset.fb='1';this.src='" + AnhMacDinh + "';}";

        /// <summary>Đường dẫn ảnh sản phẩm trong wwwroot/images, null nếu không có ảnh</summary>
        public static string AnhSanPham(string hinhAnh)
        {
            if (string.IsNullOrWhiteSpace(hinhAnh)) return null;
            // Chỉ lấy tên file (chặn đường dẫn kiểu ../ hoặc C:\...)
            string ten = Path.GetFileName(hinhAnh.Trim().Replace('\\', '/'));
            if (ten == "") return null;
            // Tương thích ngược nếu CSDL cũ còn lưu tên ảnh mẫu .svg đã chuyển sang .jpg
            if (ten.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                ten = Path.ChangeExtension(ten, ".jpg");
            return ThuMucAnh + Uri.EscapeDataString(ten);
        }

        /// <summary>Đường dẫn ảnh sản phẩm, hoặc ảnh mặc định nếu chưa có ảnh</summary>
        public static string AnhHoacMacDinh(string hinhAnh) => AnhSanPham(hinhAnh) ?? AnhMacDinh;

        /// <summary>Chữ viết tắt hiển thị khi sản phẩm chưa có ảnh</summary>
        public static string VietTat(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return "SP";
            string[] parts = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string s = parts[0].Substring(0, 1) + (parts.Length > 1 ? parts[1].Substring(0, 1) : "");
            return s.ToUpper(VN);
        }

        /// <summary>Màu nền ảnh tạm theo danh mục (MaDM 1..7 khớp màu ảnh mẫu trong wwwroot/images)</summary>
        public static string MauDanhMuc(int maDM)
        {
            string[] mau = { "#2C4A6B", "#2E7D32", "#1565C0", "#E65100", "#6A1B9A", "#C62828", "#00838F", "#AD1457" };
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

        /// <summary>Ảnh đại diện danh mục trong wwwroot/images/categories (chọn theo tên, không phụ thuộc MaDM)</summary>
        public static string AnhDanhMuc(string tenDM)
        {
            string t = (tenDM ?? "").ToLower(VN);
            string file = "the-thao";
            if (t.Contains("bóng đá")) file = "bong-da";
            else if (t.Contains("cầu lông")) file = "cau-long";
            else if (t.Contains("bóng rổ")) file = "bong-ro";
            else if (t.Contains("gym") || t.Contains("fitness")) file = "gym-fitness";
            else if (t.Contains("chạy")) file = "chay-bo";
            else if (t.Contains("bơi")) file = "boi-loi";
            else if (t.Contains("bóng bàn")) file = "bong-ban";
            return "/images/categories/" + file + ".svg";
        }

        /// <summary>
        /// Khung ảnh sản phẩm: nền màu theo danh mục + chữ viết tắt; phủ ảnh sản phẩm lên trên.
        /// HinhAnh = NULL => hiển thị no-image.svg; file ảnh lỗi/không tồn tại => tự đổi sang no-image.svg.
        /// </summary>
        public static IHtmlContent Anh(string hinhAnh, string tenSP, int maDM, string css = "")
        {
            var enc = HtmlEncoder.Default;
            string url = AnhHoacMacDinh(hinhAnh);
            string html = "<div class=\"thumb " + enc.Encode(css ?? "") + "\" style=\"--c:" + MauDanhMuc(maDM) + "\">" +
                          "<span class=\"thumb-txt\">" + enc.Encode(VietTat(tenSP)) + "</span>" +
                          "<img src=\"" + enc.Encode(url) + "\" alt=\"" + enc.Encode(tenSP ?? "") + "\" loading=\"lazy\"" +
                          (url == AnhMacDinh ? " data-fb=\"1\"" : "") +
                          " onerror=\"" + OnErrorAnh + "\" />" +
                          "</div>";
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
