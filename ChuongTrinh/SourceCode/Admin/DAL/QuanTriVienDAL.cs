using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class QuanTriVienDAL
    {
        public QuanTriVienDTO GetByTenDangNhap(string tenDangNhap)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT MaQTV, HoTen, TenDangNhap, MatKhau FROM QuanTriVien WHERE TenDangNhap = @u",
                DBConnect.P("@u", tenDangNhap));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new QuanTriVienDTO
            {
                MaQTV = r.Int("MaQTV"),
                HoTen = r.Str("HoTen"),
                TenDangNhap = r.Str("TenDangNhap"),
                MatKhau = r.Str("MatKhau")
            };
        }

        public bool DoiMatKhau(int maQTV, string matKhauHash)
        {
            return DBConnect.ExecuteNonQuery("UPDATE QuanTriVien SET MatKhau = @mk WHERE MaQTV = @id",
                DBConnect.P("@mk", matKhauHash), DBConnect.P("@id", maQTV)) > 0;
        }
    }
}
