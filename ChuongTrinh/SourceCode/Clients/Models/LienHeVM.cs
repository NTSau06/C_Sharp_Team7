using System.ComponentModel.DataAnnotations;

namespace SportZoneWeb.Models
{
    /// <summary>Form trang Liên hệ</summary>
    public class LienHeVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng (ví dụ: ten@gmail.com)")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(150, ErrorMessage = "Tiêu đề tối đa 150 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Nội dung từ 10 đến 2000 ký tự")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }
    }
}
