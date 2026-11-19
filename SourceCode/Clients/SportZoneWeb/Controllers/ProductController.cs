using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.9 - Xem chi tiết sản phẩm</summary>
    public class ProductController : Controller
    {
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();
        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();

        /// <summary>A1: hết hàng => khóa ô số lượng + nút thêm vào giỏ (xử lý ở View); A2: id không tồn tại => trang NotFound</summary>
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
            if (maKH.HasValue) vm.TrongGio = _gioHangBLL.LaySoLuong(maKH.Value, sp.MaSP);
            return View(vm);
        }
    }
}
