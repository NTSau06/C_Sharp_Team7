using System.Data;
using Microsoft.Data.SqlClient;
using SportShopWeb.DTO;
using SportShopWeb.Models;

namespace SportShopWeb.DAL
{
    public class SanPhamDAL
    {
        // DaBan: tổng số lượng đã bán trong các đơn "Đã giao" (dùng sắp xếp bán chạy)
        private const string SelectSql =
            "SELECT sp.MaSP, sp.TenSP, sp.GiaBan, sp.SoLuongTon, sp.MucTonToiThieu, sp.HinhAnh, sp.MoTa, sp.MaDM, dm.TenDM, " +
            "       ISNULL((SELECT SUM(ct.SoLuong) FROM ChiTietDonHang ct JOIN DonHang dh ON dh.MaDH = ct.MaDH " +
            "               WHERE ct.MaSP = sp.MaSP AND dh.TrangThai = N'Đã giao'), 0) AS DaBan " +
            "FROM SanPham sp JOIN DanhMuc dm ON dm.MaDM = sp.MaDM ";

        private static SanPhamDTO Map(DataRow r)
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
                TenDM = r.Str("TenDM"),
                DaBan = r.Int("DaBan")
            };
        }

        private static List<SanPhamDTO> MapAll(DataTable dt)
        {
            var list = new List<SanPhamDTO>();
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static string OrderBy(string sapXep)
        {
            switch (sapXep)
            {
                case "gia-tang": return " ORDER BY sp.GiaBan ASC, sp.MaSP DESC";
                case "gia-giam": return " ORDER BY sp.GiaBan DESC, sp.MaSP DESC";
                case "ten": return " ORDER BY sp.TenSP ASC";
                case "pho-bien": return " ORDER BY DaBan DESC, sp.MaSP DESC";
                default: return " ORDER BY sp.MaSP DESC";   // mới nhất
            }
        }

        /// <summary>
        /// Tìm/lọc/sắp xếp sản phẩm. keyword rỗng = không lọc theo từ khóa; maDM = danh mục đang xem.
        /// Tìm kiếm không phân biệt hoa thường và dấu (COLLATE ..._CI_AI).
        /// </summary>
        public List<SanPhamDTO> Search(string keyword, int? maDM, SanPhamFilter f)
        {
            keyword = (keyword ?? "").Trim();
            f = f ?? new SanPhamFilter();
            int? dm = maDM ?? f.MaDM;
            string sql = SelectSql +
                "WHERE (@kw = N'' OR sp.TenSP COLLATE Latin1_General_CI_AI LIKE @like COLLATE Latin1_General_CI_AI " +
                "       OR dm.TenDM COLLATE Latin1_General_CI_AI LIKE @like COLLATE Latin1_General_CI_AI " +
                "       OR CAST(sp.MaSP AS NVARCHAR(20)) = @kw) " +
                "  AND (@dm IS NULL OR sp.MaDM = @dm) " +
                "  AND (@giaTu IS NULL OR sp.GiaBan >= @giaTu) " +
                "  AND (@giaDen IS NULL OR sp.GiaBan <= @giaDen) " +
                "  AND (@conHang = 0 OR sp.SoLuongTon > 0)" +
                OrderBy(f.SapXep);
            return MapAll(DBConnect.ExecuteQuery(sql,
                DBConnect.PText("@kw", keyword, 200), DBConnect.PText("@like", "%" + keyword + "%", 210),
                DBConnect.PInt("@dm", dm), DBConnect.PDec("@giaTu", f.GiaTu), DBConnect.PDec("@giaDen", f.GiaDen),
                DBConnect.P("@conHang", f.ConHang ? 1 : 0)));
        }

        public SanPhamDTO GetById(int maSP)
        {
            DataTable dt = DBConnect.ExecuteQuery(SelectSql + "WHERE sp.MaSP = @id", DBConnect.P("@id", maSP));
            return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
        }

        /// <summary>Gợi ý nhanh khi gõ (AJAX) - tối đa top sản phẩm</summary>
        public List<SanPhamDTO> Suggest(string keyword, int top)
        {
            string sql = "SELECT TOP (@top) * FROM (" + SelectSql +
                "WHERE sp.TenSP COLLATE Latin1_General_CI_AI LIKE @like COLLATE Latin1_General_CI_AI) sp " +
                "ORDER BY CASE WHEN sp.TenSP COLLATE Latin1_General_CI_AI LIKE @start COLLATE Latin1_General_CI_AI THEN 0 ELSE 1 END, sp.DaBan DESC";
            return MapAll(DBConnect.ExecuteQuery(sql, DBConnect.P("@top", top),
                DBConnect.PText("@like", "%" + keyword + "%", 210), DBConnect.PText("@start", keyword + "%", 210)));
        }

        public List<SanPhamDTO> GetMoiNhat(int top)
        {
            return MapAll(DBConnect.ExecuteQuery("SELECT TOP (@top) * FROM (" + SelectSql + ") sp ORDER BY sp.MaSP DESC",
                DBConnect.P("@top", top)));
        }

        public List<SanPhamDTO> GetBanChay(int top)
        {
            return MapAll(DBConnect.ExecuteQuery("SELECT TOP (@top) * FROM (" + SelectSql + ") sp WHERE sp.DaBan > 0 ORDER BY sp.DaBan DESC, sp.MaSP DESC",
                DBConnect.P("@top", top)));
        }

        public List<SanPhamDTO> GetCungDanhMuc(int maDM, int boQuaMaSP, int top)
        {
            return MapAll(DBConnect.ExecuteQuery("SELECT TOP (@top) * FROM (" + SelectSql + "WHERE sp.MaDM = @dm AND sp.MaSP <> @id) sp ORDER BY sp.DaBan DESC",
                DBConnect.P("@top", top), DBConnect.P("@dm", maDM), DBConnect.P("@id", boQuaMaSP)));
        }

        /// <summary>Đọc tồn kho + giá trong giao dịch, khóa dòng để kiểm tra lần cuối khi đặt hàng</summary>
        public SanPhamDTO GetForUpdate(SqlTransaction t, int maSP)
        {
            DataTable dt = DBConnect.ExecuteQuery(t,
                "SELECT MaSP, TenSP, GiaBan, SoLuongTon FROM SanPham WITH (UPDLOCK, ROWLOCK) WHERE MaSP = @id",
                DBConnect.P("@id", maSP));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new SanPhamDTO { MaSP = r.Int("MaSP"), TenSP = r.Str("TenSP"), GiaBan = r.Dec("GiaBan"), SoLuongTon = r.Int("SoLuongTon") };
        }
    }
}
