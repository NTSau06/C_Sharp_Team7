using SportZoneWeb.DTO;
using SportZoneWeb.Models;

namespace SportZoneWeb.Utils
{
    /// <summary>
    /// Thương hiệu / kích cỡ / màu sắc của sản phẩm cho bộ lọc.
    /// CSDL chưa có các cột này nên tạm suy ra: thương hiệu lấy từ tên sản phẩm,
    /// kích cỡ theo loại sản phẩm, màu sắc theo mã sản phẩm (dữ liệu minh họa).
    /// </summary>
    public static class ThuocTinhSanPham
    {
        public const string Khac = "Khác";

        public static readonly string[] ThuongHieus =
        {
            "Adidas", "Butterfly", "DHS", "Động Lực", "Garmin", "Lining", "Mizuno",
            "Molten", "Nike", "Spalding", "Speedo", "Thành Công", "Yonex", Khac
        };

        public static readonly string[] KichCos = { "S", "M", "L", "XL", "39", "40", "41", "42", "43", "Số 5", "Số 7", "Free size" };

        public static readonly string[] MauSacs = { "Đen", "Trắng", "Đỏ", "Xanh dương", "Xanh lá", "Vàng" };

        /// <summary>Mã màu hiển thị cho ô chọn màu</summary>
        public static string MaMau(string mau) => mau switch
        {
            "Đen" => "#1F2933",
            "Trắng" => "#FFFFFF",
            "Đỏ" => "#D32F2F",
            "Xanh dương" => "#1565C0",
            "Xanh lá" => "#2E7D32",
            "Vàng" => "#F4B400",
            _ => "#94A3B8"
        };

        public static string ThuongHieu(SanPhamDTO sp)
        {
            string ten = sp?.TenSP ?? "";
            return ThuongHieus.FirstOrDefault(t => t != Khac && ten.Contains(t, StringComparison.OrdinalIgnoreCase)) ?? Khac;
        }

        public static string[] KichCo(SanPhamDTO sp)
        {
            string ten = (sp?.TenSP ?? "").ToLower(WebHelper.VN);
            if (ten.Contains("giày")) return new[] { "39", "40", "41", "42", "43" };
            if (ten.Contains("áo")) return new[] { "S", "M", "L", "XL" };
            if (ten.Contains("găng tay")) return new[] { "M", "L" };
            if (ten.Contains("số 5")) return new[] { "Số 5" };
            if (ten.Contains("số 7")) return new[] { "Số 7" };
            return new[] { "Free size" };
        }

        public static string[] MauSac(SanPhamDTO sp)
        {
            int id = Math.Abs(sp?.MaSP ?? 0);
            return new[] { MauSacs[id % MauSacs.Length], MauSacs[(id * 2 + 1) % MauSacs.Length] }.Distinct().ToArray();
        }

        /// <summary>Sản phẩm có khớp bộ lọc thương hiệu / kích cỡ / màu không (bỏ trống = không lọc)</summary>
        public static bool Khop(SanPhamDTO sp, SanPhamFilter f)
        {
            if (!string.IsNullOrEmpty(f.ThuongHieu) && ThuongHieu(sp) != f.ThuongHieu) return false;
            if (!string.IsNullOrEmpty(f.KichCo) && !KichCo(sp).Contains(f.KichCo)) return false;
            if (!string.IsNullOrEmpty(f.MauSac) && !MauSac(sp).Contains(f.MauSac)) return false;
            return true;
        }
    }
}
