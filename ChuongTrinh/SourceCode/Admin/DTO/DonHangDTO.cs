using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    public class DonHangDTO
    {
        public int MaDH { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public string DiaChiGiao { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongTien { get; set; }
        public string GhiChu { get; set; }
        public int MaKH { get; set; }
        public int MaPTTT { get; set; }
        public int? MaQTV { get; set; }
        public int? MaMGG { get; set; }

        // Thông tin hiển thị (JOIN)
        public string TenKH { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string TenPTTT { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string TenQTV { get; set; }
        public string CodeMGG { get; set; }

        /// <summary>Tạm tính trước giảm giá</summary>
        public decimal TamTinh => TongTien + TienGiam;
    }
}
