namespace SportZoneWeb.DTO
{
    /// <summary>Ánh xạ bảng DanhMuc (kèm số sản phẩm)</summary>
    public class DanhMucDTO
    {
        public int MaDM { get; set; }
        public string TenDM { get; set; }
        public string MoTa { get; set; }
        public int SoSanPham { get; set; }
    }
}
