using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    public class DonHangDAL
    {
        private const string SelectSql =
            "SELECT dh.MaDH, dh.NgayDat, dh.TrangThai, dh.DiaChiGiao, dh.TienGiam, dh.TongTien, dh.GhiChu, dh.MaKH, dh.MaPTTT, dh.MaMGG, " +
            "       pt.TenPTTT, mgg.Code AS CodeMGG " +
            "FROM DonHang dh JOIN PhuongThucThanhToan pt ON pt.MaPTTT = dh.MaPTTT " +
            "LEFT JOIN MaGiamGia mgg ON mgg.MaMGG = dh.MaMGG ";

        private static DonHangDTO Map(DataRow r)
        {
            return new DonHangDTO
            {
                MaDH = r.Int("MaDH"),
                NgayDat = r.Date("NgayDat"),
                TrangThai = r.Str("TrangThai"),
                DiaChiGiao = r.Str("DiaChiGiao"),
                TienGiam = r.Dec("TienGiam"),
                TongTien = r.Dec("TongTien"),
                GhiChu = r.Str("GhiChu"),
                MaKH = r.Int("MaKH"),
                MaPTTT = r.Int("MaPTTT"),
                MaMGG = r.IntN("MaMGG"),
                TenPTTT = r.Str("TenPTTT"),
                CodeMGG = r.Str("CodeMGG")
            };
        }

        public int Insert(SqlTransaction t, DonHangDTO dh)
        {
            const string sql =
                "INSERT INTO DonHang(NgayDat, TrangThai, DiaChiGiao, TienGiam, TongTien, GhiChu, MaKH, MaPTTT, MaQTV, MaMGG) " +
                "VALUES(GETDATE(), @tt, @dc, @giam, @tong, @gc, @kh, @pt, NULL, @mgg); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return Convert.ToInt32(DBConnect.ExecuteScalar(t, sql,
                DBConnect.P("@tt", dh.TrangThai), DBConnect.P("@dc", dh.DiaChiGiao), DBConnect.P("@giam", dh.TienGiam),
                DBConnect.P("@tong", dh.TongTien), DBConnect.P("@gc", string.IsNullOrWhiteSpace(dh.GhiChu) ? null : dh.GhiChu),
                DBConnect.P("@kh", dh.MaKH), DBConnect.P("@pt", dh.MaPTTT), DBConnect.PInt("@mgg", dh.MaMGG)));
        }

        public void InsertChiTiet(SqlTransaction t, int maDH, int maSP, int soLuong, decimal donGia)
        {
            DBConnect.ExecuteNonQuery(t,
                "INSERT INTO ChiTietDonHang(MaDH, MaSP, SoLuong, DonGia) VALUES(@dh, @sp, @sl, @gia)",
                DBConnect.P("@dh", maDH), DBConnect.P("@sp", maSP), DBConnect.P("@sl", soLuong), DBConnect.P("@gia", donGia));
        }

        public DonHangDTO GetById(int maDH)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE dh.MaDH = @id", DBConnect.P("@id", maDH));
            if (dt.Rows.Count == 0) return null;
            DonHangDTO dh = Map(dt.Rows[0]);
            dh.ChiTiet = GetChiTiet(maDH);
            return dh;
        }

        public List<DonHangDTO> GetByKhachHang(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE dh.MaKH = @kh ORDER BY dh.NgayDat DESC", DBConnect.P("@kh", maKH));
            var list = new List<DonHangDTO>();
            foreach (DataRow r in dt.Rows)
            {
                DonHangDTO dh = Map(r);
                dh.ChiTiet = GetChiTiet(dh.MaDH);
                list.Add(dh);
            }
            return list;
        }

        public List<ChiTietDonHangDTO> GetChiTiet(int maDH)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT ct.MaDH, ct.MaSP, sp.TenSP, sp.HinhAnh, ct.SoLuong, ct.DonGia " +
                "FROM ChiTietDonHang ct JOIN SanPham sp ON sp.MaSP = ct.MaSP WHERE ct.MaDH = @id", DBConnect.P("@id", maDH));
            var list = new List<ChiTietDonHangDTO>();
            foreach (DataRow r in dt.Rows)
                list.Add(new ChiTietDonHangDTO
                {
                    MaDH = r.Int("MaDH"), MaSP = r.Int("MaSP"), TenSP = r.Str("TenSP"), HinhAnh = r.Str("HinhAnh"),
                    SoLuong = r.Int("SoLuong"), DonGia = r.Dec("DonGia")
                });
            return list;
        }
    }
}
