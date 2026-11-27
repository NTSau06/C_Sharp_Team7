using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    public class ChiTietPhieuNhapDTO
    {
        public int MaPN { get; set; }
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public int SoLuongNhap { get; set; }
        public decimal GiaNhap { get; set; }

        public decimal ThanhTien => SoLuongNhap * GiaNhap;
    }
}
