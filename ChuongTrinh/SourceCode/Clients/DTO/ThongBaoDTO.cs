namespace SportZoneWeb.DTO
{
    /// <summary>Ánh xạ bảng ThongBao (thông báo cho khách hàng)</summary>
    public class ThongBaoDTO
    {
        public int MaTB { get; set; }
        public int MaKH { get; set; }
        public int? MaDH { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public bool DaDoc { get; set; }
    }
}
