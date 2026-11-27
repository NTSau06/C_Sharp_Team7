using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
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

        /// <summary>Cảnh báo sắp hết hàng: SoLuongTon &lt;= MucTonToiThieu</summary>
        public bool SapHetHang()
        {
            return SoLuongTon <= MucTonToiThieu;
        }

        public string TinhTrangKho => SoLuongTon == 0 ? "Hết hàng" : (SapHetHang() ? "Sắp hết hàng" : "Còn hàng");

        public override string ToString()
        {
            return "#SP" + MaSP.ToString("D2") + " - " + TenSP;
        }
    }
}
