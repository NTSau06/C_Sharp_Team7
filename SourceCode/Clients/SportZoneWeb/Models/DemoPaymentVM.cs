namespace SportZoneWeb.Models
{
    public class DemoPaymentVM
    {
        public int MaDH { get; set; }
        public decimal TongTien { get; set; }
        public string TenCong { get; set; }
        public string QrDataUri { get; set; }
        public string ScanUrl { get; set; }
        public string StatusUrl { get; set; }
        public string SuccessUrl { get; set; }
    }

    public class DemoPaymentScanVM
    {
        public int MaDH { get; set; }
        public bool ThanhToanThanhCong { get; set; }
    }
}
