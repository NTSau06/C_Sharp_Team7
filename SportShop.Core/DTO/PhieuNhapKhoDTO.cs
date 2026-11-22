using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    public class PhieuNhapKhoDTO
    {
        public int MaPN { get; set; }
        public DateTime NgayNhap { get; set; }
        public decimal TongTienNhap { get; set; }
        public string GhiChu { get; set; }
        public int MaQTV { get; set; }
        public string TenQTV { get; set; }

        private List<ChiTietPhieuNhapDTO> chiTiet = new List<ChiTietPhieuNhapDTO>();
        public List<ChiTietPhieuNhapDTO> GetChiTiet() { return chiTiet; }
        public void SetChiTiet(List<ChiTietPhieuNhapDTO> list) { chiTiet = list ?? new List<ChiTietPhieuNhapDTO>(); }
    }
}
