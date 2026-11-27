using System.Data;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    /// <summary>Truy cập tài khoản khách hàng (đăng nhập / đăng ký trên Web)</summary>
    public class TaiKhoanDAL
    {
        private const string SelectKH = "SELECT MaKH, HoTen, TenDangNhap, MatKhau, SDT, Email, DiaChi, TrangThai FROM KhachHang ";

        private static KhachHangDTO Map(DataRow r)
        {
            return new KhachHangDTO
            {
                MaKH = r.Int("MaKH"),
                HoTen = r.Str("HoTen"),
                TenDangNhap = r.Str("TenDangNhap"),
                MatKhau = r.Str("MatKhau"),
                SDT = r.Str("SDT"),
                Email = r.Str("Email"),
                DiaChi = r.Str("DiaChi"),
                TrangThai = r.Bool("TrangThai")
            };
        }

        /// <summary>Tìm theo tên đăng nhập hoặc email (khách có thể đăng nhập bằng email)</summary>
        public KhachHangDTO GetKHByUsername(string tenDN)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectKH + "WHERE TenDangNhap = @u OR Email = @u", DBConnect.P("@u", tenDN));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public KhachHangDTO GetKHById(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectKH + "WHERE MaKH = @id", DBConnect.P("@id", maKH));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public bool ExistsUsername(string tenDN)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM KhachHang WHERE TenDangNhap = @u", DBConnect.P("@u", tenDN))) > 0;
        }

        public bool ExistsEmail(string email)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM KhachHang WHERE Email = @e", DBConnect.P("@e", email))) > 0;
        }

        public bool ExistsEmailExcept(string email, int maKH)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM KhachHang WHERE Email = @e AND MaKH <> @id",
                DBConnect.P("@e", email), DBConnect.P("@id", maKH))) > 0;
        }

        public bool ExistsSDT(string sdt)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM KhachHang WHERE SDT = @s", DBConnect.P("@s", sdt))) > 0;
        }

        public bool ExistsSDTExcept(string sdt, int maKH)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM KhachHang WHERE SDT = @s AND MaKH <> @id",
                DBConnect.P("@s", sdt), DBConnect.P("@id", maKH))) > 0;
        }

        public int InsertKhachHang(KhachHangDTO kh)
        {
            const string sql =
                "INSERT INTO KhachHang(HoTen, TenDangNhap, MatKhau, SDT, Email, DiaChi, TrangThai) " +
                "VALUES(@ten, @u, @mk, @sdt, @email, @dc, 1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql,
                DBConnect.P("@ten", kh.HoTen), DBConnect.P("@u", kh.TenDangNhap), DBConnect.P("@mk", kh.MatKhau),
                DBConnect.P("@sdt", kh.SDT), DBConnect.P("@email", kh.Email), DBConnect.P("@dc", kh.DiaChi)));
        }

        public bool UpdateProfile(KhachHangDTO kh)
        {
            const string sql =
                "UPDATE KhachHang SET HoTen = @ten, Email = @email, SDT = @sdt, DiaChi = @dc WHERE MaKH = @id";
            return DBConnect.ExecuteNonQuery(sql,
                DBConnect.P("@ten", kh.HoTen), DBConnect.P("@email", kh.Email),
                DBConnect.P("@sdt", kh.SDT), DBConnect.P("@dc", kh.DiaChi),
                DBConnect.P("@id", kh.MaKH)) > 0;
        }

        public bool UpdateDiaChi(int maKH, string diaChi)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE KhachHang SET DiaChi = @dc WHERE MaKH = @id",
                DBConnect.P("@dc", diaChi), DBConnect.P("@id", maKH)) > 0;
        }

        public bool UpdatePassword(int maKH, string matKhauHash)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE KhachHang SET MatKhau = @mk WHERE MaKH = @id",
                DBConnect.P("@mk", matKhauHash), DBConnect.P("@id", maKH)) > 0;
        }
    }
}
