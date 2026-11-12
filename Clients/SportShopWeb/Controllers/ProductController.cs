using Microsoft.AspNetCore.Mvc;
using SportShopWeb.BLL;
using SportShopWeb.DAL;
using SportShopWeb.DTO;
using SportShopWeb.Models;
using SportShopWeb.Utils;

namespace SportShopWeb.Controllers
{
    /// <summary>UC 1.2.9 - Xem chi tiết sản phẩm</summary>
    public class ProductController : Controller
    {
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();

        public IActionResult Detail(int id)
        {
            SanPhamDTO sp = id > 0 ? _sanPhamBLL.GetById(id) : null;
            if (sp == null)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }
            var vm = new ProductDetailVM
            {
                SanPham = sp,
                LienQuan = _sanPhamBLL.LienQuan(sp, 4)
            };
            int? maKH = User.MaKH();
            if (maKH.HasValue) vm.TrongGio = new GioHangDAL().GetSoLuong(maKH.Value, sp.MaSP);
            return View(vm);
        }
    }
}
