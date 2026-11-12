using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    public enum KieuThongKe
    {
        Ngay,
        Tuan,
        Thang,
        Quy,
        Nam
    }

    public enum TieuChiBaoCao
    {
        SanPham,
        DanhMuc,
        KhachHang,
        PhuongThucThanhToan
    }

    /// <summary>Doanh thu của một kỳ (ngày/tuần/tháng/quý/năm)</summary>
    public class DoanhThuDTO
    {
        public DateTime TuNgay { get; set; }
        public string Ky { get; set; }
        public int SoDon { get; set; }
        /// <summary>Thực thu (tổng tiền các đơn đã giao)</summary>
        public decimal DoanhThu { get; set; }
        public decimal TienGiam { get; set; }
        public decimal DoanhThuGop => DoanhThu + TienGiam;
    }

    /// <summary>Một dòng báo cáo doanh thu chi tiết theo tiêu chí</summary>
    public class BaoCaoDoanhThuDTO
    {
        public string Nhom { get; set; }
        public int SoDon { get; set; }
        public int SoLuongBan { get; set; }
        public decimal DoanhThuGop { get; set; }
        public decimal ChietKhau { get; set; }
        public decimal ThucThu => DoanhThuGop - ChietKhau;
    }

    public class TongQuanDTO
    {
        public int DonChoXacNhan { get; set; }
        public int SanPhamSapHet { get; set; }
        public decimal DoanhThuThangNay { get; set; }
        public int SoKhachHang { get; set; }
    }
}
