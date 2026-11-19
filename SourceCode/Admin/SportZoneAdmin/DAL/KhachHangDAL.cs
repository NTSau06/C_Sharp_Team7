using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class KhachHangDAL
    {
        private const string SelectSql =
            "SELECT MaKH, HoTen, TenDangNhap, MatKhau, SDT, Email, DiaChi, TrangThai FROM KhachHang ";

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

        public List<KhachHangDTO> Search(string keyword, bool includeDeleted)
        {
            keyword = (keyword ?? "").Trim();
            string sql = SelectSql +
                "WHERE (@kw = '' OR HoTen LIKE @like OR TenDangNhap LIKE @like OR SDT LIKE @like OR Email LIKE @like " +
                "       OR CAST(MaKH AS VARCHAR(20)) = @kw) " +
                "  AND (@all = 1 OR TrangThai = 1) ORDER BY MaKH DESC";
            DataTable dt = DBConnect.ExecuteQuery(sql,
                DBConnect.PText("@kw", keyword),
                DBConnect.PText("@like", "%" + keyword + "%"),
                DBConnect.P("@all", includeDeleted ? 1 : 0));
            var list = new List<KhachHangDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public KhachHangDTO GetById(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE MaKH = @id", DBConnect.P("@id", maKH));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        public int Insert(KhachHangDTO kh)
        {
            string sql = "INSERT INTO KhachHang(HoTen, TenDangNhap, MatKhau, SDT, Email, DiaChi, TrangThai) " +
                         "VALUES(@ten, @tdn, @mk, @sdt, @email, @dc, 1); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql,
                DBConnect.P("@ten", kh.HoTen), DBConnect.P("@tdn", kh.TenDangNhap), DBConnect.P("@mk", kh.MatKhau),
                DBConnect.P("@sdt", kh.SDT), DBConnect.P("@email", kh.Email), DBConnect.P("@dc", kh.DiaChi)));
        }

        /// <summary>Cập nhật hồ sơ. Nếu kh.MatKhau rỗng thì giữ nguyên mật khẩu cũ.</summary>
        public bool Update(KhachHangDTO kh)
        {
            bool doiMatKhau = !string.IsNullOrEmpty(kh.MatKhau);
            string sql = "UPDATE KhachHang SET HoTen = @ten, TenDangNhap = @tdn, SDT = @sdt, Email = @email, DiaChi = @dc" +
                         (doiMatKhau ? ", MatKhau = @mk" : "") + " WHERE MaKH = @id";
            var ps = new List<SqlParameter>
            {
                DBConnect.P("@ten", kh.HoTen), DBConnect.P("@tdn", kh.TenDangNhap), DBConnect.P("@sdt", kh.SDT),
                DBConnect.P("@email", kh.Email), DBConnect.P("@dc", kh.DiaChi), DBConnect.P("@id", kh.MaKH)
            };
            if (doiMatKhau) ps.Add(DBConnect.P("@mk", kh.MatKhau));
            return DBConnect.ExecuteNonQuery(sql, ps.ToArray()) > 0;
        }

        /// <summary>Xóa mềm: TrangThai = 0 (bảo toàn lịch sử đơn hàng), đồng thời làm trống giỏ hàng</summary>
        public bool SoftDelete(int maKH)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE KhachHang SET TrangThai = 0 WHERE MaKH = @id; DELETE FROM GioHang WHERE MaKH = @id;",
                DBConnect.P("@id", maKH)) > 0;
        }

        public bool Restore(int maKH)
        {
            return DBConnect.ExecuteNonQuery("UPDATE KhachHang SET TrangThai = 1 WHERE MaKH = @id",
                DBConnect.P("@id", maKH)) > 0;
        }

        /// <summary>Kiểm tra trùng giá trị ở cột TenDangNhap / Email / SDT (loại trừ chính khách hàng đang sửa)</summary>
        public bool Exists(string column, string value, int excludeMaKH)
        {
            if (column != "TenDangNhap" && column != "Email" && column != "SDT")
                throw new ArgumentException("Cột không hợp lệ", "column");
            string sql = "SELECT COUNT(*) FROM KhachHang WHERE " + column + " = @v AND MaKH <> @id";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql, DBConnect.P("@v", value), DBConnect.P("@id", excludeMaKH))) > 0;
        }

        public int CountDonChuaHoanTat(int maKH)
        {
            string sql = "SELECT COUNT(*) FROM DonHang WHERE MaKH = @id AND TrangThai IN " +
                         "(N'Chờ xác nhận', N'Đã xác nhận', N'Đang giao')";
            return Convert.ToInt32(DBConnect.ExecuteScalar(sql, DBConnect.P("@id", maKH)));
        }
    }
}
