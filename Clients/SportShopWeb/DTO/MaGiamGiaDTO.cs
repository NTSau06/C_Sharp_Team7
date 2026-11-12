namespace SportShopWeb.DTO
{
    /// <summary>Ánh xạ bảng MaGiamGia</summary>
    public class MaGiamGiaDTO
    {
        public int MaMGG { get; set; }
        public string Code { get; set; }
        public int PhanTramGiam { get; set; }
        public decimal SoTienGiamToiDa { get; set; }
        public decimal DonToiThieu { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public int SoLuotToiDa { get; set; }
        public int SoLuotDaDung { get; set; }
        public bool TrangThai { get; set; }
    }

    /// <summary>Kết quả kiểm tra / áp dụng mã giảm giá</summary>
    public class ApDungMaResult
    {
        public bool HopLe { get; set; }
        public string ThongBao { get; set; }
        public MaGiamGiaDTO Ma { get; set; }
        public decimal TienGiam { get; set; }
    }
}
