using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportZoneAdmin.DAL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.BLL
{
    public class KhachHangBLL
    {
        private readonly KhachHangDAL dal = new KhachHangDAL();

        public List<KhachHangDTO> Search(string keyword, bool includeDeleted)
        {
            return dal.Search(keyword, includeDeleted);
        }

        /// <summary>
        /// Thêm (MaKH = 0) hoặc sửa tài khoản khách hàng.
        /// matKhauMoi: bắt buộc khi thêm; khi sửa để trống nghĩa là giữ nguyên.
        /// </summary>
        public int Save(KhachHangDTO kh, string matKhauMoi)
        {
            kh.HoTen = (kh.HoTen ?? "").Trim();
            kh.TenDangNhap = (kh.TenDangNhap ?? "").Trim();
            kh.Email = (kh.Email ?? "").Trim();
            kh.SDT = (kh.SDT ?? "").Trim();
            kh.DiaChi = (kh.DiaChi ?? "").Trim();

            if (kh.HoTen == "") throw new BusinessException("Họ tên không được để trống.", "HoTen");
            if (!Regex.IsMatch(kh.TenDangNhap, @"^[A-Za-z0-9_.]{4,50}$"))
                throw new BusinessException("Tên đăng nhập 4-50 ký tự, chỉ gồm chữ không dấu, số, dấu chấm hoặc gạch dưới.", "TenDangNhap");
            if (!Regex.IsMatch(kh.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") || kh.Email.Length > 100)
                throw new BusinessException("Email không đúng định dạng.", "Email");
            if (kh.SDT == "") throw new BusinessException("Số điện thoại không được để trống.", "SDT");
            if (!Regex.IsMatch(kh.SDT, @"^0\d{9,10}$"))
                throw new BusinessException("Số điện thoại phải bắt đầu bằng 0 và gồm 10-11 chữ số.", "SDT");
            if (kh.MaKH == 0 || !string.IsNullOrEmpty(matKhauMoi))
            {
                if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6)
                    throw new BusinessException("Mật khẩu phải có ít nhất 6 ký tự.", "MatKhau");
            }

            if (dal.Exists("TenDangNhap", kh.TenDangNhap, kh.MaKH)) throw new BusinessException("Tên đăng nhập đã tồn tại.", "TenDangNhap");
            if (dal.Exists("Email", kh.Email, kh.MaKH)) throw new BusinessException("Email đã được sử dụng bởi tài khoản khác.", "Email");
            if (dal.Exists("SDT", kh.SDT, kh.MaKH)) throw new BusinessException("Số điện thoại đã được sử dụng bởi tài khoản khác.", "SDT");

            kh.MatKhau = string.IsNullOrEmpty(matKhauMoi) ? null : PasswordHasher.Hash(matKhauMoi);

            ThongBaoEmail = null;
            if (kh.MaKH == 0)
            {
                int maMoi = dal.Insert(kh);
                // UC 1.2.5.1 bước 5: gửi thông tin đăng nhập; lỗi gửi mail không hủy việc tạo tài khoản
                string loi;
                ThongBaoEmail = EmailService.GuiThongTinTaiKhoan(kh.Email, kh.HoTen, kh.TenDangNhap, matKhauMoi, out loi)
                    ? "Đã gửi thông tin đăng nhập tới " + kh.Email + "."
                    : "Chưa gửi được email thông tin đăng nhập: " + loi;
                return maMoi;
            }
            dal.Update(kh);
            return kh.MaKH;
        }

        /// <summary>Kết quả gửi email sau lần Save (thêm mới) gần nhất; null nếu không gửi.</summary>
        public string ThongBaoEmail { get; private set; }

        /// <summary>Xóa mềm; từ chối nếu khách còn đơn hàng chưa hoàn tất</summary>
        public void Delete(int maKH)
        {
            int dangXuLy = dal.CountDonChuaHoanTat(maKH);
            if (dangXuLy > 0)
                throw new BusinessException("Không thể xóa: khách hàng còn " + dangXuLy + " đơn hàng chưa hoàn tất.");
            dal.SoftDelete(maKH);
        }

        public void Restore(int maKH)
        {
            dal.Restore(maKH);
        }
    }
}
