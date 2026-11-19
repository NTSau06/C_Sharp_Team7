namespace SportZoneWeb.DTO
{
    /// <summary>Ánh xạ bảng DonHang</summary>
    public class DonHangDTO
    {
        public int MaDH { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public string DiaChiGiao { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; }
        public int MaKH { get; set; }
        public int MaPTTT { get; set; }
        public int? MaMGG { get; set; }

        // Thông tin hiển thị (JOIN)
        public string TenPTTT { get; set; }
        public string CodeMGG { get; set; }
        public List<ChiTietDonHangDTO> ChiTiet { get; set; } = new List<ChiTietDonHangDTO>();

        public decimal TamTinh => TongTien + TienGiam;
    }

    /// <summary>Ánh xạ bảng ChiTietDonHang</summary>
    public class ChiTietDonHangDTO
    {
        public int MaDH { get; set; }
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    /// <summary>Các trạng thái đơn hàng (khớp CHECK constraint trong CSDL và phân hệ Admin)</summary>
    public static class TrangThaiDonHang
    {
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DangGiao = "Đang giao";
        public const string DaGiao = "Đã giao hàng";
        public const string GiaoThatBai = "Giao thất bại";
        public const string DaHuy = "Đã hủy";

        /// <summary>6 trạng thái theo thứ tự xử lý (mục 2.7.2) - dùng cho bộ lọc "Đơn hàng của tôi"</summary>
        public static readonly string[] TatCa = { ChoXacNhan, DaXacNhan, DangGiao, DaGiao, GiaoThatBai, DaHuy };

        public static string CssClass(string tt)
        {
            switch (tt)
            {
                case DaGiao: return "st-ok";
                case DaHuy:
                case GiaoThatBai: return "st-bad";
                case DangGiao:
                case DaXacNhan: return "st-info";
                default: return "st-wait";
            }
        }
    }
}
