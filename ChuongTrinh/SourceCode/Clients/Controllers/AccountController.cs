using System.Security.Claims;
using System.Security.Cryptography;
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
    /// <summary>
    /// UC 1.2.1.1 Đăng nhập, UC 1.2.1.2 Đăng ký (Khách hàng), lịch sử đơn hàng.
    /// Bổ sung: hồ sơ cá nhân, đổi mật khẩu, quên mật khẩu (OTP), sổ địa chỉ, chi tiết + theo dõi đơn, hủy đơn, mua lại.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly TaiKhoanBLL _taiKhoanBLL = new TaiKhoanBLL();
        private readonly DonHangBLL _donHangBLL = new DonHangBLL();
        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();
        private readonly DiaChiBLL _diaChiBLL = new DiaChiBLL();

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

        private void HopNhatGioKhachVaoTaiKhoan(int maKH)
        {
            Dictionary<int, int> gioKhach = HttpContext.Session.GetJson<Dictionary<int, int>>(SessionStore.KeyGioHangKhach);
            if (gioKhach == null || gioKhach.Count == 0) return;

            var loi = new List<string>();
            foreach (KeyValuePair<int, int> item in gioKhach)
            {
                if (item.Value < 1) continue;
                KetQua kq = _gioHangBLL.ThemVaoGio(maKH, item.Key, item.Value);
                if (!kq.ThanhCong) loi.Add(kq.ThongBao);
            }

            HttpContext.Session.Remove(SessionStore.KeyGioHangKhach);
            if (loi.Count > 0)
                TempData["Warn"] = "Một số sản phẩm trong giỏ khách chưa thể gộp vào tài khoản: " + string.Join(" ", loi);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (Request.Query["blocked"] == "1")
                TempData["Warn"] = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ cửa hàng để được hỗ trợ.";
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
                HopNhatGioKhachVaoTaiKhoan(kh.MaKH);
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
                HopNhatGioKhachVaoTaiKhoan(kh.MaKH);
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

        // ================================================================== HỒ SƠ CÁ NHÂN (bảng KhachHang)

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            KhachHangDTO kh = _taiKhoanBLL.GetById(User.MaKH().Value);
            if (kh == null) return RedirectToAction("Login");
            return View(new HoSoVM { TenDangNhap = kh.TenDangNhap, HoTen = kh.HoTen, SDT = kh.SDT, Email = kh.Email, DiaChi = kh.DiaChi });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(HoSoVM model)
        {
            KhachHangDTO kh = _taiKhoanBLL.GetById(User.MaKH().Value);
            if (kh == null) return RedirectToAction("Login");
            model.TenDangNhap = kh.TenDangNhap;
            if (!ModelState.IsValid) return View(model);
            try
            {
                _taiKhoanBLL.CapNhatHoSo(kh, model.HoTen, model.SDT, model.Email, model.DiaChi);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(ex.TenTruong ?? "", ex.Message);
                return View(model);
            }
            // Cập nhật tên hiển thị trên thanh menu
            await DangNhapCookie(_taiKhoanBLL.GetById(kh.MaKH));
            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("Profile");
        }

        // ================================================================== ĐỔI MẬT KHẨU

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new DoiMatKhauVM());

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(DoiMatKhauVM model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                _taiKhoanBLL.DoiMatKhau(User.MaKH().Value, model.MatKhauCu, model.MatKhauMoi);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(ex.TenTruong ?? "", ex.Message);
                return View(new DoiMatKhauVM());
            }
            TempData["Success"] = "Đổi mật khẩu thành công. Lần đăng nhập sau hãy dùng mật khẩu mới.";
            return RedirectToAction("Profile");
        }

        // ================================================================== QUÊN MẬT KHẨU (OTP lưu trong Session)

        [HttpGet]
        public IActionResult ForgotPassword() => View(new QuenMatKhauVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(QuenMatKhauVM model)
        {
            if (!ModelState.IsValid) return View(model);
            KhachHangDTO kh;
            try { kh = _taiKhoanBLL.TimTaiKhoan(model.TenDangNhapHoacEmail); }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(nameof(model.TenDangNhapHoacEmail), ex.Message);
                return View(model);
            }

            var otp = new OtpInfo
            {
                MaKH = kh.MaKH,
                Ma = RandomNumberGenerator.GetInt32(0, 1000000).ToString("000000"),
                HetHan = DateTime.Now.AddMinutes(5),
                EmailAn = AnEmail(kh.Email)
            };
            HttpContext.Session.SetJson(SessionStore.KeyOtp, otp);
            // TODO: gửi mã OTP qua email (cần cấu hình SMTP). Bản hiện tại hiển thị mã để chạy thử.
            TempData["Success"] = "Mã OTP đã được gửi tới email " + otp.EmailAn + ". Mã có hiệu lực trong 5 phút.";
            TempData["OtpDemo"] = otp.Ma;
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            OtpInfo otp = HttpContext.Session.GetJson<OtpInfo>(SessionStore.KeyOtp);
            if (otp == null) return RedirectToAction("ForgotPassword");
            return View(new DatLaiMatKhauVM { EmailAn = otp.EmailAn });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(DatLaiMatKhauVM model)
        {
            OtpInfo otp = HttpContext.Session.GetJson<OtpInfo>(SessionStore.KeyOtp);
            if (otp == null)
            {
                TempData["Warn"] = "Phiên đặt lại mật khẩu đã kết thúc. Vui lòng yêu cầu mã OTP mới.";
                return RedirectToAction("ForgotPassword");
            }
            model.EmailAn = otp.EmailAn;
            TempData["OtpDemo"] = otp.Ma;   // bản chạy thử: vẫn hiện mã khi phải nhập lại
            if (!ModelState.IsValid) return View(model);

            if (DateTime.Now > otp.HetHan)
            {
                HttpContext.Session.Remove(SessionStore.KeyOtp);
                TempData["Warn"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
                return RedirectToAction("ForgotPassword");
            }
            if (model.MaOTP != otp.Ma)
            {
                otp.SoLanSai++;
                if (otp.SoLanSai >= 5)
                {
                    HttpContext.Session.Remove(SessionStore.KeyOtp);
                    TempData["Warn"] = "Nhập sai mã OTP quá 5 lần. Vui lòng yêu cầu mã mới.";
                    return RedirectToAction("ForgotPassword");
                }
                HttpContext.Session.SetJson(SessionStore.KeyOtp, otp);
                ModelState.AddModelError(nameof(model.MaOTP), "Mã OTP không đúng (còn " + (5 - otp.SoLanSai) + " lần thử).");
                return View(model);
            }

            try { _taiKhoanBLL.DatLaiMatKhau(otp.MaKH, model.MatKhauMoi); }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(ex.TenTruong ?? "", ex.Message);
                return View(model);
            }
            HttpContext.Session.Remove(SessionStore.KeyOtp);
            TempData.Remove("OtpDemo");
            TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction("Login");
        }

        /// <summary>nguyenvana@gmail.com => ng*******@gmail.com</summary>
        private static string AnEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@')) return "đã đăng ký";
            int at = email.IndexOf('@');
            string ten = email.Substring(0, at);
            string giu = ten.Length <= 2 ? ten.Substring(0, 1) : ten.Substring(0, 2);
            return giu + new string('*', Math.Max(3, ten.Length - giu.Length)) + email.Substring(at);
        }

        // ================================================================== SỔ ĐỊA CHỈ (bảng DiaChiGiaoHang)

        private static DiaChiVM ToVM(DiaChiGiaoHangDTO d) =>
            new DiaChiVM { Id = d.MaDC, HoTenNhan = d.HoTenNhan, SDT = d.SDT, DiaChi = d.DiaChi, MacDinh = d.MacDinh };

        private List<DiaChiVM> LayDiaChi() => _diaChiBLL.LayDanhSach(User.MaKH().Value).Select(ToVM).ToList();

        [Authorize]
        [HttpGet]
        public IActionResult Addresses(int? sua = null)
        {
            var vm = new SoDiaChiVM { DanhSach = LayDiaChi() };
            DiaChiVM dc = sua.HasValue ? vm.DanhSach.FirstOrDefault(d => d.Id == sua.Value) : null;
            if (dc != null)
                vm.Form = new DiaChiVM { Id = dc.Id, HoTenNhan = dc.HoTenNhan, SDT = dc.SDT, DiaChi = dc.DiaChi, MacDinh = dc.MacDinh };
            else
            {
                KhachHangDTO kh = _taiKhoanBLL.GetById(User.MaKH().Value);
                vm.Form = new DiaChiVM { HoTenNhan = kh?.HoTen, SDT = kh?.SDT, MacDinh = vm.DanhSach.Count == 0 };
            }
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveAddress([Bind(Prefix = "Form")] DiaChiVM model)
        {
            if (!ModelState.IsValid) return View("Addresses", new SoDiaChiVM { DanhSach = LayDiaChi(), Form = model });
            try
            {
                _diaChiBLL.Luu(new DiaChiGiaoHangDTO
                {
                    MaDC = model.Id,
                    MaKH = User.MaKH().Value,
                    HoTenNhan = model.HoTenNhan,
                    SDT = model.SDT,
                    DiaChi = model.DiaChi
                }, model.MacDinh);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Addresses", new SoDiaChiVM { DanhSach = LayDiaChi(), Form = model });
            }
            TempData["Success"] = model.Id == 0 ? "Đã thêm địa chỉ mới." : "Đã cập nhật địa chỉ.";
            return RedirectToAction("Addresses");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAddress(int id)
        {
            if (_diaChiBLL.Xoa(User.MaKH().Value, id)) TempData["Success"] = "Đã xóa địa chỉ.";
            return RedirectToAction("Addresses");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetDefaultAddress(int id)
        {
            _diaChiBLL.DatMacDinh(User.MaKH().Value, id);
            TempData["Success"] = "Đã đặt làm địa chỉ mặc định.";
            return RedirectToAction("Addresses");
        }

        // ================================================================== ĐƠN HÀNG

        /// <summary>FR18 - Lịch sử đơn hàng, lọc nhanh theo trạng thái (?trangThai=Đang giao)</summary>
        [Authorize]
        public IActionResult Orders(string trangThai = null)
        {
            return View(_donHangBLL.LichSu(User.MaKH().Value, trangThai));
        }

        /// <summary>Trang chi tiết một đơn hàng + thanh theo dõi trạng thái (mốc thời gian từ bảng LichSuDonHang)</summary>
        [Authorize]
        public IActionResult OrderDetail(int id)
        {
            DonHangDTO dh = _donHangBLL.LayDonHang(User.MaKH().Value, id);
            if (dh == null)
            {
                TempData["Warn"] = "Không tìm thấy đơn hàng #DH" + id + ".";
                return RedirectToAction("Orders");
            }
            return View(new ChiTietDonVM
            {
                DonHang = dh,
                Moc = ChiTietDonVM.TaoMoc(dh, _donHangBLL.LichSuTrangThai(dh.MaDH)),
                CoTheHuy = dh.TrangThai == TrangThaiDonHang.ChoXacNhan
            });
        }

        /// <summary>Hủy đơn: chỉ khi đơn còn "Chờ xác nhận"; hoàn lượt dùng mã giảm giá</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id, string lyDo, string lyDoKhac)
        {
            string ly = lyDo == "Lý do khác" ? lyDoKhac : lyDo;
            try
            {
                DonHangDTO dh = _donHangBLL.LayDonHang(User.MaKH().Value, id);
                _donHangBLL.HuyDon(User.MaKH().Value, id, ly);
                TempData["Success"] = "Đã hủy đơn hàng #DH" + id + "." +
                    (dh?.MaMGG != null ? " Lượt dùng mã " + dh.CodeMGG + " đã được hoàn lại." : "");
            }
            catch (BusinessException ex)
            {
                TempData["Warn"] = ex.Message;
            }
            return RedirectToAction("OrderDetail", new { id });
        }

        /// <summary>Mua lại: thêm các sản phẩm của đơn cũ vào giỏ hàng (kiểm tra tồn kho như khi thêm bình thường)</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reorder(int id)
        {
            int maKH = User.MaKH().Value;
            DonHangDTO dh = _donHangBLL.LayDonHang(maKH, id);
            if (dh == null) return RedirectToAction("Orders");

            int thanhCong = 0;
            var loi = new List<string>();
            foreach (ChiTietDonHangDTO ct in dh.ChiTiet)
            {
                KetQua kq = _gioHangBLL.ThemVaoGio(maKH, ct.MaSP, ct.SoLuong);
                if (kq.ThanhCong) thanhCong++;
                else loi.Add(kq.ThongBao);
            }
            if (thanhCong > 0)
                TempData["Success"] = "Đã thêm " + thanhCong + "/" + dh.ChiTiet.Count + " sản phẩm của đơn #DH" + id + " vào giỏ hàng.";
            if (loi.Count > 0)
                TempData["Warn"] = string.Join(" ", loi);
            return RedirectToAction("Index", "Cart");
        }
    }
}
