using System.ComponentModel.DataAnnotations;
using SportZoneWeb.DTO;

namespace SportZoneWeb.Models
{
    // ====================================================================== TÀI KHOẢN

    /// <summary>Trang hồ sơ cá nhân</summary>
    public class HoSoVM
    {
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không đúng định dạng (ví dụ: ten@gmail.com)")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ tối đa 200 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }
    }

    /// <summary>Đổi mật khẩu (cần mật khẩu cũ)</summary>
    public class DoiMatKhauVM
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
        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string XacNhanMatKhau { get; set; }
    }

    /// <summary>Quên mật khẩu - bước 1: nhập tên đăng nhập hoặc email để nhận mã OTP</summary>
    public class QuenMatKhauVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        [Display(Name = "Tên đăng nhập / Email")]
        public string TenDangNhapHoacEmail { get; set; }
    }

    /// <summary>Quên mật khẩu - bước 2: nhập mã OTP và mật khẩu mới</summary>
    public class DatLaiMatKhauVM
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số")]
        [Display(Name = "Mã OTP")]
        public string MaOTP { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string XacNhanMatKhau { get; set; }

        public string EmailAn { get; set; }
    }

    /// <summary>Mã OTP đang chờ xác nhận (lưu trong Session)</summary>
    public class OtpInfo
    {
        public int MaKH { get; set; }
        public string Ma { get; set; }
        public DateTime HetHan { get; set; }
        public string EmailAn { get; set; }
        public int SoLanSai { get; set; }
    }

    /// <summary>Một địa chỉ giao hàng trong sổ địa chỉ</summary>
    public class DiaChiVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ tên người nhận")]
        public string HoTenNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại gồm 10-11 chữ số, bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Địa chỉ từ 5 đến 200 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Display(Name = "Đặt làm địa chỉ mặc định")]
        public bool MacDinh { get; set; }
    }

    public class SoDiaChiVM
    {
        public List<DiaChiVM> DanhSach { get; set; } = new List<DiaChiVM>();
        /// <summary>Form thêm mới hoặc sửa (Id = 0 là thêm mới)</summary>
        public DiaChiVM Form { get; set; } = new DiaChiVM();
    }

    // ====================================================================== ĐƠN HÀNG

    /// <summary>Một mốc trên thanh theo dõi trạng thái đơn</summary>
    public class MocTrangThai
    {
        public string Ten { get; set; }
        public string Icon { get; set; }
        public string ThoiGian { get; set; }
        /// <summary>da-qua | hien-tai | loi | cho</summary>
        public string Kieu { get; set; }
    }

    public class ChiTietDonVM
    {
        public DonHangDTO DonHang { get; set; }
        public List<MocTrangThai> Moc { get; set; } = new List<MocTrangThai>();
        public bool CoTheHuy { get; set; }

        public static readonly string[] LyDoHuy =
        {
            "Muốn thay đổi địa chỉ giao hàng",
            "Muốn thay đổi sản phẩm / số lượng",
            "Tìm được giá tốt hơn",
            "Không còn nhu cầu mua",
            "Lý do khác"
        };

        /// <summary>
        /// Dựng thanh trạng thái: Chờ xác nhận → Đã xác nhận → Đang giao → Đã giao hàng (hoặc nhánh Hủy / Giao thất bại).
        /// Mốc thời gian lấy từ bảng LichSuDonHang (trigger ghi mỗi lần đơn đổi trạng thái).
        /// </summary>
        public static List<MocTrangThai> TaoMoc(DonHangDTO dh, List<LichSuDonHangDTO> lichSu)
        {
            lichSu = lichSu ?? new List<LichSuDonHangDTO>();
            string Luc(string tt) => lichSu.LastOrDefault(l => l.TrangThai == tt)?.ThoiGian.ToString("HH:mm dd/MM/yyyy");

            var moc = new List<MocTrangThai>
            {
                new MocTrangThai
                {
                    Ten = TrangThaiDonHang.ChoXacNhan, Icon = "📝",
                    ThoiGian = "Đặt lúc " + (Luc(TrangThaiDonHang.ChoXacNhan) ?? dh.NgayDat.ToString("HH:mm dd/MM/yyyy")),
                    Kieu = dh.TrangThai == TrangThaiDonHang.ChoXacNhan ? "hien-tai" : "da-qua"
                }
            };
            if (dh.TrangThai == TrangThaiDonHang.DaHuy)
            {
                string luc = Luc(TrangThaiDonHang.DaHuy);
                moc.Add(new MocTrangThai { Ten = TrangThaiDonHang.DaHuy, Icon = "✖", Kieu = "loi", ThoiGian = luc != null ? "Hủy lúc " + luc : "Đơn đã bị hủy" });
                return moc;
            }

            string[] buoc = { TrangThaiDonHang.DaXacNhan, TrangThaiDonHang.DangGiao, TrangThaiDonHang.DaGiao };
            string[] icon = { "✔", "🚚", "📦" };
            // Số thứ tự bước hiện tại: 0 = Chờ xác nhận, 1 = Đã xác nhận, 2 = Đang giao, 3 = Đã giao / Giao thất bại
            int hienTai = dh.TrangThai == TrangThaiDonHang.GiaoThatBai ? 3 : Array.IndexOf(buoc, dh.TrangThai) + 1;
            for (int k = 0; k < buoc.Length; k++)
            {
                int so = k + 1;
                if (dh.TrangThai == TrangThaiDonHang.GiaoThatBai && so == 3)
                {
                    moc.Add(new MocTrangThai { Ten = TrangThaiDonHang.GiaoThatBai, Icon = "⚠", Kieu = "loi", ThoiGian = Luc(TrangThaiDonHang.GiaoThatBai) ?? "Giao không thành công" });
                    break;
                }
                var m = new MocTrangThai { Ten = buoc[k], Icon = icon[k] };
                string lucBuoc = Luc(buoc[k]);
                if (so < hienTai || (so == hienTai && so == 3)) { m.Kieu = "da-qua"; m.ThoiGian = lucBuoc ?? "Đã hoàn thành"; }
                else if (so == hienTai) { m.Kieu = "hien-tai"; m.ThoiGian = lucBuoc ?? "Trạng thái hiện tại"; }
                else
                {
                    m.Kieu = "cho";
                    m.ThoiGian = so == 3 ? "Dự kiến " + dh.NgayDat.AddDays(3).ToString("dd/MM/yyyy") : "Đang chờ";
                }
                moc.Add(m);
            }
            return moc;
        }
    }
}
