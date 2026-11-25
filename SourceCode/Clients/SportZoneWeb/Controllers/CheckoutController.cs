using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>UC 1.2.10 Đặt hàng, UC 1.2.11 Thanh toán (chọn phương thức thanh toán)</summary>
    public class CheckoutController : Controller
    {
        private readonly DonHangBLL _donHangBLL = new DonHangBLL();
        private readonly ThanhToanBLL _thanhToanBLL = new ThanhToanBLL();
        private readonly GioHangBLL _gioHangBLL = new GioHangBLL();
        private readonly MaGiamGiaBLL _maGiamGiaBLL = new MaGiamGiaBLL();
        private readonly TaiKhoanBLL _taiKhoanBLL = new TaiKhoanBLL();
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();

        private Dictionary<int, int> GioTam =>
            HttpContext.Session.GetJson<Dictionary<int, int>>(SessionStore.KeyGioHangKhach) ?? new Dictionary<int, int>();

        /// <summary>Nạp giỏ hàng, phương thức thanh toán và tính tiền giảm cho form</summary>
        private void NapDuLieu(int? maKH, CheckoutVM vm)
        {
            vm.Items = vm.LaMuaNgay
                ? _gioHangBLL.LayMuaNgay(vm.MuaNgayMaSP.Value, vm.MuaNgaySoLuong)
                : maKH.HasValue ? _gioHangBLL.LayGioHang(maKH.Value) : _gioHangBLL.LayGioTam(GioTam);
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

        /// <summary>Điền sẵn thông tin giao hàng từ tài khoản; không chọn sẵn phương thức thanh toán (TC23)</summary>
        private void DienThongTinGiaoHang(int maKH, CheckoutVM vm)
        {
            KhachHangDTO kh = _taiKhoanBLL.GetById(maKH);
            bool daNhapThongTin = !string.IsNullOrWhiteSpace(vm.HoTenNhan) ||
                                  !string.IsNullOrWhiteSpace(vm.SDT) ||
                                  !string.IsNullOrWhiteSpace(vm.DiaChiGiao);
            if (kh != null && !daNhapThongTin)
            {
                vm.HoTenNhan = kh.HoTen;
                vm.SDT = kh.SDT;
                vm.DiaChiGiao = kh.DiaChi;
            }
            // Sổ địa chỉ (bảng DiaChiGiaoHang): ưu tiên địa chỉ mặc định, gửi danh sách cho ô "Chọn từ sổ địa chỉ"
            List<DiaChiVM> soDiaChi = new DiaChiBLL().LayDanhSach(maKH)
                .Select(d => new DiaChiVM { Id = d.MaDC, HoTenNhan = d.HoTenNhan, SDT = d.SDT, DiaChi = d.DiaChi, MacDinh = d.MacDinh }).ToList();
            ViewBag.SoDiaChi = soDiaChi;
            DiaChiVM macDinh = soDiaChi.FirstOrDefault(d => d.MacDinh);
            if (macDinh != null && !daNhapThongTin)
            {
                vm.HoTenNhan = macDinh.HoTenNhan;
                vm.SDT = macDinh.SDT;
                vm.DiaChiGiao = macDinh.DiaChi;
            }
        }

        /// <summary>Trang đặt hàng. Có maSP => "Mua ngay": chỉ đặt sản phẩm đó, không thêm vào giỏ hàng.</summary>
        public IActionResult Index(int? maSP = null, int soLuong = 1)
        {
            int? maKH = User.MaKH();
            CheckoutVM pending = HttpContext.Session.GetJson<CheckoutVM>(SessionStore.KeyCheckoutChoDangNhap);
            if (maSP.HasValue)
                return MuaNgay(maKH, maSP.Value, soLuong, pending);

            var vm = pending ?? new CheckoutVM { MaGiamGia = HttpContext.Session.GetString(CartController.SessionMaGiamGia) };
            NapDuLieu(maKH, vm);
            if (vm.Items.Count == 0)
            {
                TempData["Warn"] = "Giỏ hàng của bạn đang trống";
                return RedirectToAction("Index", "Cart");
            }
            List<string> thayDoi = maKH.HasValue
                ? _gioHangBLL.DieuChinhTheoTonKho(maKH.Value)
                : _gioHangBLL.DieuChinhGioTam(GioTam);
            if (thayDoi.Count > 0)
            {
                if (!maKH.HasValue) HttpContext.Session.SetJson(SessionStore.KeyGioHangKhach, GioTam);
                TempData["Warn"] = string.Join(" ", thayDoi) + " Vui lòng xem lại giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            if (maKH.HasValue)
                DienThongTinGiaoHang(maKH.Value, vm);
            else
                ViewBag.SoDiaChi = new List<DiaChiVM>();
            HttpContext.Session.Remove(SessionStore.KeyCheckoutChoDangNhap);
            return View(vm);
        }

        private IActionResult MuaNgay(int? maKH, int maSP, int soLuong, CheckoutVM pending = null)
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
            CheckoutVM vm = pending ?? new CheckoutVM();
            vm.MuaNgayMaSP = maSP;
            vm.MuaNgaySoLuong = Math.Clamp(soLuong, 1, toiDa);
            NapDuLieu(maKH, vm);
            if (maKH.HasValue)
                DienThongTinGiaoHang(maKH.Value, vm);
            else
                ViewBag.SoDiaChi = new List<DiaChiVM>();
            HttpContext.Session.Remove(SessionStore.KeyCheckoutChoDangNhap);
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
            int? maKH = User.MaKH();
            List<PhuongThucThanhToanDTO> phuongThucs = _thanhToanBLL.LayPhuongThuc();
            PhuongThucThanhToanDTO phuongThuc = phuongThucs.FirstOrDefault(p => p.MaPTTT == vm.MaPTTT);
            if (phuongThuc == null)
                ModelState.AddModelError(nameof(CheckoutVM.MaPTTT), "Vui lòng chọn một phương thức thanh toán");
            else
            {
                string cong = CongThanhToan.NhanDang(phuongThuc.TenPTTT);
                if (!CongThanhToan.DemoMode && cong == CongThanhToan.VNPAY && !CongThanhToan.VnPayDaCauHinh)
                    ModelState.AddModelError(nameof(CheckoutVM.MaPTTT), "VNPay chưa được cấu hình. Vui lòng liên hệ cửa hàng hoặc chọn phương thức khác.");
                else if (!CongThanhToan.DemoMode && cong == CongThanhToan.MOMO && !CongThanhToan.MoMoDaCauHinh)
                    ModelState.AddModelError(nameof(CheckoutVM.MaPTTT), "MoMo chưa được cấu hình. Vui lòng liên hệ cửa hàng hoặc chọn phương thức khác.");
            }

            if (!maKH.HasValue && ModelState.IsValid)
            {
                HttpContext.Session.SetJson(SessionStore.KeyCheckoutChoDangNhap, new CheckoutVM
                {
                    HoTenNhan = vm.HoTenNhan,
                    SDT = vm.SDT,
                    DiaChiGiao = vm.DiaChiGiao,
                    MaPTTT = vm.MaPTTT,
                    MaGiamGia = vm.MaGiamGia,
                    MuaNgayMaSP = vm.MuaNgayMaSP,
                    MuaNgaySoLuong = vm.MuaNgaySoLuong
                });
                string returnUrl = Url.Action(nameof(Index), new { maSP = vm.MuaNgayMaSP, soLuong = vm.MuaNgaySoLuong });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    int maDH = _donHangBLL.DatHang(maKH.Value, vm);
                    HttpContext.Session.Remove(CartController.SessionMaGiamGia);
                    if (CongThanhToan.NhanDang(phuongThuc.TenPTTT) != null)
                        return RedirectToAction("Start", "Payment", new { maDH });
                    _donHangBLL.GuiEmailXacNhan(maKH.Value, maDH);
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
            if (maKH.HasValue)
                DienThongTinGiaoHang(maKH.Value, vm);
            else
                ViewBag.SoDiaChi = new List<DiaChiVM>();
            return View("Index", vm);
        }

        [Authorize]
        public IActionResult Success(int maDH)
        {
            DonHangDTO dh = _donHangBLL.LayDonHang(User.MaKH().Value, maDH);
            if (dh == null) return RedirectToAction("Orders", "Account");
            if (CongThanhToan.NhanDang(dh.TenPTTT) != null &&
                dh.TrangThaiThanhToan != TrangThaiThanhToanDH.Da)
            {
                if (dh.TrangThai == TrangThaiDonHang.DaHuy)
                    return RedirectToAction("OrderDetail", "Account", new { id = maDH });
                return RedirectToAction("Start", "Payment", new { maDH });
            }
            return View(dh);
        }
    }
}
