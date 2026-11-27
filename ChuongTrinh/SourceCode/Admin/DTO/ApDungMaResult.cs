using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    /// <summary>Kết quả kiểm tra/áp dụng mã giảm giá (dùng chung cho Web)</summary>
    public class ApDungMaResult
    {
        public bool HopLe { get; set; }
        public string ThongBao { get; set; }
        public MaGiamGiaDTO Ma { get; set; }
        public decimal TienGiam { get; set; }
    }
}
