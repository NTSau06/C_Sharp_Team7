using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.BLL
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
            if (string.IsNullOrWhiteSpace(f.SapXep) || !SanPhamFilter.CacKieuSapXep.Any(s => s.Value == f.SapXep)) f.SapXep = "moi";
            if (f.Page < 1) f.Page = 1;
            return f;
        }

        /// <summary>Sản phẩm theo danh mục, có lọc + sắp xếp + phân trang (UC 1.2.7.2 - 1.2.7.4)</summary>
        public KetQuaTrang<SanPhamDTO> TheoDanhMuc(int maDM, SanPhamFilter f)
        {
            f = ChuanHoa(f);
            f.MaDM = null;
            return LayTrang("", maDM, f);
        }

        /// <summary>Tìm kiếm theo từ khóa, có lọc + sắp xếp + phân trang (UC 1.2.8.3 - 1.2.8.5)</summary>
        public KetQuaTrang<SanPhamDTO> TimKiem(string tuKhoa, SanPhamFilter f)
        {
            tuKhoa = (tuKhoa ?? "").Trim();
            if (tuKhoa.Length == 0) throw new BusinessException("Vui lòng nhập từ khóa tìm kiếm");
            if (tuKhoa.Length > 100) tuKhoa = tuKhoa.Substring(0, 100);
            return LayTrang(tuKhoa, null, ChuanHoa(f));
        }

        /// <summary>Đếm tổng số, chỉnh số trang về khoảng hợp lệ [1..TotalPages] rồi lấy đúng trang đó</summary>
        private KetQuaTrang<SanPhamDTO> LayTrang(string tuKhoa, int? maDM, SanPhamFilter f)
        {
            var kq = new KetQuaTrang<SanPhamDTO> { PageSize = SanPhamFilter.PageSize };
            if (f.LocThuocTinh)
            {
                // CSDL chưa có cột thương hiệu / kích cỡ / màu => lấy toàn bộ kết quả rồi lọc và phân trang trong bộ nhớ
                List<SanPhamDTO> tatCa = dal.Search(tuKhoa, maDM, f).Where(sp => ThuocTinhSanPham.Khop(sp, f)).ToList();
                kq.TotalItems = tatCa.Count;
                kq.Page = Math.Min(Math.Max(1, f.Page), kq.TotalPages);
                f.Page = kq.Page;
                kq.Items = tatCa.Skip((kq.Page - 1) * kq.PageSize).Take(kq.PageSize).ToList();
                return kq;
            }
            kq.TotalItems = dal.CountSearch(tuKhoa, maDM, f);
            kq.Page = Math.Min(Math.Max(1, f.Page), kq.TotalPages);
            f.Page = kq.Page;
            if (kq.TotalItems > 0)
                kq.Items = dal.SearchPage(tuKhoa, maDM, f, kq.Page, kq.PageSize);
            return kq;
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

        /// <summary>Sản phẩm liên quan: cùng danh mục trước, thiếu thì bổ sung sản phẩm bán chạy</summary>
        public List<SanPhamDTO> LienQuan(SanPhamDTO sp, int top)
        {
            List<SanPhamDTO> ds = dal.GetCungDanhMuc(sp.MaDM, sp.MaSP, top);
            if (ds.Count < top)
            {
                foreach (SanPhamDTO x in dal.GetBanChay(top * 2))
                {
                    if (ds.Count >= top) break;
                    if (x.MaSP != sp.MaSP && ds.All(d => d.MaSP != x.MaSP)) ds.Add(x);
                }
            }
            return ds;
        }

        /// <summary>Lấy danh sách sản phẩm theo thứ tự mã cho trước (yêu thích, đã xem), bỏ mã không còn tồn tại</summary>
        public List<SanPhamDTO> TheoDanhSachMa(IEnumerable<int> dsMa)
        {
            var ds = new List<SanPhamDTO>();
            foreach (int ma in dsMa)
            {
                SanPhamDTO sp = dal.GetById(ma);
                if (sp != null) ds.Add(sp);
            }
            return ds;
        }
    }
}
