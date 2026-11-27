using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
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
                // TC08 / TC09: lỗi định dạng, trùng tên đăng nhập / email / SĐT hiển thị ngay dưới ô nhập tương ứng
                ModelState.AddModelError(ex.TenTruong ?? "", ex.Message);
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

        /// <summary>FR18 - Lịch sử đơn hàng, lọc nhanh theo trạng thái (?trangThai=Đang giao)</summary>
        [Authorize]
        public IActionResult Orders(string trangThai = null)
        {
            return View(_donHangBLL.LichSu(User.MaKH().Value, trangThai));
        }

        private AccountManageVM TaoManageVM(int maKH, ProfileVM thongTinOverride = null, ChangePasswordVM matKhauOverride = null)
        {
            KhachHangDTO kh = _taiKhoanBLL.GetById(maKH);
            var vm = new AccountManageVM
            {
                ThongTin = thongTinOverride ?? new ProfileVM
                {
                    TenDangNhap = kh?.TenDangNhap,
                    HoTen = kh?.HoTen,
                    Email = kh?.Email,
                    SDT = kh?.SDT,
                    DiaChi = kh?.DiaChi
                },
                MatKhau = matKhauOverride ?? new ChangePasswordVM(),
                TongDonHang = _donHangBLL.LichSu(maKH).Count,
                DiaChiDaDung = _donHangBLL.DiaChiDaDung(maKH, kh)
            };
            if (thongTinOverride != null && kh != null) vm.ThongTin.TenDangNhap = kh.TenDangNhap;
            return vm;
        }

        /// <summary>Quản lý tài khoản khách hàng: xem / sửa thông tin cá nhân, địa chỉ mặc định và đổi mật khẩu</summary>
        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            int maKH = User.MaKH().Value;
            if (_taiKhoanBLL.GetById(maKH) == null) return RedirectToAction("Login");
            return View(TaoManageVM(maKH));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile([Bind(Prefix = "ThongTin")] ProfileVM model)
        {
            int maKH = User.MaKH().Value;
            if (!ModelState.IsValid)
                return View(TaoManageVM(maKH, thongTinOverride: model));

            try
            {
                KhachHangDTO updated = _taiKhoanBLL.CapNhatThongTin(maKH, new KhachHangDTO
                {
                    HoTen = model.HoTen,
                    Email = model.Email,
                    SDT = model.SDT,
                    DiaChi = model.DiaChi
                });
                await DangNhapCookie(updated);
                TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
                return RedirectToAction(nameof(Profile));
            }
            catch (BusinessException ex)
            {
                string key = string.IsNullOrEmpty(ex.TenTruong) ? "" : "ThongTin." + ex.TenTruong;
                ModelState.AddModelError(key, ex.Message);
                return View(TaoManageVM(maKH, thongTinOverride: model));
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword([Bind(Prefix = "MatKhau")] ChangePasswordVM model)
        {
            int maKH = User.MaKH().Value;
            if (!ModelState.IsValid)
                return View("Profile", TaoManageVM(maKH, matKhauOverride: model));

            try
            {
                _taiKhoanBLL.DoiMatKhau(maKH, model.MatKhauCu, model.MatKhauMoi, model.XacNhanMatKhau);
                TempData["Success"] = "Đổi mật khẩu thành công.";
                return RedirectToAction(nameof(Profile));
            }
            catch (BusinessException ex)
            {
                string key = string.IsNullOrEmpty(ex.TenTruong) ? "" : "MatKhau." + ex.TenTruong;
                ModelState.AddModelError(key, ex.Message);
                return View("Profile", TaoManageVM(maKH, matKhauOverride: model));
            }
        }
    }
}
