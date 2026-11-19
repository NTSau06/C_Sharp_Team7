using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class ThongKeDAL
    {
        private const string DaGiao = "N'Đã giao hàng'";

        private static string KyExpression(KieuThongKe kieu)
        {
            switch (kieu)
            {
                case KieuThongKe.Ngay:
                    return "CONVERT(date, dh.NgayDat)";
                case KieuThongKe.Tuan: // Ngày thứ Hai đầu tuần
                    return "DATEADD(DAY, -((DATEPART(WEEKDAY, dh.NgayDat) + @@DATEFIRST - 2) % 7), CONVERT(date, dh.NgayDat))";
                case KieuThongKe.Thang:
                    return "DATEFROMPARTS(YEAR(dh.NgayDat), MONTH(dh.NgayDat), 1)";
                case KieuThongKe.Quy:
                    return "DATEFROMPARTS(YEAR(dh.NgayDat), (DATEPART(QUARTER, dh.NgayDat) - 1) * 3 + 1, 1)";
                default:
                    return "DATEFROMPARTS(YEAR(dh.NgayDat), 1, 1)";
            }
        }

        /// <summary>Doanh thu các đơn "Đã giao hàng" gom theo kỳ (chỉ những kỳ có phát sinh)</summary>
        public List<DoanhThuDTO> DoanhThuTheoKy(DateTime tuNgay, DateTime denNgay, KieuThongKe kieu)
        {
            string ky = KyExpression(kieu);
            string sql = "SELECT " + ky + " AS Ky, COUNT(*) AS SoDon, SUM(dh.TongTien) AS DoanhThu, SUM(dh.TienGiam) AS TienGiam " +
                         "FROM DonHang dh WHERE dh.TrangThai = " + DaGiao + " AND dh.NgayDat >= @f AND dh.NgayDat < @t " +
                         "GROUP BY " + ky + " ORDER BY Ky";
            DataTable dt = DBConnect.ExecuteQuery(sql, DBConnect.PDate("@f", tuNgay.Date), DBConnect.PDate("@t", denNgay.Date.AddDays(1)));
            var list = new List<DoanhThuDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new DoanhThuDTO
                {
                    TuNgay = r.Date("Ky"),
                    SoDon = r.Int("SoDon"),
                    DoanhThu = r.Dec("DoanhThu"),
                    TienGiam = r.Dec("TienGiam")
                });
            }
            return list;
        }

        public List<BaoCaoDoanhThuDTO> BaoCao(DateTime tuNgay, DateTime denNgay, TieuChiBaoCao tieuChi)
        {
            string where = "WHERE dh.TrangThai = " + DaGiao + " AND dh.NgayDat >= @f AND dh.NgayDat < @t ";
            // Chiết khấu của đơn được phân bổ cho từng dòng theo tỉ lệ giá trị dòng / tạm tính
            const string chietKhauDong = "SUM(ISNULL(ct.SoLuong * ct.DonGia * dh.TienGiam / NULLIF(dh.TongTien + dh.TienGiam, 0), 0))";
            string sql;
            switch (tieuChi)
            {
                case TieuChiBaoCao.SanPham:
                    sql = "SELECT sp.TenSP AS Nhom, COUNT(DISTINCT dh.MaDH) AS SoDon, SUM(ct.SoLuong) AS SoLuongBan, " +
                          "SUM(ct.SoLuong * ct.DonGia) AS DoanhThuGop, " + chietKhauDong + " AS ChietKhau " +
                          "FROM ChiTietDonHang ct JOIN DonHang dh ON dh.MaDH = ct.MaDH JOIN SanPham sp ON sp.MaSP = ct.MaSP " +
                          where + "GROUP BY sp.MaSP, sp.TenSP ORDER BY DoanhThuGop DESC";
                    break;
                case TieuChiBaoCao.DanhMuc:
                    sql = "SELECT dm.TenDM AS Nhom, COUNT(DISTINCT dh.MaDH) AS SoDon, SUM(ct.SoLuong) AS SoLuongBan, " +
                          "SUM(ct.SoLuong * ct.DonGia) AS DoanhThuGop, " + chietKhauDong + " AS ChietKhau " +
                          "FROM ChiTietDonHang ct JOIN DonHang dh ON dh.MaDH = ct.MaDH JOIN SanPham sp ON sp.MaSP = ct.MaSP " +
                          "JOIN DanhMuc dm ON dm.MaDM = sp.MaDM " +
                          where + "GROUP BY dm.MaDM, dm.TenDM ORDER BY DoanhThuGop DESC";
                    break;
                case TieuChiBaoCao.KhachHang:
                    sql = "SELECT kh.HoTen + ' (' + kh.TenDangNhap + ')' AS Nhom, COUNT(*) AS SoDon, SUM(ISNULL(s.SL, 0)) AS SoLuongBan, " +
                          "SUM(dh.TongTien + dh.TienGiam) AS DoanhThuGop, SUM(dh.TienGiam) AS ChietKhau " +
                          "FROM DonHang dh JOIN KhachHang kh ON kh.MaKH = dh.MaKH " +
                          "LEFT JOIN (SELECT MaDH, SUM(SoLuong) AS SL FROM ChiTietDonHang GROUP BY MaDH) s ON s.MaDH = dh.MaDH " +
                          where + "GROUP BY kh.MaKH, kh.HoTen, kh.TenDangNhap ORDER BY DoanhThuGop DESC";
                    break;
                default:
                    sql = "SELECT pt.TenPTTT AS Nhom, COUNT(*) AS SoDon, SUM(ISNULL(s.SL, 0)) AS SoLuongBan, " +
                          "SUM(dh.TongTien + dh.TienGiam) AS DoanhThuGop, SUM(dh.TienGiam) AS ChietKhau " +
                          "FROM DonHang dh JOIN PhuongThucThanhToan pt ON pt.MaPTTT = dh.MaPTTT " +
                          "LEFT JOIN (SELECT MaDH, SUM(SoLuong) AS SL FROM ChiTietDonHang GROUP BY MaDH) s ON s.MaDH = dh.MaDH " +
                          where + "GROUP BY pt.MaPTTT, pt.TenPTTT ORDER BY DoanhThuGop DESC";
                    break;
            }
            DataTable dt = DBConnect.ExecuteQuery(sql, DBConnect.PDate("@f", tuNgay.Date), DBConnect.PDate("@t", denNgay.Date.AddDays(1)));
            var list = new List<BaoCaoDoanhThuDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new BaoCaoDoanhThuDTO
                {
                    Nhom = r.Str("Nhom"),
                    SoDon = r.Int("SoDon"),
                    SoLuongBan = r.Int("SoLuongBan"),
                    DoanhThuGop = r.Dec("DoanhThuGop"),
                    ChietKhau = Math.Round(r.Dec("ChietKhau"), 0)
                });
            }
            return list;
        }

        public TongQuanDTO TongQuan()
        {
            string sql =
                "SELECT (SELECT COUNT(*) FROM DonHang WHERE TrangThai = N'Chờ xác nhận') AS DonCho, " +
                "       (SELECT COUNT(*) FROM SanPham WHERE SoLuongTon <= MucTonToiThieu) AS SapHet, " +
                "       (SELECT ISNULL(SUM(TongTien), 0) FROM DonHang WHERE TrangThai = " + DaGiao +
                "          AND NgayDat >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS DoanhThuThang, " +
                "       (SELECT COUNT(*) FROM KhachHang WHERE TrangThai = 1) AS SoKH";
            DataRow r = DBConnect.ExecuteQuery(sql).Rows[0];
            return new TongQuanDTO
            {
                DonChoXacNhan = r.Int("DonCho"),
                SanPhamSapHet = r.Int("SapHet"),
                DoanhThuThangNay = r.Dec("DoanhThuThang"),
                SoKhachHang = r.Int("SoKH")
            };
        }
    }
}
