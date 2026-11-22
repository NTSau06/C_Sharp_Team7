using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    /// <summary>
    /// Sáu trạng thái đơn hàng đã chuẩn hóa (báo cáo mục 2.7.2, khớp CHECK constraint trong CSDL).
    /// Đơn được tạo một lần ở trạng thái "Chờ xác nhận" (không còn trạng thái "Chờ thanh toán").
    /// </summary>
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

        /// <summary>Trạng thái kết thúc, không được chuyển tiếp (Bảng 2.35)</summary>
        public static bool LaKetThuc(string tt)
        {
            return tt == DaGiao || tt == GiaoThatBai || tt == DaHuy;
        }
    }
}
