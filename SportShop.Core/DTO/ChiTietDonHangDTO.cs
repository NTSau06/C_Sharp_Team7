using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    public class ChiTietDonHangDTO
    {
        public int MaDH { get; set; }
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        /// <summary>Tồn kho hiện tại của sản phẩm (để kiểm tra khi xác nhận)</summary>
        public int SoLuongTon { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;
        public bool DuHang => SoLuongTon >= SoLuong;
    }
}
