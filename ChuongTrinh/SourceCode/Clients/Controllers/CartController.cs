using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.10 - Giỏ hàng; UC 1.2.14 - Áp dụng mã giảm giá</summary>
    public class CartController : Controller
    {
        public const string SessionMaGiamGia = "MaGiamGia";

        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();
        private readonly MaGiamGiaBLL _maGiamGiaBLL = new MaGiamGiaBLL();

        private bool LaAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        private Dictionary<int, int> GioTam =>
            HttpContext.Session.GetJson<Dictionary<int, int>>(SessionStore.KeyGioHangKhach) ?? new Dictionary<int, int>();

        /// <summary>Tạm tính của giỏ hàng, hoặc của riêng sản phẩm "Mua ngay" khi có maSP</summary>
        private decimal TamTinh(int? maKH, int? maSP, int soLuong)
        {
            if (maSP.HasValue)
                return _gioHangBLL.TinhTamTinh(_gioHangBLL.LayMuaNgay(maSP.Value, soLuong));
            if (maKH.HasValue)
                return _gioHangBLL.TinhTamTinh(maKH.Value);
            return _gioHangBLL.TinhTamTinh(_gioHangBLL.LayGioTam(GioTam));
        }

        public IActionResult Index()
        {
            int? maKH = User.MaKH();
            List<GioHangItemVM> items = maKH.HasValue
                ? _gioHangBLL.LayGioHang(maKH.Value)
                : _gioHangBLL.LayGioTam(GioTam);
            var vm = new CartVM { Items = items };
            vm.TamTinh = _gioHangBLL.TinhTamTinh(vm.Items);

            string code = HttpContext.Session.GetString(SessionMaGiamGia);
            if (!string.IsNullOrEmpty(code) && vm.Items.Count > 0)
            {
                ApDungMaResult kq = _maGiamGiaBLL.KiemTraMa(code, vm.TamTinh);
                vm.MaGiamGia = code;
                vm.ThongBaoMa = kq.ThongBao;
                if (kq.HopLe) vm.TienGiam = kq.TienGiam;
            }
            try { vm.MaGoiY = _maGiamGiaBLL.DangHieuLuc(); } catch { /* không bắt buộc */ }
            return View(vm);
        }

        /// <summary>Thêm sản phẩm vào giỏ tạm của khách hoặc giỏ tài khoản.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int maSP, int soLuong = 1, string returnUrl = null)
        {
            string quayLai = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Detail", "Product", new { id = maSP });
            int? maKH = User.MaKH();
            KetQua kq;
            int count;
            if (maKH.HasValue)
            {
                kq = _gioHangBLL.ThemVaoGio(maKH.Value, maSP, soLuong);
                count = _gioHangBLL.DemSoLuong(maKH.Value);
            }
            else
            {
                Dictionary<int, int> gio = GioTam;
                kq = _gioHangBLL.ThemVaoGioTam(gio, maSP, soLuong);
                HttpContext.Session.SetJson(SessionStore.KeyGioHangKhach, gio);
                count = _gioHangBLL.DemSoLuongGioTam(gio);
            }

            if (LaAjax)
                return Json(new { ok = kq.ThanhCong, message = kq.ThongBao, count });
            TempData[kq.ThanhCong ? "Success" : "Warn"] = kq.ThongBao;
            return Redirect(quayLai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int maSP, int soLuong)
        {
            int? maKH = User.MaKH();
            KetQua kq;
            if (maKH.HasValue)
                kq = _gioHangBLL.CapNhatSoLuong(maKH.Value, maSP, soLuong);
            else
            {
                Dictionary<int, int> gio = GioTam;
                kq = _gioHangBLL.CapNhatGioTam(gio, maSP, soLuong);
                HttpContext.Session.SetJson(SessionStore.KeyGioHangKhach, gio);
            }
            TempData[kq.ThanhCong ? "Success" : "Warn"] = kq.ThongBao;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int maSP)
        {
            int? maKH = User.MaKH();
            if (maKH.HasValue)
                _gioHangBLL.XoaKhoiGio(maKH.Value, maSP);
            else
            {
                Dictionary<int, int> gio = GioTam;
                gio.Remove(maSP);
                HttpContext.Session.SetJson(SessionStore.KeyGioHangKhach, gio);
            }
            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Áp dụng mã giảm giá (AJAX) - lưu mã vào Session để dùng khi đặt hàng</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApplyVoucher(string code, int? maSP = null, int soLuong = 1)
        {
            decimal tamTinh = TamTinh(User.MaKH(), maSP, soLuong);
            code = MaGiamGiaBLL.ChuanHoaCode(code);
            if (code == "")
            {
                HttpContext.Session.Remove(SessionMaGiamGia);
                return Json(new { hopLe = false, thongBao = "Vui lòng nhập mã giảm giá", tienGiam = WebHelper.Tien(0), tongCong = WebHelper.Tien(tamTinh) });
            }
            if (tamTinh <= 0)
                return Json(new { hopLe = false, thongBao = "Giỏ hàng của bạn đang trống", tienGiam = WebHelper.Tien(0), tongCong = WebHelper.Tien(0) });

            ApDungMaResult kq = _maGiamGiaBLL.KiemTraMa(code, tamTinh);
            if (kq.HopLe) HttpContext.Session.SetString(SessionMaGiamGia, code);
            else HttpContext.Session.Remove(SessionMaGiamGia);
            decimal giam = kq.HopLe ? kq.TienGiam : 0;
            return Json(new
            {
                hopLe = kq.HopLe,
                thongBao = kq.ThongBao,
                code,
                tienGiam = WebHelper.Tien(giam),
                tongCong = WebHelper.Tien(Math.Max(0, tamTinh - giam))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveVoucher(string returnUrl = null, int? maSP = null, int soLuong = 1)
        {
            HttpContext.Session.Remove(SessionMaGiamGia);
            if (LaAjax)
            {
                decimal tamTinh = TamTinh(User.MaKH(), maSP, soLuong);
                return Json(new { ok = true, thongBao = "Đã bỏ mã giảm giá", tienGiam = WebHelper.Tien(0), tongCong = WebHelper.Tien(tamTinh) });
            }
            TempData["Success"] = "Đã bỏ mã giảm giá.";
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }
    }
}
