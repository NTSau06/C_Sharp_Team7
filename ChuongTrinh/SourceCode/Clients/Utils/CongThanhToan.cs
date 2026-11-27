using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SportZoneWeb.Utils
{
    /// <summary>Cấu hình + hàm ký/kiểm tra chữ ký cho VNPay và MoMo (môi trường sandbox). Khóa nằm trong appsettings.json.</summary>
    public static class CongThanhToan
    {
        public const string VNPAY = "VNPAY";
        public const string MOMO = "MOMO";

        public static IConfiguration Cfg { get; set; }
        public static bool DemoMode { get; set; }

        /// <summary>Tên phương thức trong bảng PhuongThucThanhToan => cổng online (null = COD / khác)</summary>
        public static string NhanDang(string tenPTTT)
        {
            string t = (tenPTTT ?? "").ToLowerInvariant();
            if (t.Contains("vnpay")) return VNPAY;
            if (t.Contains("momo")) return MOMO;
            return null;
        }

        private static string Hex(byte[] b) => Convert.ToHexString(b).ToLowerInvariant();
        private static string HmacSha512(string key, string data) { using var h = new HMACSHA512(Encoding.UTF8.GetBytes(key)); return Hex(h.ComputeHash(Encoding.UTF8.GetBytes(data))); }
        private static string HmacSha256(string key, string data) { using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key)); return Hex(h.ComputeHash(Encoding.UTF8.GetBytes(data))); }

        private static bool BangNhau(string a, string b) =>
            !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a.ToLowerInvariant()), Encoding.UTF8.GetBytes(b.ToLowerInvariant()));

        /// <summary>"123T20260101120000" => 123 (mã đơn nằm trước chữ T)</summary>
        public static int? MaDHTuMaGiaoDich(string maGD)
        {
            string s = (maGD ?? "").Split('T')[0];
            return int.TryParse(s, out int id) ? id : (int?)null;
        }

        private static string MaGiaoDichMoi(int maDH) => maDH + "T" + DateTime.Now.ToString("yyyyMMddHHmmss");

        // ============================================================ VNPay
        public static bool VnPayDaCauHinh => !string.IsNullOrWhiteSpace(Cfg?["VnPay:TmnCode"]) && !string.IsNullOrWhiteSpace(Cfg?["VnPay:HashSecret"]);

        private static string QueryDaSapXep(IEnumerable<KeyValuePair<string, string>> p) =>
            string.Join("&", p.Where(x => !string.IsNullOrEmpty(x.Value)).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => WebUtility.UrlEncode(x.Key) + "=" + WebUtility.UrlEncode(x.Value)));

        public static string VnPayTaoUrl(int maDH, decimal soTien, string ip, string returnUrl)
        {
            var p = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = Cfg["VnPay:TmnCode"],
                ["vnp_Amount"] = ((long)(soTien * 100)).ToString(),
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ip,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = "Thanh toan don hang DH" + maDH,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = MaGiaoDichMoi(maDH)
            };
            string query = QueryDaSapXep(p);
            string url = Cfg["VnPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            return url + "?" + query + "&vnp_SecureHash=" + HmacSha512(Cfg["VnPay:HashSecret"], query);
        }

        /// <summary>Kiểm tra chữ ký VNPay trả về (return URL và IPN dùng chung)</summary>
        public static bool VnPayHopLe(IQueryCollection q)
        {
            var p = q.Where(x => x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                     .Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString()));
            return BangNhau(HmacSha512(Cfg["VnPay:HashSecret"], QueryDaSapXep(p)), q["vnp_SecureHash"].ToString());
        }

        // ============================================================ MoMo
        public static bool MoMoDaCauHinh =>
            !string.IsNullOrWhiteSpace(Cfg?["MoMo:PartnerCode"]) &&
            !string.IsNullOrWhiteSpace(Cfg?["MoMo:AccessKey"]) &&
            !string.IsNullOrWhiteSpace(Cfg?["MoMo:SecretKey"]);

        /// <summary>Gọi API MoMo tạo giao dịch, trả về payUrl (null + Loi nếu thất bại)</summary>
        public static async Task<(string PayUrl, string Loi)> MoMoTaoUrl(int maDH, decimal soTien, string redirectUrl, string ipnUrl)
        {
            string partner = Cfg["MoMo:PartnerCode"], access = Cfg["MoMo:AccessKey"], secret = Cfg["MoMo:SecretKey"];
            string orderId = MaGiaoDichMoi(maDH);
            string requestId = orderId, amount = ((long)soTien).ToString(), info = "Thanh toan don hang DH" + maDH, extra = "", type = "captureWallet";
            string raw = $"accessKey={access}&amount={amount}&extraData={extra}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={info}" +
                         $"&partnerCode={partner}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={type}";
            var body = new Dictionary<string, object>
            {
                ["partnerCode"] = partner, ["accessKey"] = access, ["requestId"] = requestId, ["amount"] = amount, ["orderId"] = orderId,
                ["orderInfo"] = info, ["redirectUrl"] = redirectUrl, ["ipnUrl"] = ipnUrl, ["extraData"] = extra,
                ["requestType"] = type, ["lang"] = "vi", ["signature"] = HmacSha256(secret, raw)
            };
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                var res = await http.PostAsync(Cfg["MoMo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create",
                    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
                using JsonDocument doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                JsonElement r = doc.RootElement;
                if (r.TryGetProperty("payUrl", out JsonElement u) && !string.IsNullOrEmpty(u.GetString())) return (u.GetString(), null);
                return (null, r.TryGetProperty("message", out JsonElement m) ? m.GetString() : "MoMo không tạo được giao dịch.");
            }
            catch (Exception ex) { return (null, "Không kết nối được MoMo: " + ex.Message); }
        }

        /// <summary>Kiểm tra chữ ký MoMo (redirect = GET query, IPN = POST JSON); lay(tên) trả về giá trị tham số</summary>
        public static bool MoMoHopLe(Func<string, string> lay)
        {
            string raw = $"accessKey={Cfg["MoMo:AccessKey"]}&amount={lay("amount")}&extraData={lay("extraData")}&message={lay("message")}&orderId={lay("orderId")}" +
                         $"&orderInfo={lay("orderInfo")}&orderType={lay("orderType")}&partnerCode={lay("partnerCode")}&payType={lay("payType")}" +
                         $"&requestId={lay("requestId")}&responseTime={lay("responseTime")}&resultCode={lay("resultCode")}&transId={lay("transId")}";
            return BangNhau(HmacSha256(Cfg["MoMo:SecretKey"], raw), lay("signature"));
        }
    }
}
