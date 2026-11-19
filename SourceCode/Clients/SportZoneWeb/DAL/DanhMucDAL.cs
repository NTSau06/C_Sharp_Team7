using System.Data;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    public class DanhMucDAL
    {
        public List<DanhMucDTO> GetAll()
        {
            DataTable dt = DBConnect.ExecuteQuery(
                "SELECT dm.MaDM, dm.TenDM, dm.MoTa, (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaDM = dm.MaDM) AS SoSanPham " +
                "FROM DanhMuc dm ORDER BY dm.TenDM");
            var list = new List<DanhMucDTO>();
            foreach (DataRow r in dt.Rows)
                list.Add(new DanhMucDTO { MaDM = r.Int("MaDM"), TenDM = r.Str("TenDM"), MoTa = r.Str("MoTa"), SoSanPham = r.Int("SoSanPham") });
            return list;
        }

        public DanhMucDTO GetById(int maDM)
        {
            DataTable dt = DBConnect.ExecuteQuery("SELECT MaDM, TenDM, MoTa FROM DanhMuc WHERE MaDM = @id", DBConnect.P("@id", maDM));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            return new DanhMucDTO { MaDM = r.Int("MaDM"), TenDM = r.Str("TenDM"), MoTa = r.Str("MoTa") };
        }
    }
}
