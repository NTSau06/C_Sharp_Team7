namespace SportZoneWeb.DAL
{
    /// <summary>Trạng thái chuẩn bị CSDL lúc khởi động (chạy nền để website mở ngay)</summary>
    public static class DbStatus
    {
        public static volatile bool Ready;
        public static volatile string Message = "Đang kết nối SQL Server...";
        public static volatile string Error;
    }
}
