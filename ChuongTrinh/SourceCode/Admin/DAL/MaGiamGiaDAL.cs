using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class MaGiamGiaDAL
    {
        private const string SelectSql =
            "SELECT MaMGG, Code, PhanTramGiam, SoTienGiamToiDa, DonToiThieu, NgayBatDau, NgayKetThuc, " +
            "       SoLuotToiDa, SoLuotDaDung, TrangThai FROM MaGiamGia ";

        private static MaGiamGiaDTO Map(DataRow r)
        {
            return new MaGiamGiaDTO
            {
                MaMGG = r.Int("MaMGG"),
                Code = r.Str("Code"),
                PhanTramGiam = r.Int("PhanTramGiam"),
                SoTienGiamToiDa = r.Dec("SoTienGiamToiDa"),
                DonToiThieu = r.Dec("DonToiThieu"),
                NgayBatDau = r.Date("NgayBatDau"),
                NgayKetThuc = r.Date("NgayKetThuc"),
                SoLuotToiDa = r.Int("SoLuotToiDa"),
                SoLuotDaDung = r.Int("SoLuotDaDung"),
                TrangThai = r.Bool("TrangThai")
            };
        }

        public List<MaGiamGiaDTO> GetAll(string keyword)
        {
            keyword = (keyword ?? "").Trim();
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE (@kw = '' OR Code LIKE @like) ORDER BY MaMGG DESC",
                DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"));
            var list = new List<MaGiamGiaDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public MaGiamGiaDTO GetById(int maMGG)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE MaMGG = @id", DBConnect.P("@id", maMGG));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public MaGiamGiaDTO GetByCode(string code)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE Code = @c", DBConnect.P("@c", code));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public int Insert(MaGiamGiaDTO m)
        {
            string sql = "INSERT INTO MaGiamGia(Code, PhanTramGiam, SoTienGiamToiDa, DonToiThieu, NgayBatDau, NgayKetThuc, SoLuotToiDa, SoLuotDaDung, TrangThai) " +
                         "VALUES(@code, @pt, @max, @min, @bd, @kt, @luot, 0, @tt); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql,
                DBConnect.P("@code", m.Code), DBConnect.P("@pt", m.PhanTramGiam), DBConnect.P("@max", m.SoTienGiamToiDa),
                DBConnect.P("@min", m.DonToiThieu), DBConnect.PDate("@bd", m.NgayBatDau), DBConnect.PDate("@kt", m.NgayKetThuc),
                DBConnect.P("@luot", m.SoLuotToiDa), DBConnect.P("@tt", m.TrangThai)));
        }

        public bool Update(MaGiamGiaDTO m)
        {
            string sql = "UPDATE MaGiamGia SET Code = @code, PhanTramGiam = @pt, SoTienGiamToiDa = @max, DonToiThieu = @min, " +
                         "NgayBatDau = @bd, NgayKetThuc = @kt, SoLuotToiDa = @luot, TrangThai = @tt WHERE MaMGG = @id";
            return DBConnect.ExecuteNonQuery(sql,
                DBConnect.P("@code", m.Code), DBConnect.P("@pt", m.PhanTramGiam), DBConnect.P("@max", m.SoTienGiamToiDa),
                DBConnect.P("@min", m.DonToiThieu), DBConnect.PDate("@bd", m.NgayBatDau), DBConnect.PDate("@kt", m.NgayKetThuc),
                DBConnect.P("@luot", m.SoLuotToiDa), DBConnect.P("@tt", m.TrangThai), DBConnect.P("@id", m.MaMGG)) > 0;
        }

        public bool Delete(int maMGG)
        {
            return DBConnect.ExecuteNonQuery("DELETE FROM MaGiamGia WHERE MaMGG = @id", DBConnect.P("@id", maMGG)) > 0;
        }

        public bool SetTrangThai(int maMGG, bool kichHoat)
        {
            return DBConnect.ExecuteNonQuery("UPDATE MaGiamGia SET TrangThai = @tt WHERE MaMGG = @id",
                DBConnect.P("@tt", kichHoat), DBConnect.P("@id", maMGG)) > 0;
        }

        public bool ExistsCode(string code, int excludeMaMGG)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM MaGiamGia WHERE Code = @c AND MaMGG <> @id",
                DBConnect.P("@c", code), DBConnect.P("@id", excludeMaMGG))) > 0;
        }

        public int CountDonHang(int maMGG)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM DonHang WHERE MaMGG = @id", DBConnect.P("@id", maMGG)));
        }

        /// <summary>Tăng lượt dùng (Web - khi đặt hàng). Trả về 0 nếu mã vừa hết lượt.</summary>
        public int TangLuotDung(SqlTransaction t, int maMGG)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE MaGiamGia SET SoLuotDaDung = SoLuotDaDung + 1 WHERE MaMGG = @id AND TrangThai = 1 AND SoLuotDaDung < SoLuotToiDa",
                DBConnect.P("@id", maMGG));
        }

        /// <summary>Hoàn lượt dùng khi đơn hàng bị hủy</summary>
        public int GiamLuotDung(SqlTransaction t, int maMGG)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE MaGiamGia SET SoLuotDaDung = SoLuotDaDung - 1 WHERE MaMGG = @id AND SoLuotDaDung > 0",
                DBConnect.P("@id", maMGG));
        }
    }
}
