using SportZoneWeb.DAL;
using SportZoneWeb.DTO;

namespace SportZoneWeb.BLL
{
    /// <summary>UC 1.2.11 - Thanh toán: chọn phương thức thanh toán</summary>
    public class ThanhToanBLL
    {
        private readonly PhuongThucThanhToanDAL ptttDAL = new PhuongThucThanhToanDAL();

        public List<PhuongThucThanhToanDTO> LayPhuongThuc() => ptttDAL.GetAll();

        public bool KiemTraPhuongThuc(int? maPTTT) => maPTTT.HasValue && ptttDAL.Exists(maPTTT.Value);
    }
}
