using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
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
        /// <summary>true: kích hoạt, false: vô hiệu hóa</summary>
        public bool TrangThai { get; set; }

        public string TinhTrang
        {
            get
            {
                if (!TrangThai) return "Vô hiệu hóa";
                DateTime now = DateTime.Now;
                if (now < NgayBatDau) return "Chưa bắt đầu";
                if (now > NgayKetThuc) return "Hết hạn";
                if (SoLuotDaDung >= SoLuotToiDa) return "Hết lượt";
                return "Đang hiệu lực";
            }
        }

        public bool DangHieuLuc => TinhTrang == "Đang hiệu lực";
    }
}
