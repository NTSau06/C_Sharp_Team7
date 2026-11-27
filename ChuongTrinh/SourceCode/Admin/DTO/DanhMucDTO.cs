using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    public class DanhMucDTO
    {
        public int MaDM { get; set; }
        public string TenDM { get; set; }
        public string MoTa { get; set; }
        /// <summary>Số sản phẩm thuộc danh mục (chỉ dùng để hiển thị)</summary>
        public int SoSanPham { get; set; }
    }
}
