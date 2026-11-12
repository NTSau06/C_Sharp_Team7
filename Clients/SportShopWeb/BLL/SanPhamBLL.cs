using SportShopWeb.DAL;
using SportShopWeb.DTO;
using SportShopWeb.Models;

namespace SportShopWeb.BLL
{
    public class SanPhamBLL
    {
        private readonly SanPhamDAL dal = new SanPhamDAL();

        /// <summary>Chuẩn hóa bộ lọc: bỏ giá âm, đảo khoảng giá nếu nhập ngược</summary>
        public static SanPhamFilter ChuanHoa(SanPhamFilter f)
        {
            f = f ?? new SanPhamFilter();
            if (f.GiaTu.HasValue && f.GiaTu < 0) f.GiaTu = null;
            if (f.GiaDen.HasValue && f.GiaDen < 0) f.GiaDen = null;
            if (f.GiaTu.HasValue && f.GiaDen.HasValue && f.GiaTu > f.GiaDen)
            {
                decimal tmp = f.GiaTu.Value;
                f.GiaTu = f.GiaDen;
                f.GiaDen = tmp;
            }
            if (string.IsNullOrWhiteSpace(f.SapXep)) f.SapXep = "moi";
            return f;
        }

        /// <summary>Sản phẩm theo danh mục, có lọc + sắp xếp (UC 1.2.7.2 - 1.2.7.4)</summary>
        public List<SanPhamDTO> TheoDanhMuc(int maDM, SanPhamFilter f)
        {
            f = ChuanHoa(f);
            f.MaDM = null;
            return dal.Search("", maDM, f);
        }

        /// <summary>Tìm kiếm theo từ khóa, có lọc + sắp xếp (UC 1.2.8.3 - 1.2.8.5)</summary>
        public List<SanPhamDTO> TimKiem(string tuKhoa, SanPhamFilter f)
        {
            tuKhoa = (tuKhoa ?? "").Trim();
            if (tuKhoa.Length == 0) throw new BusinessException("Vui lòng nhập từ khóa tìm kiếm");
            if (tuKhoa.Length > 100) tuKhoa = tuKhoa.Substring(0, 100);
            return dal.Search(tuKhoa, null, ChuanHoa(f));
        }

        /// <summary>Gợi ý nhanh khi gõ (UC 1.2.8.2)</summary>
        public List<SanPhamDTO> GoiY(string tuKhoa)
        {
            tuKhoa = (tuKhoa ?? "").Trim();
            if (tuKhoa.Length < 1 || tuKhoa.Length > 100) return new List<SanPhamDTO>();
            return dal.Suggest(tuKhoa, 8);
        }

        public SanPhamDTO GetById(int maSP) => dal.GetById(maSP);

        public List<SanPhamDTO> MoiNhat(int top) => dal.GetMoiNhat(top);

        public List<SanPhamDTO> BanChay(int top) => dal.GetBanChay(top);

        public List<SanPhamDTO> LienQuan(SanPhamDTO sp, int top) => dal.GetCungDanhMuc(sp.MaDM, sp.MaSP, top);
    }
}
