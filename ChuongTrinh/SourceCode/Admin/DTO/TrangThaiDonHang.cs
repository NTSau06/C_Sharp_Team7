using System;
using System.Collections.Generic;

namespace SportZoneAdmin.DTO
{
    /// <summary>Các trạng thái đơn hàng (6 trạng thái, khớp CHECK constraint trong CSDL - mục 2.7.2 báo cáo)</summary>
    public static class TrangThaiDonHang
    {
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DangGiao = "Đang giao";
        public const string DaGiao = "Đã giao hàng";
        public const string GiaoThatBai = "Giao thất bại";
        public const string DaHuy = "Đã hủy";

        public static readonly string[] TatCa =
        {
            ChoXacNhan, DaXacNhan, DangGiao, DaGiao, GiaoThatBai, DaHuy
        };

        /// <summary>Đơn đang chờ quản trị viên xác nhận / hủy</summary>
        public static bool LaChoXuLy(string tt)
        {
            return tt == ChoXacNhan;
        }
    }
}
