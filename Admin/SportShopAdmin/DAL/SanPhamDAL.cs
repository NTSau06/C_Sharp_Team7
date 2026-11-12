using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportShopAdmin.DTO;

namespace SportShopAdmin.DAL
{
    public class SanPhamDAL
    {
        internal const string SelectSql =
            "SELECT sp.MaSP, sp.TenSP, sp.GiaBan, sp.SoLuongTon, sp.MucTonToiThieu, sp.HinhAnh, sp.MoTa, sp.MaDM, dm.TenDM " +
            "FROM SanPham sp JOIN DanhMuc dm ON dm.MaDM = sp.MaDM ";

        internal static SanPhamDTO Map(DataRow r)
        {
            return new SanPhamDTO
            {
                MaSP = r.Int("MaSP"),
                TenSP = r.Str("TenSP"),
                GiaBan = r.Dec("GiaBan"),
                SoLuongTon = r.Int("SoLuongTon"),
                MucTonToiThieu = r.Int("MucTonToiThieu"),
                HinhAnh = r.Str("HinhAnh"),
                MoTa = r.Str("MoTa"),
                MaDM = r.Int("MaDM"),
                TenDM = r.Str("TenDM")
            };
        }

        public List<SanPhamDTO> GetAll(string keyword, int? maDM)
        {
            keyword = (keyword ?? "").Trim();
            string sql = SelectSql +
                "WHERE (@kw = '' OR sp.TenSP LIKE @like OR CAST(sp.MaSP AS VARCHAR(20)) = @kw) " +
                "  AND (@dm IS NULL OR sp.MaDM = @dm) ORDER BY sp.MaSP DESC";
            DataTable dt = DBConnect.ExecuteQuery(sql,
                DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"), DBConnect.PInt("@dm", maDM));
            var list = new List<SanPhamDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public SanPhamDTO GetById(int maSP)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE sp.MaSP = @id", DBConnect.P("@id", maSP));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public int Insert(SanPhamDTO sp)
        {
            string sql = "INSERT INTO SanPham(TenSP, GiaBan, SoLuongTon, MucTonToiThieu, HinhAnh, MoTa, MaDM) " +
                         "VALUES(@ten, @gia, @ton, @min, @anh, @mota, @dm); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql,
                DBConnect.P("@ten", sp.TenSP), DBConnect.P("@gia", sp.GiaBan), DBConnect.P("@ton", sp.SoLuongTon),
                DBConnect.P("@min", sp.MucTonToiThieu), DBConnect.P("@anh", sp.HinhAnh), DBConnect.P("@mota", sp.MoTa),
                DBConnect.P("@dm", sp.MaDM)));
        }

        /// <summary>Cập nhật thông tin sản phẩm (không đổi SoLuongTon - tồn kho chỉ thay đổi qua nhập kho / xác nhận đơn)</summary>
        public bool Update(SanPhamDTO sp)
        {
            string sql = "UPDATE SanPham SET TenSP = @ten, GiaBan = @gia, MucTonToiThieu = @min, HinhAnh = @anh, " +
                         "MoTa = @mota, MaDM = @dm WHERE MaSP = @id";
            return DBConnect.ExecuteNonQuery(sql,
                DBConnect.P("@ten", sp.TenSP), DBConnect.P("@gia", sp.GiaBan), DBConnect.P("@min", sp.MucTonToiThieu),
                DBConnect.P("@anh", sp.HinhAnh), DBConnect.P("@mota", sp.MoTa), DBConnect.P("@dm", sp.MaDM),
                DBConnect.P("@id", sp.MaSP)) > 0;
        }

        public bool Delete(int maSP)
        {
            return DBConnect.ExecuteNonQuery(
                "DELETE FROM GioHang WHERE MaSP = @id; DELETE FROM SanPham WHERE MaSP = @id;",
                DBConnect.P("@id", maSP)) > 0;
        }

        /// <summary>Số chứng từ (đơn hàng, phiếu nhập) có tham chiếu tới sản phẩm</summary>
        public int CountReferences(int maSP)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(
                "SELECT (SELECT COUNT(*) FROM ChiTietDonHang WHERE MaSP = @id) + (SELECT COUNT(*) FROM ChiTietPhieuNhap WHERE MaSP = @id)",
                DBConnect.P("@id", maSP)));
        }
    }
}
