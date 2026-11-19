namespace SportZoneWeb.Models
{
    /// <summary>Bộ lọc + sắp xếp sản phẩm (UC 1.2.7.3, 1.2.7.4, 1.2.8.4, 1.2.8.5) - nhận từ query string</summary>
    public class SanPhamFilter
    {
        public decimal? GiaTu { get; set; }
        public decimal? GiaDen { get; set; }
        /// <summary>Chỉ hiện sản phẩm còn hàng</summary>
        public bool ConHang { get; set; }
        /// <summary>Lọc theo loại sản phẩm (danh mục) - dùng ở trang tìm kiếm</summary>
        public int? MaDM { get; set; }
        /// <summary>moi | gia-tang | gia-giam | ten | pho-bien</summary>
        public string SapXep { get; set; }
        /// <summary>Trang hiện tại (bắt đầu từ 1) - query string "Page"</summary>
        public int Page { get; set; } = 1;

        /// <summary>Số sản phẩm mỗi trang (2 hàng × 4 cột)</summary>
        public const int PageSize = 8;

        public bool DangLoc => GiaTu.HasValue || GiaDen.HasValue || ConHang || MaDM.HasValue;

        public static readonly (string Value, string Text)[] CacKieuSapXep =
        {
            ("moi", "Mới nhất"),
            ("pho-bien", "Bán chạy"),
            ("gia-tang", "Giá: thấp → cao"),
            ("gia-giam", "Giá: cao → thấp"),
            ("ten", "Tên A → Z")
        };
    }
}
