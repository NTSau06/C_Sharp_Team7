using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportShopAdmin.DAL;
using SportShopAdmin.DTO;
using SportShopAdmin.Utils;

namespace SportShopAdmin.BLL
{
    /// <summary>Đăng nhập / đổi mật khẩu (Desktop và Web dùng chung)</summary>
    public class TaiKhoanBLL
    {
        private readonly QuanTriVienDAL qtvDal = new QuanTriVienDAL();

        public QuanTriVienDTO DangNhapQuanTri(string tenDangNhap, string matKhau)
        {
            tenDangNhap = (tenDangNhap ?? "").Trim();
            if (tenDangNhap == "" || string.IsNullOrEmpty(matKhau))
                throw new BusinessException("Vui lòng nhập tên đăng nhập và mật khẩu.");
            QuanTriVienDTO qtv = qtvDal.GetByTenDangNhap(tenDangNhap);
            if (qtv == null || !PasswordHasher.Verify(matKhau, qtv.MatKhau))
                throw new BusinessException("Sai tên đăng nhập hoặc mật khẩu.");
            return qtv;
        }

        public void DoiMatKhau(QuanTriVienDTO qtv, string matKhauCu, string matKhauMoi, string nhapLai)
        {
            QuanTriVienDTO current = qtvDal.GetByTenDangNhap(qtv.TenDangNhap);
            if (current == null || !PasswordHasher.Verify(matKhauCu ?? "", current.MatKhau))
                throw new BusinessException("Mật khẩu hiện tại không đúng.");
            if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6)
                throw new BusinessException("Mật khẩu mới phải có ít nhất 6 ký tự.");
            if (matKhauMoi != nhapLai)
                throw new BusinessException("Nhập lại mật khẩu mới không khớp.");
            qtvDal.DoiMatKhau(current.MaQTV, PasswordHasher.Hash(matKhauMoi));
        }
    }
}
