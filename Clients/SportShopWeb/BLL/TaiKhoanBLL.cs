using System.Text.RegularExpressions;
using SportShopWeb.DAL;
using SportShopWeb.DTO;
using SportShopWeb.Utils;

namespace SportShopWeb.BLL
{
    /// <summary>UC 1.2.1.1 Đăng nhập, UC 1.2.1.2 Đăng ký (Khách hàng - Web)</summary>
    public class TaiKhoanBLL
    {
        private readonly TaiKhoanDAL taiKhoanDAL = new TaiKhoanDAL();

        public KhachHangDTO DangNhapKH(string tenDN, string matKhau)
        {
            tenDN = (tenDN ?? "").Trim();
            if (tenDN == "" || string.IsNullOrEmpty(matKhau))
                throw new BusinessException("Vui lòng nhập tên đăng nhập và mật khẩu.");

            KhachHangDTO kh = taiKhoanDAL.GetKHByUsername(tenDN);
            if (kh == null || !PasswordHasher.Verify(matKhau, kh.MatKhau))
                throw new BusinessException("Sai tên đăng nhập hoặc mật khẩu.");
            if (!kh.TrangThai)
                throw new BusinessException("Tài khoản đã bị khóa. Vui lòng liên hệ cửa hàng.");
            return kh;
        }

        public KhachHangDTO GetById(int maKH) => taiKhoanDAL.GetKHById(maKH);

        /// <summary>Đăng ký tài khoản mới, trả về MaKH</summary>
        public int DangKy(KhachHangDTO kh, string matKhau)
        {
            string loi = KiemTraHopLe(kh, matKhau);
            if (loi != null) throw new BusinessException(loi);
            kh.MatKhau = PasswordHasher.Hash(matKhau);
            return taiKhoanDAL.InsertKhachHang(kh);
        }

        /// <summary>Trả về thông báo lỗi, hoặc null nếu hợp lệ</summary>
        private string KiemTraHopLe(KhachHangDTO kh, string matKhau)
        {
            kh.HoTen = (kh.HoTen ?? "").Trim();
            kh.TenDangNhap = (kh.TenDangNhap ?? "").Trim();
            kh.Email = (kh.Email ?? "").Trim().ToLowerInvariant();
            kh.SDT = (kh.SDT ?? "").Trim();
            kh.DiaChi = string.IsNullOrWhiteSpace(kh.DiaChi) ? null : kh.DiaChi.Trim();

            if (kh.HoTen.Length < 2) return "Họ tên không hợp lệ.";
            if (!Regex.IsMatch(kh.TenDangNhap, @"^[a-zA-Z0-9_.]{4,50}$"))
                return "Tên đăng nhập 4-50 ký tự, chỉ gồm chữ không dấu, số, '_' hoặc '.'.";
            if (!Regex.IsMatch(kh.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return "Email không hợp lệ.";
            if (!Regex.IsMatch(kh.SDT, @"^0\d{9,10}$")) return "Số điện thoại không hợp lệ.";
            if (string.IsNullOrEmpty(matKhau) || matKhau.Length < 6) return "Mật khẩu tối thiểu 6 ký tự.";
            if (taiKhoanDAL.ExistsUsername(kh.TenDangNhap)) return "Tên đăng nhập \"" + kh.TenDangNhap + "\" đã tồn tại.";
            if (taiKhoanDAL.ExistsEmail(kh.Email)) return "Email \"" + kh.Email + "\" đã được sử dụng.";
            if (taiKhoanDAL.ExistsSDT(kh.SDT)) return "Số điện thoại \"" + kh.SDT + "\" đã được sử dụng.";
            return null;
        }
    }
}
