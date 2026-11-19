using SportZoneWeb.DAL;
using SportZoneWeb.DTO;

namespace SportZoneWeb.BLL
{
    public class DanhMucBLL
    {
        private readonly DanhMucDAL dal = new DanhMucDAL();

        public List<DanhMucDTO> GetAll() => dal.GetAll();

        public DanhMucDTO GetById(int maDM) => dal.GetById(maDM);
    }
}
