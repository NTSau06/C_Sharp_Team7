using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    /// <summary>Bảng DiaChiGiaoHang - sổ địa chỉ giao hàng</summary>
    public class DiaChiDAL
    {
        private static DiaChiGiaoHangDTO Map(DataRow r) => new DiaChiGiaoHangDTO
        {
            MaDC = r.Int("MaDC"),
            MaKH = r.Int("MaKH"),
            HoTenNhan = r.Str("HoTenNhan"),
            SDT = r.Str("SDT"),
            DiaChi = r.Str("DiaChi"),
            MacDinh = r.Bool("MacDinh")
        };

        public List<DiaChiGiaoHangDTO> GetByKhachHang(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT MaDC, MaKH, HoTenNhan, SDT, DiaChi, MacDinh FROM DiaChiGiaoHang WHERE MaKH = @kh ORDER BY MacDinh DESC, MaDC",
                DBConnect.P("@kh", maKH));
            return dt.Rows.Cast<DataRow>().Select(Map).ToList();
        }

        public int Count(int maKH) =>
            Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM DiaChiGiaoHang WHERE MaKH = @kh", DBConnect.P("@kh", maKH)));

        public int Insert(SqlTransaction t, DiaChiGiaoHangDTO dc)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar(t,
                "INSERT INTO DiaChiGiaoHang(MaKH, HoTenNhan, SDT, DiaChi, MacDinh) VALUES(@kh, @ten, @sdt, @dc, @md); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);",
                DBConnect.P("@kh", dc.MaKH), DBConnect.P("@ten", dc.HoTenNhan), DBConnect.P("@sdt", dc.SDT),
                DBConnect.P("@dc", dc.DiaChi), DBConnect.P("@md", dc.MacDinh)));
        }

        /// <summary>Sửa địa chỉ của chính khách hàng (MaKH khớp), trả về true nếu có dòng được sửa</summary>
        public bool Update(SqlTransaction t, DiaChiGiaoHangDTO dc)
        {
            return DBConnect.ExecuteNonQuery(t,
                "UPDATE DiaChiGiaoHang SET HoTenNhan = @ten, SDT = @sdt, DiaChi = @dc WHERE MaDC = @id AND MaKH = @kh",
                DBConnect.P("@ten", dc.HoTenNhan), DBConnect.P("@sdt", dc.SDT), DBConnect.P("@dc", dc.DiaChi),
                DBConnect.P("@id", dc.MaDC), DBConnect.P("@kh", dc.MaKH)) > 0;
        }

        /// <summary>Đặt một địa chỉ làm mặc định, bỏ mặc định các địa chỉ còn lại</summary>
        public void SetMacDinh(SqlTransaction t, int maKH, int maDC)
        {
            DBConnect.ExecuteNonQuery(t,
                "UPDATE DiaChiGiaoHang SET MacDinh = CASE WHEN MaDC = @id THEN 1 ELSE 0 END WHERE MaKH = @kh",
                DBConnect.P("@id", maDC), DBConnect.P("@kh", maKH));
        }

        /// <summary>Xóa địa chỉ; nếu xóa địa chỉ mặc định thì địa chỉ còn lại đầu tiên thành mặc định</summary>
        public bool Delete(SqlTransaction t, int maKH, int maDC)
        {
            int n = DBConnect.ExecuteNonQuery(t, "DELETE FROM DiaChiGiaoHang WHERE MaDC = @id AND MaKH = @kh",
                DBConnect.P("@id", maDC), DBConnect.P("@kh", maKH));
            DBConnect.ExecuteNonQuery(t,
                "IF NOT EXISTS (SELECT 1 FROM DiaChiGiaoHang WHERE MaKH = @kh AND MacDinh = 1) " +
                "UPDATE DiaChiGiaoHang SET MacDinh = 1 WHERE MaDC = (SELECT MIN(MaDC) FROM DiaChiGiaoHang WHERE MaKH = @kh)",
                DBConnect.P("@kh", maKH));
            return n > 0;
        }
    }

    /// <summary>Bảng YeuThich - sản phẩm yêu thích</summary>
    public class YeuThichDAL
    {
        public List<int> GetMaSP(int maKH)
        {
            DataTable dt = DBConnect.ExecuteQuery("SELECT MaSP FROM YeuThich WHERE MaKH = @kh ORDER BY NgayThem DESC", DBConnect.P("@kh", maKH));
            return dt.Rows.Cast<DataRow>().Select(r => r.Int("MaSP")).ToList();
        }

        public bool Exists(int maKH, int maSP) =>
            DBConnect.ExecuteScalar("SELECT 1 FROM YeuThich WHERE MaKH = @kh AND MaSP = @sp",
                DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP)) != null;

        public void Insert(int maKH, int maSP) =>
            DBConnect.ExecuteNonQuery(
                "IF NOT EXISTS (SELECT 1 FROM YeuThich WHERE MaKH = @kh AND MaSP = @sp) INSERT INTO YeuThich(MaKH, MaSP) VALUES(@kh, @sp)",
                DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP));

        public void Delete(int maKH, int maSP) =>
            DBConnect.ExecuteNonQuery("DELETE FROM YeuThich WHERE MaKH = @kh AND MaSP = @sp", DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP));
    }

    /// <summary>Bảng DanhGia - đánh giá sản phẩm</summary>
    public class DanhGiaDAL
    {
        public List<DanhGiaDTO> GetBySanPham(int maSP)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT dg.MaDG, dg.MaSP, dg.MaKH, kh.HoTen, dg.SoSao, dg.NoiDung, dg.NgayDanhGia " +
                "FROM DanhGia dg JOIN KhachHang kh ON kh.MaKH = dg.MaKH WHERE dg.MaSP = @sp ORDER BY dg.NgayDanhGia DESC",
                DBConnect.P("@sp", maSP));
            return dt.Rows.Cast<DataRow>().Select(r => new DanhGiaDTO
            {
                MaDG = r.Int("MaDG"),
                MaSP = r.Int("MaSP"),
                MaKH = r.Int("MaKH"),
                HoTen = r.Str("HoTen"),
                SoSao = r.Int("SoSao"),
                NoiDung = r.Str("NoiDung"),
                NgayDanhGia = r.Date("NgayDanhGia")
            }).ToList();
        }

        /// <summary>Thêm mới hoặc cập nhật đánh giá của khách cho sản phẩm (mỗi khách 1 đánh giá)</summary>
        public void Upsert(int maSP, int maKH, int soSao, string noiDung)
        {
            DBConnect.ExecuteNonQuery(
                "UPDATE DanhGia SET SoSao = @sao, NoiDung = @nd, NgayDanhGia = GETDATE() WHERE MaSP = @sp AND MaKH = @kh; " +
                "IF @@ROWCOUNT = 0 INSERT INTO DanhGia(MaSP, MaKH, SoSao, NoiDung) VALUES(@sp, @kh, @sao, @nd);",
                DBConnect.P("@sp", maSP), DBConnect.P("@kh", maKH), DBConnect.P("@sao", soSao), DBConnect.P("@nd", noiDung));
        }

        /// <summary>Khách đã mua sản phẩm trong một đơn "Đã giao hàng"</summary>
        public bool DaMua(int maKH, int maSP) =>
            DBConnect.ExecuteScalar(
                "SELECT TOP 1 1 FROM DonHang dh JOIN ChiTietDonHang ct ON ct.MaDH = dh.MaDH " +
                "WHERE dh.MaKH = @kh AND ct.MaSP = @sp AND dh.TrangThai = N'Đã giao hàng'",
                DBConnect.P("@kh", maKH), DBConnect.P("@sp", maSP)) != null;
    }

    /// <summary>Bảng LichSuDonHang (ghi tự động bằng trigger TR_DonHang_LichSu)</summary>
    public class LichSuDonHangDAL
    {
        public List<LichSuDonHangDTO> GetByDonHang(int maDH)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT MaDH, TrangThai, ThoiGian, GhiChu FROM LichSuDonHang WHERE MaDH = @dh ORDER BY ThoiGian, MaLS",
                DBConnect.P("@dh", maDH));
            return dt.Rows.Cast<DataRow>().Select(r => new LichSuDonHangDTO
            {
                MaDH = r.Int("MaDH"),
                TrangThai = r.Str("TrangThai"),
                ThoiGian = r.Date("ThoiGian"),
                GhiChu = r.Str("GhiChu")
            }).ToList();
        }
    }
}
