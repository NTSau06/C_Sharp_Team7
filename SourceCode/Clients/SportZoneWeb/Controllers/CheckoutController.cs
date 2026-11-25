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
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();

        /// <summary>Nạp giỏ hàng, thông tin địa chỉ tài khoản/đã dùng, phương thức thanh toán và tính tiền giảm cho form</summary>
        private void NapDuLieu(int maKH, CheckoutVM vm)
        {
            vm.Items = vm.LaMuaNgay
                ? _gioHangBLL.LayMuaNgay(vm.MuaNgayMaSP.Value, vm.MuaNgaySoLuong)
                : _gioHangBLL.LayGioHang(maKH);
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

            KhachHangDTO kh = _taiKhoanBLL.GetById(maKH);
            vm.TaiKhoanMacDinh = kh == null
                ? new DiaChiGiaoItem()
                : new DiaChiGiaoItem { HoTen = kh.HoTen, SDT = kh.SDT, DiaChi = kh.DiaChi };
            vm.DiaChiDaDung = _donHangBLL.DiaChiDaDung(maKH, kh);
            if (string.IsNullOrWhiteSpace(vm.ChonDiaChi)) vm.ChonDiaChi = "account";
        }

        /// <summary>Điền sẵn thông tin giao hàng từ tài khoản đã đăng ký; không chọn sẵn phương thức thanh toán (TC23)</summary>
        private void DienThongTinGiaoHang(int maKH, CheckoutVM vm)
        {
            vm.ChonDiaChi = "account";
            vm.HoTenNhan = vm.TaiKhoanMacDinh.HoTen;
            vm.SDT = vm.TaiKhoanMacDinh.SDT;
            vm.DiaChiGiao = vm.TaiKhoanMacDinh.DiaChi;
            vm.LuuDiaChiMacDinh = !vm.TaiKhoanMacDinh.CoDiaChi;
            vm.MaPTTT = null;
        }

        /// <summary>Trang đặt hàng. Có maSP => "Mua ngay": chỉ đặt sản phẩm đó, không thêm vào giỏ hàng.</summary>
        public IActionResult Index(int? maSP = null, int soLuong = 1)
        {
            int maKH = User.MaKH().Value;
            if (maSP.HasValue) return MuaNgay(maKH, maSP.Value, soLuong);

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

            DienThongTinGiaoHang(maKH, vm);
            return View(vm);
        }

        private IActionResult MuaNgay(int maKH, int maSP, int soLuong)
        {
            SanPhamDTO sp = maSP > 0 ? _sanPhamBLL.GetById(maSP) : null;
            if (sp == null)
            {
                TempData["Warn"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }
            if (sp.HetHang)
            {
                TempData["Warn"] = "Sản phẩm \"" + sp.TenSP + "\" đã hết hàng.";
                return RedirectToAction("Detail", "Product", new { id = maSP });
            }
            int toiDa = Math.Min(sp.SoLuongTon, GioHangBLL.SoLuongToiDaMoiSanPham);
            if (soLuong > toiDa) TempData["Warn"] = GioHangBLL.ThongBaoVuotTonKho(sp.SoLuongTon) + ". Đã chỉnh về " + toiDa + ".";
            var vm = new CheckoutVM { MuaNgayMaSP = maSP, MuaNgaySoLuong = Math.Clamp(soLuong, 1, toiDa) };
            NapDuLieu(maKH, vm);
            DienThongTinGiaoHang(maKH, vm);
            return View("Index", vm);
        }

        /// <summary>Lỗi tồn kho / sản phẩm thay đổi: Mua ngay => về trang sản phẩm, đặt cả giỏ => về giỏ hàng</summary>
        private IActionResult QuayLaiKhiLoi(CheckoutVM vm, string thongBao)
        {
            TempData["Warn"] = thongBao;
            return vm.LaMuaNgay
                ? RedirectToAction("Detail", "Product", new { id = vm.MuaNgayMaSP })
                : RedirectToAction("Index", "Cart");
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
                    if (vm.LuuDiaChiMacDinh && !string.IsNullOrWhiteSpace(vm.DiaChiGiao))
                        _taiKhoanBLL.CapNhatDiaChiMacDinh(maKH, vm.DiaChiGiao);
                    HttpContext.Session.Remove(CartController.SessionMaGiamGia);
                    return RedirectToAction(nameof(Success), new { maDH });
                }
                catch (OutOfStockException ex)
                {
                    // BR01-BR02: vượt tồn kho khi kiểm tra lần cuối => quay lại để điều chỉnh
                    return QuayLaiKhiLoi(vm, ex.Message);
                }
                catch (GioHangThayDoiException ex)
                {
                    return QuayLaiKhiLoi(vm, ex.Message);
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
            if (vm.Items.Count == 0) return QuayLaiKhiLoi(vm, "Không còn sản phẩm để đặt hàng.");
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
