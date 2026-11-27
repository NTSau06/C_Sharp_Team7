using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Utils;

namespace SportZoneWeb.BLL
{
    /// <summary>UC 1.2.11 - Thanh toán: chọn phương thức + ghi nhận kết quả thanh toán online (VNPay / MoMo)</summary>
    public class ThanhToanBLL
    {
        private readonly PhuongThucThanhToanDAL ptttDAL = new PhuongThucThanhToanDAL();
        private readonly DonHangDAL donHangDAL = new DonHangDAL();

        public List<PhuongThucThanhToanDTO> LayPhuongThuc() => ptttDAL.GetAll();

        public bool KiemTraPhuongThuc(int? maPTTT) => maPTTT.HasValue && ptttDAL.Exists(maPTTT.Value);

        /// <summary>
        /// Ghi nhận kết quả từ cổng thanh toán. Chỉ tin dữ liệu đã kiểm tra chữ ký (do controller làm) và khớp số tiền đơn.
        /// Idempotent: cổng gọi lặp (return + IPN) không làm hỏng dữ liệu. Trả về null nếu đơn không tồn tại.
        /// </summary>
        public DonHangDTO GhiNhanKetQua(int maDH, bool thanhCong, decimal soTienCong, string maGiaoDich)
        {
            DonHangDTO dh = donHangDAL.GetById(maDH);
            if (dh == null) return null;
            if (thanhCong && soTienCong == dh.TongTien) donHangDAL.DanhDauDaThanhToan(maDH, maGiaoDich);
            else if (!thanhCong) donHangDAL.DanhDauThanhToanThatBai(maDH);
            return donHangDAL.GetById(maDH);
        }

        public bool BatDauThanhToanDemo(int maDH, string maXacThuc) =>
            donHangDAL.BatDauThanhToanDemo(maDH, maXacThuc);

        public DonHangDTO LayDonHangDemo(int maDH, string maXacThuc)
        {
            DonHangDTO dh = donHangDAL.GetById(maDH);
            return dh != null && dh.MaXacThucDemo == maXacThuc &&
                   dh.TrangThai != TrangThaiDonHang.DaHuy &&
                   CongThanhToan.NhanDang(dh.TenPTTT) != null ? dh : null;
        }

        public DonHangDTO GhiNhanThanhToanDemo(int maDH, string maXacThuc)
        {
            DonHangDTO dh = LayDonHangDemo(maDH, maXacThuc);
            if (dh == null || dh.TrangThaiThanhToan == TrangThaiThanhToanDH.Da) return dh;

            string maGiaoDich = "DEMO-" + maDH + "-" + Guid.NewGuid().ToString("N");
            donHangDAL.DanhDauThanhToanDemo(maDH, maXacThuc, maGiaoDich);
            return LayDonHangDemo(maDH, maXacThuc);
        }
    }
}
