namespace SportZoneWeb.BLL
{
    /// <summary>
    /// Lỗi nghiệp vụ (mục 3.3.2 / 4.5.5): thông báo hiển thị trực tiếp cho khách hàng.
    /// TenTruong (nếu có) = tên thuộc tính trên form để Controller gắn lỗi ngay dưới ô nhập tương ứng.
    /// </summary>
    public class BusinessException : Exception
    {
        public string TenTruong { get; }

        public BusinessException(string message) : base(message) { }

        public BusinessException(string message, string tenTruong) : base(message)
        {
            TenTruong = tenTruong;
        }
    }

    /// <summary>Giỏ hàng thay đổi khi kiểm tra lần cuối: trống / sản phẩm bị xóa (UC 1.2.10 - A2)</summary>
    public class GioHangThayDoiException : BusinessException
    {
        public GioHangThayDoiException(string message) : base(message) { }
    }

    /// <summary>BR01-BR02: số lượng đặt vượt quá tồn kho hiện tại (kiểm tra lần cuối trong giao dịch đặt hàng)</summary>
    public class OutOfStockException : BusinessException
    {
        public int MaSP { get; }
        public int SoLuongTon { get; }

        public OutOfStockException(int maSP, string tenSP, int soLuongTon)
            : base(soLuongTon <= 0
                ? "Sản phẩm \"" + tenSP + "\" đã hết hàng. Vui lòng xem lại giỏ hàng."
                : "Số lượng vượt quá tồn kho hiện tại: \"" + tenSP + "\" chỉ còn " + soLuongTon + " sản phẩm. Vui lòng xem lại giỏ hàng.")
        {
            MaSP = maSP;
            SoLuongTon = soLuongTon;
        }
    }

    /// <summary>BR06-BR08: mã giảm giá không còn hợp lệ (hết hạn, hết lượt, bị vô hiệu hóa, chưa đạt đơn tối thiểu) khi đặt hàng</summary>
    public class VoucherExpiredException : BusinessException
    {
        public string Code { get; }

        public VoucherExpiredException(string code, string message) : base(message, "MaGiamGia")
        {
            Code = code;
        }
    }
}
