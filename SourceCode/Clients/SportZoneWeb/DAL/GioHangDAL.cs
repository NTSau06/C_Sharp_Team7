using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    /// <summary>Giỏ hàng lưu trong bảng GioHang (theo khách hàng đã đăng nhập)</summary>
    public class GioHangDAL
    {
        public List<GioHangDTO> GetByKhachHang(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery("SELECT MaKH, MaSP, SoLuong FROM GioHang WHERE MaKH = @kh", DBConnect.P("@kh", maKH));
            var list = new List<GioHangDTO>();
            foreach (DataRow r in dt.Rows)
                list.Add(new GioHangDTO { MaKH = r.Int("MaKH"), MaSP = r.Int("MaSP"), SoLuong = r.Int("SoLuong") });
            return list;
        }

        /// <summary>Danh sách giỏ kèm thông tin sản phẩm (để hiển thị)</summary>
        public DataTable GetChiTiet(int maKH)
        {
            return DBConnect.ExecuteQuery(
                "SELECT gh.MaSP, gh.SoLuong, sp.TenSP, sp.HinhAnh, sp.GiaBan, sp.SoLuongTon, sp.MaDM, dm.TenDM " +
                "FROM GioHang gh JOIN SanPham sp ON sp.MaSP = gh.MaSP JOIN DanhMuc dm ON dm.MaDM = sp.MaDM " +
                "WHERE gh.MaKH = @kh ORDER BY sp.TenSP", DBConnect.P("@kh", maKH));
        }

        public int GetSoLuong(int maKH, int maSP)
        {
            object o = DBConnect.ExecuteScalar("SELECT SoLuong FROM GioHang WHERE MaKH = @kh AND MaSP = @sp",
                DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Tổng số lượng sản phẩm trong giỏ (hiển thị trên biểu tượng giỏ hàng)</summary>
        public int Count(int maKH)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT ISNULL(SUM(SoLuong), 0) FROM GioHang WHERE MaKH = @kh", DBConnect.P("@kh", maKH)));
        }

        /// <summary>Thêm mới hoặc ghi đè số lượng</summary>
        public bool Upsert(int maKH, int maSP, int sl)
        {
            const string sql =
                "IF EXISTS (SELECT 1 FROM GioHang WHERE MaKH = @kh AND MaSP = @sp) " +
                "  UPDATE GioHang SET SoLuong = @sl WHERE MaKH = @kh AND MaSP = @sp " +
                "ELSE INSERT INTO GioHang(MaKH, MaSP, SoLuong) VALUES(@kh, @sp, @sl)";
            return DBConnect.ExecuteNonQuery(sql, DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP), DBConnect.P("@sl", sl)) > 0;
        }

        public bool Delete(int maKH, int maSP)
        {
            return DBConnect.ExecuteNonQuery("DELETE FROM GioHang WHERE MaKH = @kh AND MaSP = @sp",
                DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP)) > 0;
        }

        public bool Clear(int maKH, SqlTransaction t)
        {
            return DBConnect.ExecuteNonQuery(t, "DELETE FROM GioHang WHERE MaKH = @kh", DBConnect.P("@kh", maKH)) > 0;
        }
    }
}
