namespace SportZoneWeb.DTO
{
    /// <summary>Ánh xạ bảng SanPham (kèm TenDM khi JOIN DanhMuc)</summary>
    public class SanPhamDTO
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }
        public int MucTonToiThieu { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public int MaDM { get; set; }
        public string TenDM { get; set; }
        /// <summary>Tổng số lượng đã bán (đơn Đã giao) - dùng để sắp xếp "phổ biến"</summary>
        public int DaBan { get; set; }

        public bool HetHang => SoLuongTon <= 0;
        public bool SapHetHang => SoLuongTon > 0 && SoLuongTon <= MucTonToiThieu;
        public string TinhTrang => HetHang ? "Hết hàng" : (SapHetHang ? "Sắp hết hàng" : "Còn hàng");
    }
}
