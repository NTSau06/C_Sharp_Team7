using Microsoft.Data.SqlClient;
using SportShopWeb.DAL;
using SportShopWeb.DTO;

namespace SportShopWeb.BLL
{
    /// <summary>UC 1.2.14 - Áp dụng mã giảm giá (quy tắc giống MaGiamGiaBLL.KiemTraMa của Admin)</summary>
    public class MaGiamGiaBLL
    {
        private readonly MaGiamGiaDAL dal = new MaGiamGiaDAL();

        public static string ChuanHoaCode(string code) => (code ?? "").Trim().ToUpperInvariant();

        public ApDungMaResult KiemTraMa(string code, decimal tamTinh)
        {
            return KiemTra(dal.GetByCode(ChuanHoaCode(code)), tamTinh);
        }

        /// <summary>Kiểm tra lại trong giao dịch đặt hàng (khóa dòng mã giảm giá)</summary>
        public ApDungMaResult KiemTraMa(SqlTransaction t, string code, decimal tamTinh)
        {
            return KiemTra(dal.GetByCode(t, ChuanHoaCode(code)), tamTinh);
        }

        private static ApDungMaResult KiemTra(MaGiamGiaDTO m, decimal tamTinh)
        {
            var kq = new ApDungMaResult();
            if (m == null || !m.TrangThai) { kq.ThongBao = "Mã không tồn tại"; return kq; }
            DateTime now = DateTime.Now;
            if (now < m.NgayBatDau || now > m.NgayKetThuc || m.SoLuotDaDung >= m.SoLuotToiDa)
            {
                kq.ThongBao = "Mã đã hết hạn hoặc hết lượt sử dụng";
                return kq;
            }
            if (tamTinh < m.DonToiThieu)
            {
                kq.ThongBao = "Đơn hàng tối thiểu " + Utils.WebHelper.Tien(m.DonToiThieu) + " để dùng mã này";
                return kq;
            }
            kq.HopLe = true;
            kq.Ma = m;
            kq.TienGiam = Math.Min(Math.Round(tamTinh * m.PhanTramGiam / 100m, 0), m.SoTienGiamToiDa);
            kq.ThongBao = "Áp dụng mã " + m.Code + " thành công: giảm " + Utils.WebHelper.Tien(kq.TienGiam);
            return kq;
        }

        public int TangLuotDung(SqlTransaction t, int maMGG) => dal.TangLuotDung(t, maMGG);

        public List<MaGiamGiaDTO> DangHieuLuc() => dal.GetDangHieuLuc();
    }
}
