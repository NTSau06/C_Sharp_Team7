using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportZoneAdmin.DAL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.BLL
{
    public class KhoBLL
    {
        private readonly KhoDAL dal = new KhoDAL();

        public List<SanPhamDTO> GetTonKho(string keyword, bool chiSapHet)
        {
            return dal.GetTonKho(keyword, chiSapHet);
        }

        /// <summary>Lập phiếu nhập: ghi phiếu + chi tiết và cộng dồn tồn kho trong một transaction. Trả về MaPN.</summary>
        public int LapPhieuNhap(PhieuNhapKhoDTO pn)
        {
            List<ChiTietPhieuNhapDTO> items = pn.GetChiTiet();
            if (items.Count == 0) throw new BusinessException("Phiếu nhập phải có ít nhất một sản phẩm.");
            foreach (ChiTietPhieuNhapDTO ct in items)
            {
                if (ct.SoLuongNhap <= 0 || ct.GiaNhap <= 0)
                    throw new BusinessException("Số lượng và đơn giá nhập của \"" + ct.TenSP + "\" phải lớn hơn 0.");
            }
            if (items.GroupBy(x => x.MaSP).Any(g => g.Count() > 1))
                throw new BusinessException("Một sản phẩm chỉ được xuất hiện một lần trong phiếu nhập.");

            pn.GhiChu = (pn.GhiChu ?? "").Trim();
            if (pn.GhiChu.Length > 255) throw new BusinessException("Ghi chú phiếu nhập tối đa 255 ký tự.");
            pn.MaQTV = Session.MaQTV;
            pn.TongTienNhap = items.Sum(x => x.ThanhTien);

            return DBConnect.RunInTransaction(t =>
            {
                int maPN = dal.InsertPhieuNhap(t, pn);
                foreach (ChiTietPhieuNhapDTO ct in items)
                {
                    dal.InsertChiTietPhieuNhap(t, maPN, ct);
                    dal.CongTonKho(t, ct.MaSP, ct.SoLuongNhap);
                }
                return maPN;
            });
        }

        public List<PhieuNhapKhoDTO> GetPhieuNhap(DateTime tuNgay, DateTime denNgay)
        {
            KiemTraKhoangNgay(tuNgay, denNgay);
            return dal.GetPhieuNhap(tuNgay, denNgay);
        }

        public List<ChiTietPhieuNhapDTO> GetChiTietPhieuNhap(int maPN)
        {
            return dal.GetChiTietPhieuNhap(maPN);
        }

        public List<LichSuKhoDTO> GetLichSu(DateTime tuNgay, DateTime denNgay, string keyword)
        {
            KiemTraKhoangNgay(tuNgay, denNgay);
            return dal.GetLichSu(tuNgay, denNgay, keyword);
        }

        internal static void KiemTraKhoangNgay(DateTime tuNgay, DateTime denNgay)
        {
            if (denNgay.Date < tuNgay.Date) throw new BusinessException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
        }
    }
}
