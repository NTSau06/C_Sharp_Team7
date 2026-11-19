using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
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

        public MaGiamGiaDTO GetByCode(string code)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE Code = @c", DBConnect.P("@c", code));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public MaGiamGiaDTO GetByCode(SqlTransaction t, string code)
        {
            DataTable dt = DBConnect.ExecuteQuery(t, SelectSql + "WITH (UPDLOCK, ROWLOCK) WHERE Code = @c", DBConnect.P("@c", code));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        /// <summary>Các mã đang hiệu lực (hiển thị gợi ý ở trang giỏ hàng)</summary>
        public List<MaGiamGiaDTO> GetDangHieuLuc()
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql +
                "WHERE TrangThai = 1 AND GETDATE() BETWEEN NgayBatDau AND NgayKetThuc AND SoLuotDaDung < SoLuotToiDa ORDER BY DonToiThieu");
            var list = new List<MaGiamGiaDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        /// <summary>Tăng lượt dùng khi đặt hàng. Trả về 0 nếu mã vừa hết lượt / bị vô hiệu hóa.</summary>
        public int TangLuotDung(SqlTransaction t, int maMGG)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE MaGiamGia SET SoLuotDaDung = SoLuotDaDung + 1 WHERE MaMGG = @id AND TrangThai = 1 AND SoLuotDaDung < SoLuotToiDa",
                DBConnect.P("@id", maMGG));
        }
    }
}
