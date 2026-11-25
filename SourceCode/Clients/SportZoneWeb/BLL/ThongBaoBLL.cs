using SportZoneWeb.DAL;
using SportZoneWeb.DTO;

namespace SportZoneWeb.BLL
{
    /// <summary>Thông báo cho khách hàng. Thông báo đổi trạng thái đơn được tạo tự động bằng trigger TR_DonHang_ThongBao (Web + Admin).</summary>
    public class ThongBaoBLL
    {
        private readonly ThongBaoDAL dal = new ThongBaoDAL();

        public List<ThongBaoDTO> LayDanhSach(int maKH) => dal.GetByKhachHang(maKH);
        public int DemChuaDoc(int maKH) => dal.DemChuaDoc(maKH);
        public void DocTatCa(int maKH) => dal.DanhDauDaDoc(maKH, null);
        public void DocMot(int maKH, int maTB) => dal.DanhDauDaDoc(maKH, maTB);
        public void Them(int maKH, int? maDH, string tieuDe, string noiDung) => dal.Them(maKH, maDH, tieuDe, noiDung);
    }
}
