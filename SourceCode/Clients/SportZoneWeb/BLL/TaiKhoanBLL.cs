using System.Text.RegularExpressions;
using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Utils;

namespace SportZoneWeb.BLL
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
            // TC05: tài khoản đã bị Quản trị viên xóa mềm (TrangThai = 0) không được đăng nhập
            if (!kh.TrangThai)
                throw new BusinessException("Tài khoản đã bị vô hiệu hóa (đã xóa). Vui lòng liên hệ cửa hàng để được hỗ trợ.");
            return kh;
        }

        public KhachHangDTO GetById(int maKH) => taiKhoanDAL.GetKHById(maKH);

        public bool TaiKhoanConHoatDong(int maKH) => taiKhoanDAL.IsActive(maKH);

        /// <summary>Quên mật khẩu: tìm tài khoản theo tên đăng nhập hoặc email (chỉ đọc CSDL)</summary>
        public KhachHangDTO TimTaiKhoan(string tenDangNhapHoacEmail)
        {
            string s = (tenDangNhapHoacEmail ?? "").Trim();
            if (s == "") throw new BusinessException("Vui lòng nhập tên đăng nhập hoặc email.");
            KhachHangDTO kh = taiKhoanDAL.GetKHByUsername(s);
            if (kh == null || !kh.TrangThai) throw new BusinessException("Không tìm thấy tài khoản với thông tin này.");
            return kh;
        }

        /// <summary>Kiểm tra email / SĐT mới không trùng với khách hàng khác (bỏ qua nếu không đổi)</summary>
        public void KiemTraHoSo(KhachHangDTO hienTai, string email, string sdt)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            sdt = (sdt ?? "").Trim();
            if (!string.Equals(email, hienTai.Email, StringComparison.OrdinalIgnoreCase) && taiKhoanDAL.ExistsEmail(email))
                throw new BusinessException("Email \"" + email + "\" đã được sử dụng.", "Email");
            if (sdt != hienTai.SDT && taiKhoanDAL.ExistsSDT(sdt))
                throw new BusinessException("Số điện thoại \"" + sdt + "\" đã được sử dụng.", "SDT");
        }

        /// <summary>Cập nhật hồ sơ (họ tên, SĐT, email, địa chỉ) vào bảng KhachHang</summary>
        public void CapNhatHoSo(KhachHangDTO hienTai, string hoTen, string sdt, string email, string diaChi)
        {
            hoTen = (hoTen ?? "").Trim();
            email = (email ?? "").Trim().ToLowerInvariant();
            sdt = (sdt ?? "").Trim();
            diaChi = string.IsNullOrWhiteSpace(diaChi) ? null : diaChi.Trim();
            if (hoTen.Length < 2) throw new BusinessException("Họ tên không hợp lệ.", "HoTen");
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) throw new BusinessException("Email không đúng định dạng (ví dụ: ten@gmail.com)", "Email");
            if (!Regex.IsMatch(sdt, @"^0\d{9,10}$")) throw new BusinessException("Số điện thoại không hợp lệ.", "SDT");
            KiemTraHoSo(hienTai, email, sdt);
            taiKhoanDAL.UpdateHoSo(hienTai.MaKH, hoTen, sdt, email, diaChi);
        }

        /// <summary>Đổi mật khẩu (cần đúng mật khẩu hiện tại), lưu dạng SALT$SHA256</summary>
        public void DoiMatKhau(int maKH, string matKhauCu, string matKhauMoi)
        {
            KiemTraDoiMatKhau(taiKhoanDAL.GetKHById(maKH), matKhauCu, matKhauMoi);
            taiKhoanDAL.UpdateMatKhau(maKH, PasswordHasher.Hash(matKhauMoi));
        }

        /// <summary>Đặt lại mật khẩu sau khi xác nhận OTP</summary>
        public void DatLaiMatKhau(int maKH, string matKhauMoi)
        {
            if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6)
                throw new BusinessException("Mật khẩu mới tối thiểu 6 ký tự.", "MatKhauMoi");
            if (!taiKhoanDAL.UpdateMatKhau(maKH, PasswordHasher.Hash(matKhauMoi)))
                throw new BusinessException("Không tìm thấy tài khoản.");
        }

        /// <summary>Đổi mật khẩu: kiểm tra mật khẩu hiện tại và mật khẩu mới</summary>
        public void KiemTraDoiMatKhau(KhachHangDTO kh, string matKhauCu, string matKhauMoi)
        {
            if (kh == null || !PasswordHasher.Verify(matKhauCu ?? "", kh.MatKhau))
                throw new BusinessException("Mật khẩu hiện tại không đúng.", "MatKhauCu");
            if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6)
                throw new BusinessException("Mật khẩu mới tối thiểu 6 ký tự.", "MatKhauMoi");
            if (matKhauMoi == matKhauCu)
                throw new BusinessException("Mật khẩu mới phải khác mật khẩu hiện tại.", "MatKhauMoi");
        }

        /// <summary>Đăng ký tài khoản mới, trả về MaKH</summary>
        public int DangKy(KhachHangDTO kh, string matKhau)
        {
            KiemTraHopLe(kh, matKhau);
            kh.MatKhau = PasswordHasher.Hash(matKhau);   // TC07: lưu dạng SALT$SHA256
            return taiKhoanDAL.InsertKhachHang(kh);
        }

        /// <summary>Kiểm tra dữ liệu đăng ký; lỗi => BusinessException kèm tên ô nhập (TC08, TC09)</summary>
        private void KiemTraHopLe(KhachHangDTO kh, string matKhau)
        {
            kh.HoTen = (kh.HoTen ?? "").Trim();
            kh.TenDangNhap = (kh.TenDangNhap ?? "").Trim();
            kh.Email = (kh.Email ?? "").Trim().ToLowerInvariant();
            kh.SDT = (kh.SDT ?? "").Trim();
            kh.DiaChi = string.IsNullOrWhiteSpace(kh.DiaChi) ? null : kh.DiaChi.Trim();

            if (kh.HoTen.Length < 2) throw new BusinessException("Họ tên không hợp lệ.", "HoTen");
            if (!Regex.IsMatch(kh.TenDangNhap, @"^[a-zA-Z0-9_.]{4,50}$"))
                throw new BusinessException("Tên đăng nhập 4-50 ký tự, chỉ gồm chữ không dấu, số, '_' hoặc '.'.", "TenDangNhap");
            if (!Regex.IsMatch(kh.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new BusinessException("Email không đúng định dạng (ví dụ: ten@gmail.com)", "Email");
            if (!Regex.IsMatch(kh.SDT, @"^0\d{9,10}$")) throw new BusinessException("Số điện thoại không hợp lệ.", "SDT");
            if (string.IsNullOrEmpty(matKhau) || matKhau.Length < 6) throw new BusinessException("Mật khẩu tối thiểu 6 ký tự.", "MatKhau");
            if (taiKhoanDAL.ExistsUsername(kh.TenDangNhap))
                throw new BusinessException("Tên đăng nhập \"" + kh.TenDangNhap + "\" đã tồn tại.", "TenDangNhap");
            if (taiKhoanDAL.ExistsEmail(kh.Email))
                throw new BusinessException("Email \"" + kh.Email + "\" đã được sử dụng.", "Email");
            if (taiKhoanDAL.ExistsSDT(kh.SDT))
                throw new BusinessException("Số điện thoại \"" + kh.SDT + "\" đã được sử dụng.", "SDT");
        }
    }
}
