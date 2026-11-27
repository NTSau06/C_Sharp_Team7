using System.Data;
using SportZoneWeb.DTO;

namespace SportZoneWeb.DAL
{
    public class PhuongThucThanhToanDAL
    {
        public List<PhuongThucThanhToanDTO> GetAll()
        {
            DataTable dt = DBConnect.ExecuteQuery("SELECT MaPTTT, TenPTTT FROM PhuongThucThanhToan ORDER BY MaPTTT");
            var list = new List<PhuongThucThanhToanDTO>();
            foreach (DataRow r in dt.Rows) list.Add(new PhuongThucThanhToanDTO { MaPTTT = r.Int("MaPTTT"), TenPTTT = r.Str("TenPTTT") });
            return list;
        }

        public bool Exists(int maPTTT)
        {
            return Convert.ToInt32(DBConnect.ExecuteScalar("SELECT COUNT(*) FROM PhuongThucThanhToan WHERE MaPTTT = @id", DBConnect.P("@id", maPTTT))) > 0;
        }
    }
}
