using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportShopWeb.BLL;
using SportShopWeb.DTO;
using SportShopWeb.Models;
using SportShopWeb.Utils;

namespace SportShopWeb.Controllers
{
    /// <summary>UC 1.2.1.1 Đăng nhập, UC 1.2.1.2 Đăng ký (Khách hàng), lịch sử đơn hàng</summary>
    public class AccountController : Controller
    {
        private readonly TaiKhoanBLL _taiKhoanBLL = new TaiKhoanBLL();
        private readonly DonHangBLL _donHangBLL = new DonHangBLL();

        private async Task DangNhapCookie(KhachHangDTO kh)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, kh.TenDangNhap),
                new Claim(ClaimTypes.GivenName, kh.HoTen ?? kh.TenDangNhap),
                new Claim(ClaimTypes.Email, kh.Email ?? ""),
                new Claim(WebHelper.ClaimMaKH, kh.MaKH.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });
        }

        private IActionResult QuayLai(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.MaKH().HasValue) return QuayLai(returnUrl);
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                KhachHangDTO kh = _taiKhoanBLL.DangNhapKH(model.TenDangNhap, model.MatKhau);
                await DangNhapCookie(kh);
                TempData["Success"] = "Xin chào " + kh.HoTen + "!";
                return QuayLai(model.ReturnUrl);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.MatKhau = "";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new RegisterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);
            try
            {
                var kh = new KhachHangDTO
                {
                    HoTen = model.HoTen,
                    TenDangNhap = model.TenDangNhap,
                    Email = model.Email,
                    SDT = model.SDT,
                    DiaChi = model.DiaChi
                };
                kh.MaKH = _taiKhoanBLL.DangKy(kh, model.MatKhau);
                await DangNhapCookie(kh);
                TempData["Success"] = "Đăng ký tài khoản thành công. Chào mừng " + kh.HoTen + "!";
                return QuayLai(returnUrl);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["Success"] = "Bạn đã đăng xuất.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>Lịch sử đơn hàng của khách hàng</summary>
        [Authorize]
        public IActionResult Orders()
        {
            return View(_donHangBLL.LichSu(User.MaKH().Value));
        }
    }
}
