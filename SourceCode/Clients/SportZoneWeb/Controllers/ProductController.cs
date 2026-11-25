using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.9 - Xem chi tiết sản phẩm; bổ sung: đánh giá, yêu thích, đã xem gần đây, sản phẩm liên quan</summary>
    public class ProductController : Controller
    {
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();
        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();
        private readonly DanhGiaBLL _danhGiaBLL = new DanhGiaBLL();

        /// <summary>A1: hết hàng => khóa ô số lượng + nút thêm vào giỏ (xử lý ở View); A2: id không tồn tại => trang NotFound</summary>
        public IActionResult Detail(int id)
        {
            SanPhamDTO sp = id > 0 ? _sanPhamBLL.GetById(id) : null;
            if (sp == null)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            // Đã xem gần đây (Session): lấy danh sách TRƯỚC khi ghi sản phẩm hiện tại
            List<int> daXem = HttpContext.Session.DaXem().Where(m => m != sp.MaSP).Take(4).ToList();
            HttpContext.Session.GhiDaXem(sp.MaSP);

            var vm = new ProductDetailVM
            {
                SanPham = sp,
                LienQuan = _sanPhamBLL.LienQuan(sp, 4),
                YeuThich = HttpContext.YeuThich().Contains(sp.MaSP),
                DaXem = _sanPhamBLL.TheoDanhSachMa(daXem),
                DanhGias = _danhGiaBLL.TheoSanPham(sp.MaSP)
            };
            int? maKH = User.MaKH();
            if (maKH.HasValue)
            {
                vm.TrongGio = _gioHangBLL.LaySoLuong(maKH.Value, sp.MaSP);
                vm.DuocDanhGia = _danhGiaBLL.DuocDanhGia(maKH.Value, sp.MaSP);
                vm.DanhGiaCuaToi = vm.DanhGias.FirstOrDefault(d => d.MaKH == maKH.Value);
            }
            return View(vm);
        }

        /// <summary>Gửi đánh giá (1-5 sao + nhận xét) vào bảng DanhGia. Chỉ khách đã mua và đã nhận hàng.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Review(int maSP, int soSao, string noiDung)
        {
            if (_sanPhamBLL.GetById(maSP) == null) return RedirectToAction("Index", "Home");
            try
            {
                _danhGiaBLL.Gui(User.MaKH().Value, maSP, soSao, noiDung);
                TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            }
            catch (BusinessException ex)
            {
                TempData["Warn"] = ex.Message;
            }
            return Redirect(Url.Action("Detail", new { id = maSP }) + "#danh-gia");
        }
    }
}
