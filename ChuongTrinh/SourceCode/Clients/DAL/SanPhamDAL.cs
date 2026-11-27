using System.Data;
using Microsoft.Data.SqlClient;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;

namespace SportZoneWeb.DAL
{
    public class SanPhamDAL
    {
        // DaBan: tổng số lượng đã bán trong các đơn "Đã giao hàng" (dùng sắp xếp bán chạy)
        private const string SelectSql =
            "SELECT sp.MaSP, sp.TenSP, sp.GiaBan, sp.SoLuongTon, sp.MucTonToiThieu, sp.HinhAnh, sp.MoTa, sp.MaDM, dm.TenDM, " +
            "       ISNULL((SELECT SUM(ct.SoLuong) FROM ChiTietDonHang ct JOIN DonHang dh ON dh.MaDH = ct.MaDH " +
            "               WHERE ct.MaSP = sp.MaSP AND dh.TrangThai = N'Đã giao hàng'), 0) AS DaBan " +
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

        // Điều kiện tìm/lọc dùng chung cho truy vấn đếm và truy vấn lấy trang.
        // Tìm kiếm không phân biệt hoa thường và dấu (COLLATE ..._CI_AI).
        private const string WhereSql =
            "WHERE (@kw = N'' OR sp.TenSP COLLATE Latin1_General_CI_AI LIKE @like COLLATE Latin1_General_CI_AI " +
            "       OR dm.TenDM COLLATE Latin1_General_CI_AI LIKE @like COLLATE Latin1_General_CI_AI " +
            "       OR CAST(sp.MaSP AS NVARCHAR(20)) = @kw) " +
            "  AND (@dm IS NULL OR sp.MaDM = @dm) " +
            "  AND (@giaTu IS NULL OR sp.GiaBan >= @giaTu) " +
            "  AND (@giaDen IS NULL OR sp.GiaBan <= @giaDen) " +
            "  AND (@conHang = 0 OR sp.SoLuongTon > 0) ";

        /// <summary>Tạo bộ SqlParameter mới cho mỗi lần thực thi (một SqlParameter không dùng lại cho 2 lệnh)</summary>
        private static List<SqlParameter> FilterParams(string keyword, int? dm, SanPhamFilter f)
        {
            return new List<SqlParameter>
            {
                DBConnect.PText("@kw", keyword, 200), DBConnect.PText("@like", "%" + keyword + "%", 210),
                DBConnect.PInt("@dm", dm), DBConnect.PDec("@giaTu", f.GiaTu), DBConnect.PDec("@giaDen", f.GiaDen),
                DBConnect.P("@conHang", f.ConHang ? 1 : 0)
            };
        }

        /// <summary>
        /// Tìm/lọc/sắp xếp sản phẩm (không phân trang). keyword rỗng = không lọc theo từ khóa; maDM = danh mục đang xem.
        /// </summary>
        public List<SanPhamDTO> Search(string keyword, int? maDM, SanPhamFilter f)
        {
            keyword = (keyword ?? "").Trim();
            f = f ?? new SanPhamFilter();
            int? dm = maDM ?? f.MaDM;
            return MapAll(DBConnect.ExecuteQuery(SelectSql + WhereSql + OrderBy(f.SapXep), FilterParams(keyword, dm, f).ToArray()));
        }

        /// <summary>Đếm số sản phẩm thỏa điều kiện tìm/lọc (để tính số trang)</summary>
        public int CountSearch(string keyword, int? maDM, SanPhamFilter f)
        {
            keyword = (keyword ?? "").Trim();
            f = f ?? new SanPhamFilter();
            int? dm = maDM ?? f.MaDM;
            object o = DBConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM SanPham sp JOIN DanhMuc dm ON dm.MaDM = sp.MaDM " + WhereSql,
                FilterParams(keyword, dm, f).ToArray());
            return Convert.ToInt32(o);
        }

        /// <summary>Lấy một trang kết quả tìm/lọc/sắp xếp (OFFSET ... FETCH - SQL Server 2012 trở lên)</summary>
        public List<SanPhamDTO> SearchPage(string keyword, int? maDM, SanPhamFilter f, int page, int pageSize)
        {
            keyword = (keyword ?? "").Trim();
            f = f ?? new SanPhamFilter();
            int? dm = maDM ?? f.MaDM;
            List<SqlParameter> ps = FilterParams(keyword, dm, f);
            ps.Add(DBConnect.P("@skip", Math.Max(0, (page - 1) * pageSize)));
            ps.Add(DBConnect.P("@take", pageSize));
            string sql = SelectSql + WhereSql + OrderBy(f.SapXep) + " OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
            return MapAll(DBConnect.ExecuteQuery(sql, ps.ToArray()));
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
