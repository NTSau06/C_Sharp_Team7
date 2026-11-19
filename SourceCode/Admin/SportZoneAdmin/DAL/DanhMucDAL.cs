using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class DanhMucDAL
    {
        public List<DanhMucDTO> GetAll(string keyword)
        {
            keyword = (keyword ?? "").Trim();
            string sql =
                "SELECT dm.MaDM, dm.TenDM, dm.MoTa, " +
                "       (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaDM = dm.MaDM) AS SoSanPham " +
                "FROM DanhMuc dm WHERE (@kw = '' OR dm.TenDM LIKE @like) ORDER BY dm.TenDM";
            DataTable dt = DBConnect.ExecuteQuery(sql, DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"));
            var list = new List<DanhMucDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new DanhMucDTO
                {
                    MaDM = r.Int("MaDM"),
                    TenDM = r.Str("TenDM"),
                    MoTa = r.Str("MoTa"),
                    SoSanPham = r.Int("SoSanPham")
                });
            }
            return list;
        }

        public int Insert(DanhMucDTO dm)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(
                "INSERT INTO DanhMuc(TenDM, MoTa) VALUES(@ten, @mota); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                DBConnect.P("@ten", dm.TenDM), DBConnect.P("@mota", dm.MoTa)));
        }

        public bool Update(DanhMucDTO dm)
        {
            return DBConnect.ExecuteNonQuery("UPDATE DanhMuc SET TenDM = @ten, MoTa = @mota WHERE MaDM = @id",
                DBConnect.P("@ten", dm.TenDM), DBConnect.P("@mota", dm.MoTa), DBConnect.P("@id", dm.MaDM)) > 0;
        }

        public bool Delete(int maDM)
        {
            return DBConnect.ExecuteNonQuery("DELETE FROM DanhMuc WHERE MaDM = @id", DBConnect.P("@id", maDM)) > 0;
        }

        public bool ExistsTen(string ten, int excludeMaDM)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM DanhMuc WHERE TenDM = @ten AND MaDM <> @id",
                DBConnect.P("@ten", ten), DBConnect.P("@id", excludeMaDM))) > 0;
        }

        public int CountSanPham(int maDM)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM SanPham WHERE MaDM = @id", DBConnect.P("@id", maDM)));
        }
    }
}
