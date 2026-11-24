using System.Text.Json;
using SportZoneWeb.BLL;

namespace SportZoneWeb.Utils
{
    /// <summary>
    /// Dữ liệu tạm theo phiên (Session): sản phẩm đã xem gần đây và mã OTP quên mật khẩu.
    /// Các dữ liệu cần lưu lâu dài (yêu thích, sổ địa chỉ, đánh giá...) nằm trong CSDL.
    /// </summary>
    public static class SessionStore
    {
        public const string KeyDaXem = "DaXem";
        public const string KeyOtp = "OtpQuenMatKhau";
        private const string KeyYeuThichCache = "__YeuThich";

        public static T GetJson<T>(this ISession s, string key) where T : class
        {
            string json = s.GetString(key);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<T>(json); } catch { return null; }
        }

        public static void SetJson<T>(this ISession s, string key, T value) => s.SetString(key, JsonSerializer.Serialize(value));

        // ------------------------------------------------------------------ đã xem gần đây (tối đa 8, lưu trong Session)
        public const int SoDaXemToiDa = 8;

        public static List<int> DaXem(this ISession s) => s.GetJson<List<int>>(KeyDaXem) ?? new List<int>();

        public static void GhiDaXem(this ISession s, int maSP)
        {
            List<int> ds = s.DaXem();
            ds.Remove(maSP);
            ds.Insert(0, maSP);
            if (ds.Count > SoDaXemToiDa) ds = ds.Take(SoDaXemToiDa).ToList();
            s.SetJson(KeyDaXem, ds);
        }

        // ------------------------------------------------------------------ yêu thích (đọc CSDL 1 lần mỗi request)
        /// <summary>Mã sản phẩm yêu thích của khách đang đăng nhập (chưa đăng nhập = rỗng)</summary>
        public static HashSet<int> YeuThich(this HttpContext ctx)
        {
            if (ctx.Items[KeyYeuThichCache] is HashSet<int> daCo) return daCo;
            var ds = new HashSet<int>();
            int? maKH = ctx.User.MaKH();
            if (maKH.HasValue)
            {
                try { ds = new HashSet<int>(new YeuThichBLL().LayMaSP(maKH.Value)); } catch { /* CSDL chưa sẵn sàng */ }
            }
            ctx.Items[KeyYeuThichCache] = ds;
            return ds;
        }
    }
}
