using System.ComponentModel.DataAnnotations;
using SportZoneWeb.DTO;

namespace SportZoneWeb.Models
{
    /// <summary>Kết quả thao tác nghiệp vụ đơn giản</summary>
    public class KetQua
    {
        public bool ThanhCong { get; set; }
        public string ThongBao { get; set; }

        public static KetQua Ok(string msg) => new KetQua { ThanhCong = true, ThongBao = msg };
        public static KetQua Loi(string msg) => new KetQua { ThanhCong = false, ThongBao = msg };
    }

    /// <summary>Trang danh sách sản phẩm: theo danh mục hoặc kết quả tìm kiếm</summary>
    public class ProductListVM
    {
        public string TieuDe { get; set; }
        public string MoTa { get; set; }
        public SanPhamFilter BoLoc { get; set; } = new SanPhamFilter();
        public List<SanPhamDTO> SanPhams { get; set; } = new List<SanPhamDTO>();
        public string ThongBao { get; set; }

        /// <summary>Từ khóa (trang tìm kiếm)</summary>
        public string TuKhoa { get; set; }
        /// <summary>Danh mục đang xem (trang danh mục)</summary>
        public DanhMucDTO DanhMuc { get; set; }
        /// <summary>Danh sách danh mục cho bộ lọc "loại sản phẩm" / gợi ý</summary>
        public List<DanhMucDTO> DanhMucs { get; set; } = new List<DanhMucDTO>();
        public bool LaTrangTimKiem => DanhMuc == null;

        // ----- phân trang (W2)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = SanPhamFilter.PageSize;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        public int TuSo => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int DenSo => Math.Min(TotalItems, Page * PageSize);
    }

    /// <summary>Kết quả truy vấn có phân trang</summary>
    public class KetQuaTrang<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    }

    public class HomeVM
    {
        public List<DanhMucDTO> DanhMucs { get; set; } = new List<DanhMucDTO>();
        public List<SanPhamDTO> MoiNhat { get; set; } = new List<SanPhamDTO>();
        public List<SanPhamDTO> BanChay { get; set; } = new List<SanPhamDTO>();
        public List<MaGiamGiaDTO> KhuyenMai { get; set; } = new List<MaGiamGiaDTO>();
        public string ThongBao { get; set; }
    }

    public class ProductDetailVM
    {
        public SanPhamDTO SanPham { get; set; }
        public List<SanPhamDTO> LienQuan { get; set; } = new List<SanPhamDTO>();
        public int TrongGio { get; set; }
    }

    /// <summary>Một dòng trong giỏ hàng</summary>
    public class GioHangItemVM
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public int MaDM { get; set; }
        public string TenDM { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongTon { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
        public bool VuotTonKho => SoLuong > SoLuongTon;
        /// <summary>Số lượng tối đa được chọn trong giỏ (tồn kho, giới hạn 99/sản phẩm)</summary>
        public int ToiDa => Math.Min(SoLuongTon, SportZoneWeb.BLL.GioHangBLL.SoLuongToiDaMoiSanPham);
    }

    public class CartVM
    {
        public List<GioHangItemVM> Items { get; set; } = new List<GioHangItemVM>();
        public decimal TamTinh { get; set; }
        public string MaGiamGia { get; set; }
        public decimal TienGiam { get; set; }
        public string ThongBaoMa { get; set; }
        public decimal TongCong => Math.Max(0, TamTinh - TienGiam);
        public List<MaGiamGiaDTO> MaGoiY { get; set; } = new List<MaGiamGiaDTO>();
    }

    /// <summary>Một địa chỉ + số điện thoại giao hàng (từ tài khoản hoặc đơn hàng trước đó)</summary>
    public class DiaChiGiaoItem
    {
        public string HoTen { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }
        public bool CoDiaChi => !string.IsNullOrWhiteSpace(DiaChi);
    }

    /// <summary>Form đặt hàng + thanh toán</summary>
    public class CheckoutVM
    {
        /// <summary>"account" = thông tin tài khoản, "saved-0/1/..." = địa chỉ từng dùng, "other" = nhập địa chỉ & SĐT khác</summary>
        public string ChonDiaChi { get; set; } = "account";

        /// <summary>Tùy chọn lưu địa chỉ vừa nhập làm địa chỉ mặc định trong tài khoản</summary>
        public bool LuuDiaChiMacDinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ tên người nhận")]
        public string HoTenNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Địa chỉ từ 5 đến 200 ký tự")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string DiaChiGiao { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public int? MaPTTT { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string MaGiamGia { get; set; }

        /// <summary>"Mua ngay": đặt riêng 1 sản phẩm, không dùng và không xóa giỏ hàng (null = đặt cả giỏ)</summary>
        public int? MuaNgayMaSP { get; set; }
        public int MuaNgaySoLuong { get; set; } = 1;
        public bool LaMuaNgay => MuaNgayMaSP.HasValue;

        // ----- dữ liệu hiển thị
        public DiaChiGiaoItem TaiKhoanMacDinh { get; set; } = new DiaChiGiaoItem();
        public List<DiaChiGiaoItem> DiaChiDaDung { get; set; } = new List<DiaChiGiaoItem>();
        public List<GioHangItemVM> Items { get; set; } = new List<GioHangItemVM>();
        public List<PhuongThucThanhToanDTO> PhuongThucs { get; set; } = new List<PhuongThucThanhToanDTO>();
        public decimal TamTinh { get; set; }
        public decimal TienGiam { get; set; }
        public string ThongBaoMa { get; set; }
        public decimal TongTien => Math.Max(0, TamTinh - TienGiam);
    }

    public class LoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        [Display(Name = "Tên đăng nhập / Email")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegisterVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [RegularExpression(@"^[a-zA-Z0-9_.]{4,50}$", ErrorMessage = "Tên đăng nhập 4-50 ký tự, chỉ gồm chữ không dấu, số, '_' hoặc '.'")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không đúng định dạng (ví dụ: ten@gmail.com)")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        public string XacNhanMatKhau { get; set; }
    }

    public class ProfileVM
    {
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không đúng định dạng (ví dụ: ten@gmail.com)")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        [Display(Name = "Địa chỉ mặc định")]
        public string DiaChi { get; set; }
    }

    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string XacNhanMatKhau { get; set; }
    }

    public class AccountManageVM
    {
        public ProfileVM ThongTin { get; set; } = new ProfileVM();
        public ChangePasswordVM MatKhau { get; set; } = new ChangePasswordVM();
        public int TongDonHang { get; set; }
        public List<DiaChiGiaoItem> DiaChiDaDung { get; set; } = new List<DiaChiGiaoItem>();
    }

    /// <summary>FR18 - Lịch sử đơn hàng, lọc nhanh theo trạng thái</summary>
    public class OrdersVM
    {
        public List<DonHangDTO> DonHangs { get; set; } = new List<DonHangDTO>();
        /// <summary>Trạng thái đang lọc (null/rỗng = Tất cả)</summary>
        public string TrangThai { get; set; }
        public int TongSo { get; set; }
        public Dictionary<string, int> SoLuongTheoTrangThai { get; set; } = new Dictionary<string, int>();
        public int Dem(string tt) => SoLuongTheoTrangThai.TryGetValue(tt, out int n) ? n : 0;
    }

    public class ErrorVM
    {
        public string TieuDe { get; set; }
        public string ThongBao { get; set; }
    }
}
