using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportShopAdmin.DTO;

namespace SportShopAdmin.DAL
{
    public class KhoDAL
    {
        public List<SanPhamDTO> GetTonKho(string keyword, bool chiSapHet)
        {
            keyword = (keyword ?? "").Trim();
            string sql = SanPhamDAL.SelectSql +
                "WHERE (@kw = '' OR sp.TenSP LIKE @like OR CAST(sp.MaSP AS VARCHAR(20)) = @kw) " +
                "  AND (@sh = 0 OR sp.SoLuongTon <= sp.MucTonToiThieu) " +
                "ORDER BY CASE WHEN sp.SoLuongTon <= sp.MucTonToiThieu THEN 0 ELSE 1 END, sp.TenSP";
            DataTable dt = DBConnect.ExecuteQuery(sql,
                DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"),
                DBConnect.P("@sh", chiSapHet ? 1 : 0));
            var list = new List<SanPhamDTO>();
            foreach (DataRow r in dt.Rows) list.Add(SanPhamDAL.Map(r));
            return list;
        }

        /// <summary>Trừ tồn kho; trả về 0 nếu không đủ hàng (điều kiện SoLuongTon &gt;= SL)</summary>
        public int TruTonKho(SqlTransaction t, int maSP, int soLuong)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE SanPham SET SoLuongTon = SoLuongTon - @sl WHERE MaSP = @id AND SoLuongTon >= @sl",
                DBConnect.P("@sl", soLuong), DBConnect.P("@id", maSP));
        }

        public int CongTonKho(SqlTransaction t, int maSP, int soLuong)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE SanPham SET SoLuongTon = SoLuongTon + @sl WHERE MaSP = @id",
                DBConnect.P("@sl", soLuong), DBConnect.P("@id", maSP));
        }

        public int InsertPhieuNhap(SqlTransaction t, PhieuNhapKhoDTO pn)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(t,
                "INSERT INTO PhieuNhapKho(NgayNhap, TongTienNhap, GhiChu, MaQTV) VALUES(GETDATE(), @tong, @gc, @qtv); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);",
                DBConnect.P("@tong", pn.TongTienNhap), DBConnect.P("@gc", pn.GhiChu), DBConnect.P("@qtv", pn.MaQTV)));
        }

        public void InsertChiTietPhieuNhap(SqlTransaction t, int maPN, ChiTietPhieuNhapDTO ct)
        {
            DBConnect.ExecuteNonQuery(t,
                "INSERT INTO ChiTietPhieuNhap(MaPN, MaSP, SoLuongNhap, GiaNhap) VALUES(@pn, @sp, @sl, @gia)",
                DBConnect.P("@pn", maPN), DBConnect.P("@sp", ct.MaSP), DBConnect.P("@sl", ct.SoLuongNhap), DBConnect.P("@gia", ct.GiaNhap));
        }

        public List<PhieuNhapKhoDTO> GetPhieuNhap(DateTime tuNgay, DateTime denNgay)
        {
            string sql = "SELECT pn.MaPN, pn.NgayNhap, pn.TongTienNhap, pn.GhiChu, pn.MaQTV, q.HoTen AS TenQTV " +
                         "FROM PhieuNhapKho pn JOIN QuanTriVien q ON q.MaQTV = pn.MaQTV " +
                         "WHERE pn.NgayNhap >= @f AND pn.NgayNhap < @t ORDER BY pn.NgayNhap DESC";
            DataTable dt = DBConnect.ExecuteQuery(sql, DBConnect.PDate("@f", tuNgay.Date), DBConnect.PDate("@t", denNgay.Date.AddDays(1)));
            var list = new List<PhieuNhapKhoDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new PhieuNhapKhoDTO
                {
                    MaPN = r.Int("MaPN"),
                    NgayNhap = r.Date("NgayNhap"),
                    TongTienNhap = r.Dec("TongTienNhap"),
                    GhiChu = r.Str("GhiChu"),
                    MaQTV = r.Int("MaQTV"),
                    TenQTV = r.Str("TenQTV")
                });
            }
            return list;
        }

        public List<ChiTietPhieuNhapDTO> GetChiTietPhieuNhap(int maPN)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT ct.MaPN, ct.MaSP, sp.TenSP, ct.SoLuongNhap, ct.GiaNhap FROM ChiTietPhieuNhap ct " +
                "JOIN SanPham sp ON sp.MaSP = ct.MaSP WHERE ct.MaPN = @id ORDER BY sp.TenSP",
                DBConnect.P("@id", maPN));
            var list = new List<ChiTietPhieuNhapDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new ChiTietPhieuNhapDTO
                {
                    MaPN = r.Int("MaPN"),
                    MaSP = r.Int("MaSP"),
                    TenSP = r.Str("TenSP"),
                    SoLuongNhap = r.Int("SoLuongNhap"),
                    GiaNhap = r.Dec("GiaNhap")
                });
            }
            return list;
        }

        /// <summary>Lịch sử nhập (phiếu nhập) và xuất (đơn hàng đã xác nhận / đang giao / đã giao)</summary>
        public List<LichSuKhoDTO> GetLichSu(DateTime tuNgay, DateTime denNgay, string keyword)
        {
            keyword = (keyword ?? "").Trim();
            string sql =
                "SELECT * FROM ( " +
                "  SELECT pn.NgayNhap AS Ngay, N'Nhập kho' AS Loai, 'PN' + CAST(pn.MaPN AS VARCHAR(10)) AS ChungTu, " +
                "         ct.MaSP, sp.TenSP, ct.SoLuongNhap AS SoLuong, ct.GiaNhap AS DonGia " +
                "  FROM ChiTietPhieuNhap ct JOIN PhieuNhapKho pn ON pn.MaPN = ct.MaPN JOIN SanPham sp ON sp.MaSP = ct.MaSP " +
                "  UNION ALL " +
                "  SELECT dh.NgayDat, N'Xuất bán', 'DH' + CAST(dh.MaDH AS VARCHAR(10)), ct.MaSP, sp.TenSP, ct.SoLuong, ct.DonGia " +
                "  FROM ChiTietDonHang ct JOIN DonHang dh ON dh.MaDH = ct.MaDH JOIN SanPham sp ON sp.MaSP = ct.MaSP " +
                "  WHERE dh.TrangThai IN (N'Đã xác nhận', N'Đang giao', N'Đã giao hàng') " +
                ") x WHERE x.Ngay >= @f AND x.Ngay < @t AND (@kw = '' OR x.TenSP LIKE @like) ORDER BY x.Ngay DESC";
            DataTable dt = DBConnect.ExecuteQuery(sql,
                DBConnect.PDate("@f", tuNgay.Date), DBConnect.PDate("@t", denNgay.Date.AddDays(1)),
                DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"));
            var list = new List<LichSuKhoDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new LichSuKhoDTO
                {
                    Ngay = r.Date("Ngay"),
                    Loai = r.Str("Loai"),
                    ChungTu = r.Str("ChungTu"),
                    MaSP = r.Int("MaSP"),
                    TenSP = r.Str("TenSP"),
                    SoLuong = r.Int("SoLuong"),
                    DonGia = r.Dec("DonGia")
                });
            }
            return list;
        }
    }
}
