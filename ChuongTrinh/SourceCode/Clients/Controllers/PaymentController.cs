using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SportZoneWeb.BLL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>
    /// UC 1.2.11 Thanh toán online qua VNPay hoặc MoMo (sandbox).
    /// Start: tạo giao dịch và chuyển khách sang cổng thanh toán.
    /// *Return: khách quay lại website (kiểm tra chữ ký + số tiền). *Ipn: cổng gọi máy chủ -> máy chủ (chạy được khi website có địa chỉ công khai).
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly DonHangBLL _donHangBLL = new DonHangBLL();
        private readonly ThanhToanBLL _thanhToanBLL = new ThanhToanBLL();

        private string DiaChiTuyetDoi(string action) => Url.Action(action, "Payment", null, Request.Scheme, Request.Host.Value);

        // ------------------------------------------------------------------ bắt đầu thanh toán / thanh toán lại
        [Authorize]
        public async Task<IActionResult> Start(int maDH)
        {
            DonHangDTO dh = _donHangBLL.LayDonHang(User.MaKH().Value, maDH);
            if (dh == null) return RedirectToAction("Orders", "Account");
            string cong = CongThanhToan.NhanDang(dh.TenPTTT);
            if (dh.TrangThai == TrangThaiDonHang.DaHuy)
                return RedirectToAction("OrderDetail", "Account", new { id = maDH });
            if (cong == null || dh.TrangThaiThanhToan == TrangThaiThanhToanDH.Da)
                return RedirectToAction("Success", "Checkout", new { maDH });
            if (CongThanhToan.DemoMode)
                return TaoThanhToanDemo(dh);

            if (cong == CongThanhToan.VNPAY)
            {
                if (!CongThanhToan.VnPayDaCauHinh)
                    return ChuaCauHinh(maDH, "VNPay");
                string ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                return Redirect(CongThanhToan.VnPayTaoUrl(maDH, dh.TongTien, ip, DiaChiTuyetDoi("VnPayReturn")));
            }

            if (!CongThanhToan.MoMoDaCauHinh)
                return ChuaCauHinh(maDH, "MoMo");
            var (payUrl, loi) = await CongThanhToan.MoMoTaoUrl(maDH, dh.TongTien, DiaChiTuyetDoi("MoMoReturn"), DiaChiTuyetDoi("MoMoIpn"));
            if (payUrl == null)
            {
                TempData["Warn"] = "Không tạo được giao dịch MoMo: " + loi + " Bạn có thể thanh toán lại trong \"Đơn hàng của tôi\".";
                return RedirectToAction("OrderDetail", "Account", new { id = maDH });
            }
            return Redirect(payUrl);
        }

        private IActionResult TaoThanhToanDemo(DonHangDTO dh)
        {
            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string maXacThuc = MaXacThucDemo(token);
            if (!_thanhToanBLL.BatDauThanhToanDemo(dh.MaDH, maXacThuc))
            {
                TempData["Warn"] = "Không thể bắt đầu thanh toán demo cho đơn hàng này.";
                return RedirectToAction("OrderDetail", "Account", new { id = dh.MaDH });
            }

            Uri baseUri = null;
            string baseUrl = CongThanhToan.Cfg["DemoPayment:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri) ||
                 (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)))
                throw new InvalidOperationException("DemoPayment:BaseUrl phải là URL tuyệt đối dùng HTTP hoặc HTTPS.");

            string protocol = baseUri?.Scheme ?? Request.Scheme;
            string host = baseUri?.Authority ?? Request.Host.Value;
            string scanUrl = Url.Action(nameof(DemoScan), "Payment", new { maDH = dh.MaDH, token }, protocol, host);
            string statusUrl = Url.Action(nameof(DemoStatus), "Payment", new { maDH = dh.MaDH, token }, Request.Scheme, Request.Host.Value);

            using QRCodeData qrData = QRCodeGenerator.GenerateQrCode(scanUrl, QRCodeGenerator.ECCLevel.Q);
            using var pngQr = new PngByteQRCode(qrData);
            var vm = new DemoPaymentVM
            {
                MaDH = dh.MaDH,
                TongTien = dh.TongTien,
                TenCong = CongThanhToan.NhanDang(dh.TenPTTT),
                QrDataUri = "data:image/png;base64," + Convert.ToBase64String(pngQr.GetGraphic(10)),
                ScanUrl = scanUrl,
                StatusUrl = statusUrl,
                SuccessUrl = Url.Action("Success", "Checkout", new { maDH = dh.MaDH }, Request.Scheme, Request.Host.Value)
            };
            return View("Demo", vm);
        }

        [HttpGet]
        public IActionResult DemoScan(int maDH, string token)
        {
            if (!CongThanhToan.DemoMode || string.IsNullOrWhiteSpace(token)) return NotFound();
            DonHangDTO dh = _thanhToanBLL.GhiNhanThanhToanDemo(maDH, MaXacThucDemo(token));
            if (dh == null) return NotFound();
            return View("DemoScan", new DemoPaymentScanVM
            {
                MaDH = maDH,
                ThanhToanThanhCong = dh.TrangThaiThanhToan == TrangThaiThanhToanDH.Da
            });
        }

        [HttpGet]
        public IActionResult DemoStatus(int maDH, string token)
        {
            if (!CongThanhToan.DemoMode || string.IsNullOrWhiteSpace(token)) return NotFound();
            DonHangDTO dh = _thanhToanBLL.LayDonHangDemo(maDH, MaXacThucDemo(token));
            if (dh == null) return NotFound();
            return Json(new { paid = dh.TrangThaiThanhToan == TrangThaiThanhToanDH.Da });
        }

        private static string MaXacThucDemo(string token)
        {
            using SHA256 sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        private IActionResult ChuaCauHinh(int maDH, string ten)
        {
            TempData["Warn"] = "Cổng " + ten + " chưa được cấu hình trên website. Đơn #DH" + maDH + " chưa được thanh toán; vui lòng liên hệ cửa hàng hoặc thử lại sau.";
            return RedirectToAction("OrderDetail", "Account", new { id = maDH });
        }

        /// <summary>Hiển thị kết quả sau khi quay lại từ cổng: về trang đơn hàng kèm thông báo</summary>
        private IActionResult KetQua(DonHangDTO dh, bool thanhCong, string loiCong)
        {
            if (dh == null) return RedirectToAction("Orders", "Account");
            if (thanhCong || dh.TrangThaiThanhToan == TrangThaiThanhToanDH.Da)
            {
                _donHangBLL.GuiEmailXacNhan(dh.MaKH, dh.MaDH);
                TempData["Success"] = "Thanh toán đơn #DH" + dh.MaDH + " thành công. Cảm ơn bạn!";
                return RedirectToAction("Success", "Checkout", new { maDH = dh.MaDH });
            }
            TempData["Warn"] = (loiCong ?? "Thanh toán không thành công.") + " Đơn #DH" + dh.MaDH + " vẫn được giữ, bạn có thể thanh toán lại.";
            return RedirectToAction("OrderDetail", "Account", new { id = dh.MaDH });
        }

        // ------------------------------------------------------------------ VNPay
        [HttpGet]
        public IActionResult VnPayReturn()
        {
            if (!CongThanhToan.VnPayDaCauHinh || !CongThanhToan.VnPayHopLe(Request.Query))
            {
                TempData["Warn"] = "Dữ liệu trả về từ VNPay không hợp lệ.";
                return RedirectToAction("Orders", "Account");
            }
            int? maDH = CongThanhToan.MaDHTuMaGiaoDich(Request.Query["vnp_TxnRef"]);
            if (!maDH.HasValue) return RedirectToAction("Orders", "Account");
            bool ok = Request.Query["vnp_ResponseCode"] == "00" && Request.Query["vnp_TransactionStatus"] == "00";
            decimal.TryParse(Request.Query["vnp_Amount"], out decimal amount);
            DonHangDTO dh = _thanhToanBLL.GhiNhanKetQua(maDH.Value, ok, amount / 100, Request.Query["vnp_TransactionNo"]);
            string loi = Request.Query["vnp_ResponseCode"] == "24" ? "Bạn đã hủy thanh toán." : null;
            if (dh != null && User.MaKH() != dh.MaKH) return RedirectToAction("Orders", "Account");
            return KetQua(dh, dh?.TrangThaiThanhToan == TrangThaiThanhToanDH.Da, loi);
        }

        [HttpGet]
        public IActionResult VnPayIpn()
        {
            if (!CongThanhToan.VnPayDaCauHinh || !CongThanhToan.VnPayHopLe(Request.Query))
                return Json(new { RspCode = "97", Message = "Invalid signature" });
            int? maDH = CongThanhToan.MaDHTuMaGiaoDich(Request.Query["vnp_TxnRef"]);
            if (!maDH.HasValue) return Json(new { RspCode = "01", Message = "Order not found" });
            bool ok = Request.Query["vnp_ResponseCode"] == "00" && Request.Query["vnp_TransactionStatus"] == "00";
            decimal.TryParse(Request.Query["vnp_Amount"], out decimal amount);
            DonHangDTO dh = _thanhToanBLL.GhiNhanKetQua(maDH.Value, ok, amount / 100, Request.Query["vnp_TransactionNo"]);
            if (dh == null) return Json(new { RspCode = "01", Message = "Order not found" });
            if (ok && amount / 100 != dh.TongTien) return Json(new { RspCode = "04", Message = "Invalid amount" });
            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        // ------------------------------------------------------------------ MoMo
        [HttpGet]
        public IActionResult MoMoReturn()
        {
            if (!CongThanhToan.MoMoDaCauHinh || !CongThanhToan.MoMoHopLe(k => Request.Query[k].ToString()))
            {
                TempData["Warn"] = "Dữ liệu trả về từ MoMo không hợp lệ.";
                return RedirectToAction("Orders", "Account");
            }
            int? maDH = CongThanhToan.MaDHTuMaGiaoDich(Request.Query["orderId"]);
            if (!maDH.HasValue) return RedirectToAction("Orders", "Account");
            bool ok = Request.Query["resultCode"] == "0";
            decimal.TryParse(Request.Query["amount"], out decimal amount);
            DonHangDTO dh = _thanhToanBLL.GhiNhanKetQua(maDH.Value, ok, amount, Request.Query["transId"]);
            if (dh != null && User.MaKH() != dh.MaKH) return RedirectToAction("Orders", "Account");
            return KetQua(dh, dh?.TrangThaiThanhToan == TrangThaiThanhToanDH.Da, ok ? null : Request.Query["message"].ToString());
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MoMoIpn()
        {
            Dictionary<string, string> d;
            try
            {
                using JsonDocument doc = await JsonDocument.ParseAsync(Request.Body);
                d = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());
            }
            catch { return BadRequest(); }
            string Lay(string k) => d.TryGetValue(k, out string v) ? v : "";
            if (!CongThanhToan.MoMoDaCauHinh || !CongThanhToan.MoMoHopLe(Lay)) return BadRequest();
            int? maDH = CongThanhToan.MaDHTuMaGiaoDich(Lay("orderId"));
            if (!maDH.HasValue) return BadRequest();
            decimal.TryParse(Lay("amount"), out decimal amount);
            DonHangDTO dh = _thanhToanBLL.GhiNhanKetQua(maDH.Value, Lay("resultCode") == "0", amount, Lay("transId"));
            if (dh == null || (Lay("resultCode") == "0" && amount != dh.TongTien)) return BadRequest();
            return NoContent();   // MoMo yêu cầu HTTP 204
        }
    }
}
