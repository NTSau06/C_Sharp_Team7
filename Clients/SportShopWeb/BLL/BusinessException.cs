namespace SportShopWeb.BLL
{
    /// <summary>Lỗi nghiệp vụ: thông báo hiển thị trực tiếp cho khách hàng</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    /// <summary>Giỏ hàng có sản phẩm hết hàng / vượt tồn kho khi kiểm tra lần cuối (UC 1.2.10 - A2)</summary>
    public class GioHangThayDoiException : BusinessException
    {
        public GioHangThayDoiException(string message) : base(message) { }
    }
}
