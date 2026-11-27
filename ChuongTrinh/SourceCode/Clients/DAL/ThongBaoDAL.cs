using System.Data;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    public class ThongBaoDAL
    {
        public List<ThongBaoDTO> GetByKhachHang(int maKH, int top = 50)
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT TOP (@n) MaTB, MaKH, MaDH, TieuDe, NoiDung, NgayTao, DaDoc FROM ThongBao WHERE MaKH = @kh ORDER BY NgayTao DESC, MaTB DESC",
                DBConnect.P("@n", top), DBConnect.P("@kh", maKH));
            return dt.Rows.Cast<DataRow>().Select(r => new ThongBaoDTO
            {
                MaTB = r.Int("MaTB"), MaKH = r.Int("MaKH"), MaDH = r.IntN("MaDH"), TieuDe = r.Str("TieuDe"),
                NoiDung = r.Str("NoiDung"), NgayTao = r.Date("NgayTao"), DaDoc = r.Bool("DaDoc")
            }).ToList();
        }

        public int DemChuaDoc(int maKH) =>
            Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM ThongBao WHERE MaKH = @kh AND DaDoc = 0", DBConnect.P("@kh", maKH)));

        public void DanhDauDaDoc(int maKH, int? maTB) =>
            DBConnect.ExecuteNonQuery(
                "UPDATE ThongBao SET DaDoc = 1 WHERE MaKH = @kh AND DaDoc = 0 AND (@tb IS NULL OR MaTB = @tb)",
                DBConnect.P("@kh", maKH), DBConnect.PInt("@tb", maTB));

        public void Them(int maKH, int? maDH, string tieuDe, string noiDung) =>
            DBConnect.ExecuteNonQuery(
                "INSERT INTO ThongBao(MaKH, MaDH, TieuDe, NoiDung) VALUES(@kh, @dh, @td, @nd)",
                DBConnect.P("@kh", maKH), DBConnect.PInt("@dh", maDH), DBConnect.P("@td", tieuDe), DBConnect.P("@nd", noiDung));
    }
}
