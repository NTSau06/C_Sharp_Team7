using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    /// <summary>Các trạng thái đơn hàng (khớp CHECK constraint trong CSDL)</summary>
    public static class TrangThaiDonHang
    {
        public const string ChoThanhToan = "Chờ thanh toán";
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DangGiao = "Đang giao";
        public const string DaGiao = "Đã giao";
        public const string GiaoThatBai = "Giao thất bại";
        public const string DaHuy = "Đã hủy";

        public static readonly string[] TatCa =
        {
            ChoThanhToan, ChoXacNhan, DaXacNhan, DangGiao, DaGiao, GiaoThatBai, DaHuy
        };

        public static bool LaChoXuLy(string tt)
        {
            return tt == ChoXacNhan || tt == ChoThanhToan;
        }
    }
}
