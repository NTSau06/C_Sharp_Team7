using System;
using System.Collections.Generic;

namespace SportShopAdmin.DTO
{
    public class KhachHangDTO
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        /// <summary>true: hoạt động, false: đã xóa mềm</summary>
        public bool TrangThai { get; set; }

        public string TrangThaiText => TrangThai ? "Hoạt động" : "Đã xóa";
    }
}
