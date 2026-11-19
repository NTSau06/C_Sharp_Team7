using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.10 Đặt hàng, UC 1.2.11 Thanh toán (chọn phương thức thanh toán)</summary>
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly DonHangBLL _donHangBLL = new DonHangBLL();
        private readonly ThanhToanBLL _thanhToanBLL = new ThanhToanBLL();
        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();
        private readonly MaGiamGiaBLL _maGiamGiaBLL = new MaGiamGiaBLL();
        private readonly TaiKhoanBLL _taiKhoanBLL = new TaiKhoanBLL();

        /// <summary>Nạp giỏ hàng, phương thức thanh toán và tính tiền giảm cho form</summary>
        private void NapDuLieu(int maKH, CheckoutVM vm)
        {
            vm.Items = _gioHangBLL.LayGioHang(maKH);
            vm.PhuongThucs = _thanhToanBLL.LayPhuongThuc();
            vm.TamTinh = _gioHangBLL.TinhTamTinh(vm.Items);
            vm.TienGiam = 0;
            vm.ThongBaoMa = null;
            vm.MaGiamGia = MaGiamGiaBLL.ChuanHoaCode(vm.MaGiamGia);
            if (vm.MaGiamGia != "")
            {
                ApDungMaResult kq = _maGiamGiaBLL.KiemTraMa(vm.MaGiamGia, vm.TamTinh);
                vm.ThongBaoMa = kq.ThongBao;
                if (kq.HopLe) vm.TienGiam = kq.TienGiam;
            }
        }

        public IActionResult Index()
        {
            int maKH = User.MaKH().Value;
            var vm = new CheckoutVM { MaGiamGia = HttpContext.Session.GetString(CartController.SessionMaGiamGia) };
            NapDuLieu(maKH, vm);
            if (vm.Items.Count == 0)
            {
                TempData["Warn"] = "Giỏ hàng của bạn đang trống";
                return RedirectToAction("Index", "Cart");
            }
            List<string> thayDoi = _gioHangBLL.DieuChinhTheoTonKho(maKH);
            if (thayDoi.Count > 0)
            {
                TempData["Warn"] = string.Join(" ", thayDoi) + " Vui lòng xem lại giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Điền sẵn thông tin giao hàng từ tài khoản
            KhachHangDTO kh = _taiKhoanBLL.GetById(maKH);
            if (kh != null)
            {
                vm.HoTenNhan = kh.HoTen;
                vm.SDT = kh.SDT;
                vm.DiaChiGiao = kh.DiaChi;
            }
            // Không chọn sẵn phương thức thanh toán: khách phải tự chọn (TC23 - bỏ trống sẽ bị chặn)
            vm.MaPTTT = null;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirm(CheckoutVM vm)
        {
            int maKH = User.MaKH().Value;
            if (!_thanhToanBLL.KiemTraPhuongThuc(vm.MaPTTT))
                ModelState.AddModelError(nameof(CheckoutVM.MaPTTT), "Vui lòng chọn một phương thức thanh toán");

            if (ModelState.IsValid)
            {
                try
                {
                    int maDH = _donHangBLL.DatHang(maKH, vm);
                    HttpContext.Session.Remove(CartController.SessionMaGiamGia);
                    return RedirectToAction(nameof(Success), new { maDH });
                }
                catch (OutOfStockException ex)
                {
                    // BR01-BR02: vượt tồn kho khi kiểm tra lần cuối => quay lại giỏ hàng để điều chỉnh
                    TempData["Warn"] = ex.Message;
                    return RedirectToAction("Index", "Cart");
                }
                catch (GioHangThayDoiException ex)
                {
                    TempData["Warn"] = ex.Message;
                    return RedirectToAction("Index", "Cart");
                }
                catch (VoucherExpiredException ex)
                {
                    // BR06-BR08: mã hết hạn / hết lượt => giao dịch đã rollback, báo lỗi ngay dưới ô mã giảm giá
                    HttpContext.Session.Remove(CartController.SessionMaGiamGia);
                    ModelState.AddModelError(nameof(CheckoutVM.MaGiamGia), ex.Message);
                }
                catch (BusinessException ex)
                {
                    ModelState.AddModelError(ex.TenTruong ?? "", ex.Message);
                }
            }

            NapDuLieu(maKH, vm);
            if (vm.Items.Count == 0) return RedirectToAction("Index", "Cart");
            return View("Index", vm);
        }

        public IActionResult Success(int maDH)
        {
            DonHangDTO dh = _donHangBLL.LayDonHang(User.MaKH().Value, maDH);
            if (dh == null) return RedirectToAction("Orders", "Account");
            return View(dh);
        }
    }
}
