using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.DAL
{
    public class DonHangDAL
    {
        private const string SelectSql =
            "SELECT dh.MaDH, dh.NgayDat, dh.TrangThai, dh.DiaChiGiao, dh.TienGiam, dh.TongTien, dh.GhiChu, " +
            "       dh.MaKH, dh.MaPTTT, dh.MaQTV, dh.MaMGG, dh.TrangThaiThanhToan, kh.HoTen AS TenKH, kh.SDT, kh.Email, pt.TenPTTT, " +
            "       q.HoTen AS TenQTV, m.Code AS CodeMGG " +
            "FROM DonHang dh " +
            "JOIN KhachHang kh ON kh.MaKH = dh.MaKH " +
            "JOIN PhuongThucThanhToan pt ON pt.MaPTTT = dh.MaPTTT " +
            "LEFT JOIN QuanTriVien q ON q.MaQTV = dh.MaQTV " +
            "LEFT JOIN MaGiamGia m ON m.MaMGG = dh.MaMGG ";

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
                MaQTV = r.IntN("MaQTV"),
                MaMGG = r.IntN("MaMGG"),
                TenKH = r.Str("TenKH"),
                SDT = r.Str("SDT"),
                Email = r.Str("Email"),
                TenPTTT = r.Str("TenPTTT"),
                TrangThaiThanhToan = r.Str("TrangThaiThanhToan"),
                TenQTV = r.Str("TenQTV"),
                CodeMGG = r.Str("CodeMGG")
            };
        }

        public List<DonHangDTO> GetList(string keyword, string trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            keyword = (keyword ?? "").Trim();
            string sql = SelectSql +
                "WHERE (@kw = '' OR CAST(dh.MaDH AS VARCHAR(20)) = @kw OR kh.HoTen LIKE @like OR kh.SDT LIKE @like) " +
                "  AND (@tt = '' OR dh.TrangThai = @tt) " +
                "  AND (@f IS NULL OR dh.NgayDat >= @f) AND (@t IS NULL OR dh.NgayDat < @t) " +
                "ORDER BY dh.MaDH DESC";
            DataTable dt = DBConnect.ExecuteQuery(sql,
                DBConnect.PText("@kw", keyword), DBConnect.PText("@like", "%" + keyword + "%"),
                DBConnect.PText("@tt", trangThai ?? ""),
                DBConnect.PDate("@f", tuNgay.HasValue ? tuNgay.Value.Date : (DateTime?)null),
                DBConnect.PDate("@t", denNgay.HasValue ? denNgay.Value.Date.AddDays(1) : (DateTime?)null));
            var list = new List<DonHangDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public DonHangDTO GetById(int maDH)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE dh.MaDH = @id", DBConnect.P("@id", maDH));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        /// <summary>Đọc đơn hàng trong transaction và khóa dòng (UPDLOCK) để tránh xử lý trùng</summary>
        public DonHangDTO GetForUpdate(SqlTransaction t, int maDH)
        {
            DataTable dt = DBConnect.ExecuteQuery(t,
                "SELECT dh.MaDH, dh.TrangThai, dh.MaMGG, dh.TrangThaiThanhToan, pt.TenPTTT " +
                "FROM DonHang dh WITH (UPDLOCK, ROWLOCK) JOIN PhuongThucThanhToan pt ON pt.MaPTTT = dh.MaPTTT " +
                "WHERE dh.MaDH = @id",
                DBConnect.P("@id", maDH));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new DonHangDTO
            {
                MaDH = r.Int("MaDH"),
                TrangThai = r.Str("TrangThai"),
                MaMGG = r.IntN("MaMGG"),
                TrangThaiThanhToan = r.Str("TrangThaiThanhToan"),
                TenPTTT = r.Str("TenPTTT")
            };
        }

        private const string ChiTietSql =
            "SELECT ct.MaDH, ct.MaSP, sp.TenSP, ct.SoLuong, ct.DonGia, sp.SoLuongTon " +
            "FROM ChiTietDonHang ct JOIN SanPham sp ON sp.MaSP = ct.MaSP WHERE ct.MaDH = @id ORDER BY sp.TenSP";

        private static List<ChiTietDonHangDTO> MapChiTiet(DataTable dt)
        {
            var list = new List<ChiTietDonHangDTO>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new ChiTietDonHangDTO
                {
                    MaDH = r.Int("MaDH"),
                    MaSP = r.Int("MaSP"),
                    TenSP = r.Str("TenSP"),
                    SoLuong = r.Int("SoLuong"),
                    DonGia = r.Dec("DonGia"),
                    SoLuongTon = r.Int("SoLuongTon")
                });
            }
            return list;
        }

        public List<ChiTietDonHangDTO> GetChiTiet(int maDH)
        {
            return MapChiTiet(DBConnect.ExecuteQuery(ChiTietSql, DBConnect.P("@id", maDH)));
        }

        public List<ChiTietDonHangDTO> GetChiTiet(SqlTransaction t, int maDH)
        {
            return MapChiTiet(DBConnect.ExecuteQuery(t, ChiTietSql, DBConnect.P("@id", maDH)));
        }

        /// <summary>Cập nhật trạng thái; ghiChu = null thì giữ nguyên ghi chú cũ</summary>
        public bool UpdateTrangThai(SqlTransaction t, int maDH, string trangThai, int maQTV, string ghiChu)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE DonHang SET TrangThai = @tt, MaQTV = @qtv, GhiChu = COALESCE(@gc, GhiChu) WHERE MaDH = @id",
                DBConnect.P("@tt", trangThai), DBConnect.P("@qtv", maQTV), DBConnect.P("@gc", ghiChu), DBConnect.P("@id", maDH)) > 0;
        }
    }
}
