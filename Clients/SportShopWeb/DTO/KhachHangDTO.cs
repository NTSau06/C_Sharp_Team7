namespace SportShopWeb.DTO
{
    /// <summary>Ánh xạ bảng KhachHang</summary>
    public class KhachHangDTO
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public bool TrangThai { get; set; }
    }
}
