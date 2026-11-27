using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>Danh sách yêu thích (bảng YeuThich) - cần đăng nhập</summary>
    public class WishlistController : Controller
    {
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();
        private readonly YeuThichBLL _yeuThichBLL = new YeuThichBLL();

        private bool LaAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        [Authorize]
        public IActionResult Index()
        {
            return View(_sanPhamBLL.TheoDanhSachMa(_yeuThichBLL.LayMaSP(User.MaKH().Value)));
        }

        /// <summary>Thêm / bỏ sản phẩm khỏi yêu thích. Chưa đăng nhập => chuyển tới trang Đăng nhập rồi quay lại.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int maSP, string returnUrl = null)
        {
            string quayLai = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index");
            int? maKH = User.MaKH();
            if (!maKH.HasValue)
            {
                string loginUrl = Url.Action("Login", "Account", new { returnUrl = quayLai });
                if (LaAjax) return Json(new { ok = false, needLogin = true, url = loginUrl, message = "Vui lòng đăng nhập để lưu sản phẩm yêu thích." });
                TempData["Warn"] = "Vui lòng đăng nhập để lưu sản phẩm yêu thích.";
                return Redirect(loginUrl);
            }

            var sp = _sanPhamBLL.GetById(maSP);
            if (sp == null)
            {
                if (LaAjax) return Json(new { ok = false, message = "Sản phẩm không tồn tại." });
                return Redirect(quayLai);
            }
            bool them = _yeuThichBLL.Doi(maKH.Value, maSP);
            string msg = them ? "Đã thêm \"" + sp.TenSP + "\" vào yêu thích." : "Đã bỏ \"" + sp.TenSP + "\" khỏi yêu thích.";
            if (LaAjax) return Json(new { ok = true, active = them, message = msg, count = _yeuThichBLL.LayMaSP(maKH.Value).Count });
            TempData["Success"] = msg;
            return Redirect(quayLai);
        }
    }
}
