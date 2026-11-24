namespace SportZoneWeb.DTO
{
    /// <summary>Ánh xạ bảng DiaChiGiaoHang (sổ địa chỉ)</summary>
    public class DiaChiGiaoHangDTO
    {
        public int MaDC { get; set; }
        public int MaKH { get; set; }
        public string HoTenNhan { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public bool MacDinh { get; set; }
    }

    /// <summary>Ánh xạ bảng DanhGia (kèm HoTen khách khi JOIN KhachHang)</summary>
    public class DanhGiaDTO
    {
        public int MaDG { get; set; }
        public int MaSP { get; set; }
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public int SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; }
    }

    /// <summary>Ánh xạ bảng LichSuDonHang (mốc đổi trạng thái đơn hàng)</summary>
    public class LichSuDonHangDTO
    {
        public int MaDH { get; set; }
        public string TrangThai { get; set; }
        public DateTime ThoiGian { get; set; }
        public string GhiChu { get; set; }
    }
}
