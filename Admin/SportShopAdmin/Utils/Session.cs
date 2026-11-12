using SportShopAdmin.DTO;

namespace SportShopAdmin.Utils
{
    /// <summary>Lưu quản trị viên đang đăng nhập</summary>
    public static class Session
    {
        public static QuanTriVienDTO CurrentAdmin { get; set; }

        public static int MaQTV
        {
            get { return CurrentAdmin == null ? 0 : CurrentAdmin.MaQTV; }
        }
    }
}
