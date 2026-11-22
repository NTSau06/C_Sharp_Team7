using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportShopAdmin.DAL;
using SportShopAdmin.DTO;
using SportShopAdmin.Utils;

namespace SportShopAdmin.BLL
{
    public class ThongKeBLL
    {
        private readonly ThongKeDAL dal = new ThongKeDAL();

        public TongQuanDTO TongQuan()
        {
            return dal.TongQuan();
        }

        /// <summary>Doanh thu theo kỳ, bổ sung các kỳ không phát sinh (doanh thu = 0) để vẽ biểu đồ liên tục</summary>
        public List<DoanhThuDTO> DoanhThuTheoKy(DateTime tuNgay, DateTime denNgay, KieuThongKe kieu)
        {
            KhoBLL.KiemTraKhoangNgay(tuNgay, denNgay);
            if (kieu == KieuThongKe.Ngay && (denNgay.Date - tuNgay.Date).TotalDays > 366)
                throw new BusinessException("Thống kê theo ngày chỉ hỗ trợ tối đa 1 năm. Hãy chọn theo tuần/tháng/quý.");

            Dictionary<DateTime, DoanhThuDTO> data = dal.DoanhThuTheoKy(tuNgay, denNgay, kieu).ToDictionary(x => x.TuNgay.Date);
            var result = new List<DoanhThuDTO>();
            for (DateTime ky = DauKy(tuNgay, kieu); ky <= denNgay.Date; ky = KyTiepTheo(ky, kieu))
            {
                DoanhThuDTO d;
                if (!data.TryGetValue(ky, out d)) d = new DoanhThuDTO { TuNgay = ky };
                d.Ky = TenKy(ky, kieu);
                result.Add(d);
            }
            return result;
        }

        public List<BaoCaoDoanhThuDTO> BaoCao(DateTime tuNgay, DateTime denNgay, TieuChiBaoCao tieuChi)
        {
            KhoBLL.KiemTraKhoangNgay(tuNgay, denNgay);
            return dal.BaoCao(tuNgay, denNgay, tieuChi);
        }

        public static DateTime DauKy(DateTime d, KieuThongKe kieu)
        {
            d = d.Date;
            switch (kieu)
            {
                case KieuThongKe.Ngay: return d;
                case KieuThongKe.Tuan: return d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
                case KieuThongKe.Thang: return new DateTime(d.Year, d.Month, 1);
                case KieuThongKe.Quy: return new DateTime(d.Year, (d.Month - 1) / 3 * 3 + 1, 1);
                default: return new DateTime(d.Year, 1, 1);
            }
        }

        private static DateTime KyTiepTheo(DateTime d, KieuThongKe kieu)
        {
            switch (kieu)
            {
                case KieuThongKe.Ngay: return d.AddDays(1);
                case KieuThongKe.Tuan: return d.AddDays(7);
                case KieuThongKe.Thang: return d.AddMonths(1);
                case KieuThongKe.Quy: return d.AddMonths(3);
                default: return d.AddYears(1);
            }
        }

        private static string TenKy(DateTime d, KieuThongKe kieu)
        {
            switch (kieu)
            {
                case KieuThongKe.Ngay: return d.ToString("dd/MM/yyyy");
                case KieuThongKe.Tuan: return "Tuần " + d.ToString("dd/MM/yyyy");
                case KieuThongKe.Thang: return d.ToString("MM/yyyy");
                case KieuThongKe.Quy: return "Quý " + ((d.Month - 1) / 3 + 1) + "/" + d.Year;
                default: return d.Year.ToString();
            }
        }
    }
}
