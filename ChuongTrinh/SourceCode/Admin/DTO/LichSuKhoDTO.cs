using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    /// <summary>Một dòng lịch sử nhập / xuất kho</summary>
    public class LichSuKhoDTO
    {
        public DateTime Ngay { get; set; }
        public string Loai { get; set; }
        public string ChungTu { get; set; }
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}
