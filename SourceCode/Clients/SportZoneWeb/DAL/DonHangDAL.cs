using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    public class DonHangDAL
    {
        private const string SelectSql =
            "SELECT dh.MaDH, dh.NgayDat, dh.TrangThai, dh.DiaChiGiao, dh.TienGiam, dh.TongTien, dh.GhiChu, dh.MaKH, dh.MaPTTT, dh.MaMGG, dh.TrangThaiThanhToan, dh.MaGiaoDich, dh.MaXacThucDemo, " +
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
                TrangThaiThanhToan = r.Str("TrangThaiThanhToan"),
                MaGiaoDich = r.Str("MaGiaoDich"),
                MaXacThucDemo = r.Str("MaXacThucDemo"),
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

        /// <summary>
        /// Khách hủy đơn của mình: chỉ đổi khi đơn còn "Chờ xác nhận" (kiểm tra ngay trong câu UPDATE),
        /// hoàn 1 lượt dùng mã giảm giá nếu có. Trả về false nếu đơn không còn hủy được.
        /// </summary>
        public bool HuyDon(SqlTransaction t, int maKH, int maDH, string lyDo)
        {
            // Bảng DonHang có trigger TR_DonHang_LichSu => OUTPUT phải ghi vào biến bảng (OUTPUT ... INTO)
            DataTable dt = DBConnect.ExecuteQuery(t,
                "DECLARE @kq TABLE (MaMGG INT NULL); " +
                "UPDATE DonHang SET TrangThai = N'Đã hủy', GhiChu = @gc OUTPUT inserted.MaMGG INTO @kq " +
                "WHERE MaDH = @dh AND MaKH = @kh AND TrangThai = N'Chờ xác nhận'; " +
                "SELECT MaMGG FROM @kq;",
                DBConnect.P("@gc", lyDo), DBConnect.P("@dh", maDH), DBConnect.P("@kh", maKH));
            if (dt.Rows.Count == 0) return false;
            int? maMGG = dt.Rows[0].IntN("MaMGG");
            if (maMGG.HasValue)
                DBConnect.ExecuteNonQuery(t,
                    "UPDATE MaGiamGia SET SoLuotDaDung = SoLuotDaDung - 1 WHERE MaMGG = @m AND SoLuotDaDung > 0",
                    DBConnect.P("@m", maMGG.Value));
            return true;
        }

        public void InsertChiTiet(SqlTransaction t, int maDH, int maSP, int soLuong, decimal donGia)
        {
            DBConnect.ExecuteNonQuery(t,
                "INSERT INTO ChiTietDonHang(MaDH, MaSP, SoLuong, DonGia) VALUES(@dh, @sp, @sl, @gia)",
                DBConnect.P("@dh", maDH), DBConnect.P("@sp", maSP), DBConnect.P("@sl", soLuong), DBConnect.P("@gia", donGia));
        }

        /// <summary>Gateway báo thanh toán thành công. Chỉ đổi khi đơn chưa "Đã thanh toán" (an toàn khi gateway gọi lặp). Trả về true nếu vừa đổi.</summary>
        public bool DanhDauDaThanhToan(int maDH, string maGiaoDich)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE DonHang SET TrangThaiThanhToan = @tt, MaGiaoDich = @gd WHERE MaDH = @dh AND TrangThaiThanhToan <> @tt",
                DBConnect.P("@tt", TrangThaiThanhToanDH.Da), DBConnect.P("@gd", (object)maGiaoDich ?? DBNull.Value), DBConnect.P("@dh", maDH)) > 0;
        }

        /// <summary>Gateway báo thất bại / khách hủy thanh toán (không ghi đè đơn đã thanh toán)</summary>
        public void DanhDauThanhToanThatBai(int maDH)
        {
            DBConnect.ExecuteNonQuery(
                "UPDATE DonHang SET TrangThaiThanhToan = @tt WHERE MaDH = @dh AND TrangThaiThanhToan <> @da",
                DBConnect.P("@tt", TrangThaiThanhToanDH.ThatBai), DBConnect.P("@da", TrangThaiThanhToanDH.Da), DBConnect.P("@dh", maDH));
        }

        public bool BatDauThanhToanDemo(int maDH, string maXacThuc)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE DonHang SET MaXacThucDemo = @mx WHERE MaDH = @dh AND TrangThaiThanhToan <> @da AND TrangThai <> @huy",
                DBConnect.P("@mx", maXacThuc), DBConnect.P("@dh", maDH),
                DBConnect.P("@da", TrangThaiThanhToanDH.Da), DBConnect.P("@huy", TrangThaiDonHang.DaHuy)) > 0;
        }

        public bool DanhDauThanhToanDemo(int maDH, string maXacThuc, string maGiaoDich)
        {
            return DBConnect.ExecuteNonQuery(
                "UPDATE DonHang SET TrangThaiThanhToan = @da, MaGiaoDich = @gd " +
                "WHERE MaDH = @dh AND MaXacThucDemo = @mx AND TrangThaiThanhToan <> @da AND TrangThai <> @huy",
                DBConnect.P("@da", TrangThaiThanhToanDH.Da), DBConnect.P("@gd", maGiaoDich),
                DBConnect.P("@dh", maDH), DBConnect.P("@mx", maXacThuc),
                DBConnect.P("@huy", TrangThaiDonHang.DaHuy)) > 0;
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
