using Microsoft.AspNetCore.Mvc;
using SportShopWeb.BLL;
using SportShopWeb.Models;
using SportShopWeb.Utils;

namespace SportShopWeb.Controllers
{
    /// <summary>UC 1.2.8 - Tìm kiếm thông tin</summary>
    public class SearchController : Controller
    {
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();
        private readonly DanhMucBLL _danhMucBLL = new DanhMucBLL();

        /// <summary>UC 1.2.8.1 / 1.2.8.3 / 1.2.8.4 / 1.2.8.5 - Tìm, lọc, sắp xếp kết quả</summary>
        public IActionResult Index(string q, SanPhamFilter f)
        {
            f = SanPhamBLL.ChuanHoa(f);
            var vm = new ProductListVM
            {
                TuKhoa = (q ?? "").Trim(),
                BoLoc = f,
                DanhMucs = _danhMucBLL.GetAll()
            };
            vm.TieuDe = vm.TuKhoa == "" ? "Tìm kiếm" : "Kết quả tìm kiếm cho \"" + vm.TuKhoa + "\"";
            try
            {
                vm.SanPhams = _sanPhamBLL.TimKiem(vm.TuKhoa, f);
                if (vm.SanPhams.Count == 0)
                    vm.ThongBao = f.DangLoc ? "Không có sản phẩm nào thỏa mãn bộ lọc" : "Không tìm thấy kết quả phù hợp với từ khóa của bạn";
            }
            catch (BusinessException ex)
            {
                vm.ThongBao = ex.Message;
            }
            return View("~/Views/Shared/ProductList.cshtml", vm);
        }

        /// <summary>UC 1.2.8.2 - Gợi ý kết quả (AJAX)</summary>
        [HttpGet]
        public JsonResult Suggest(string q)
        {
            var data = _sanPhamBLL.GoiY(q).Select(sp => new
            {
                maSP = sp.MaSP,
                tenSP = sp.TenSP,
                tenDM = sp.TenDM,
                gia = WebHelper.Tien(sp.GiaBan),
                anh = WebHelper.AnhSanPham(sp.HinhAnh),
                vietTat = WebHelper.VietTat(sp.TenSP),
                mau = WebHelper.MauDanhMuc(sp.MaDM),
                url = Url.Action("Detail", "Product", new { id = sp.MaSP })
            });
            return Json(data);
        }
    }
}
