using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.7 - Xem danh mục thông tin</summary>
    public class CategoryController : Controller
    {
        private readonly DanhMucBLL _danhMucBLL = new DanhMucBLL();
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();

        /// <summary>UC 1.2.7.1 - Hiển thị danh sách danh mục</summary>
        public IActionResult Index()
        {
            try
            {
                return View(_danhMucBLL.GetAll());
            }
            catch (Exception)
            {
                ViewBag.ThongBao = "Không thể tải danh sách danh mục. Vui lòng thử lại sau";
                return View(new List<DanhMucDTO>());
            }
        }

        /// <summary>UC 1.2.7.2 / 1.2.7.3 / 1.2.7.4 - Xem, lọc, sắp xếp sản phẩm theo danh mục (có phân trang, ?Page=)</summary>
        [HttpGet("/Category/Products/{maDM:int}")]
        public IActionResult Products(int maDM, SanPhamFilter f)
        {
            DanhMucDTO dm = _danhMucBLL.GetById(maDM);
            if (dm == null) return NotFound();

            f = SanPhamBLL.ChuanHoa(f);
            KetQuaTrang<SanPhamDTO> kq = _sanPhamBLL.TheoDanhMuc(maDM, f);
            var vm = new ProductListVM
            {
                TieuDe = dm.TenDM,
                MoTa = dm.MoTa,
                DanhMuc = dm,
                BoLoc = f,
                SanPhams = kq.Items,
                Page = kq.Page,
                PageSize = kq.PageSize,
                TotalItems = kq.TotalItems,
                DanhMucs = _danhMucBLL.GetAll()
            };
            if (vm.SanPhams.Count == 0)
                vm.ThongBao = f.DangLoc ? "Không tìm thấy sản phẩm phù hợp với bộ lọc" : "Danh mục hiện chưa có sản phẩm nào";
            return View("~/Views/Shared/ProductList.cshtml", vm);
        }
    }
}
