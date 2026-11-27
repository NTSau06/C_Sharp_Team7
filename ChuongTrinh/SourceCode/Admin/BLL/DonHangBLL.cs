using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportZoneAdmin.DAL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.BLL
{
    /// <summary>Điều phối DonHangDAL, KhoDAL, MaGiamGiaDAL - mọi thao tác đổi trạng thái chạy trong SqlTransaction</summary>
    public class DonHangBLL
    {
        private readonly DonHangDAL dal = new DonHangDAL();
        private readonly KhoDAL khoDal = new KhoDAL();
        private readonly MaGiamGiaDAL mggDal = new MaGiamGiaDAL();

        public List<DonHangDTO> GetList(string keyword, string trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            if (tuNgay.HasValue && denNgay.HasValue && denNgay.Value.Date < tuNgay.Value.Date)
                throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.");
            return dal.GetList(keyword, trangThai, tuNgay, denNgay);
        }

        public DonHangDTO GetById(int maDH)
        {
            DonHangDTO dh = dal.GetById(maDH);
            if (dh == null) throw new BusinessException("Không thể tải thông tin chi tiết đơn hàng.");
            return dh;
        }

        public List<ChiTietDonHangDTO> GetChiTiet(int maDH)
        {
            return dal.GetChiTiet(maDH);
        }

        /// <summary>Xác nhận đơn: kiểm tra và trừ tồn kho trong cùng một transaction</summary>
        public void XacNhan(int maDH)
        {
            int maQTV = Session.MaQTV;
            DBConnect.RunInTransaction(t =>
            {
                DonHangDTO dh = dal.GetForUpdate(t, maDH);
                if (dh == null) throw new BusinessException("Đơn hàng không tồn tại.");
                if (!TrangThaiDonHang.LaChoXuLy(dh.TrangThai))
                    throw new BusinessException("Chỉ xác nhận được đơn ở trạng thái \"Chờ xác nhận\". Đơn #" + maDH + " đang là \"" + dh.TrangThai + "\".");
                List<ChiTietDonHangDTO> items = dal.GetChiTiet(t, maDH);
                if (items.Count == 0) throw new BusinessException("Đơn hàng không có sản phẩm nào.");
                foreach (ChiTietDonHangDTO it in items)
                {
                    if (khoDal.TruTonKho(t, it.MaSP, it.SoLuong) == 0)
                        throw new BusinessException("Sản phẩm \"" + it.TenSP + "\" không đủ tồn kho (cần " + it.SoLuong +
                                                    ", còn " + it.SoLuongTon + ").\nHãy nhập thêm hàng hoặc hủy đơn.");
                }
                dal.UpdateTrangThai(t, maDH, TrangThaiDonHang.DaXacNhan, maQTV, null);
            });
        }

        /// <summary>Hủy / từ chối đơn đang chờ: lưu lý do, hoàn lượt mã giảm giá (nếu có)</summary>
        public void Huy(int maDH, string lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo == "") throw new BusinessException("Vui lòng nhập lý do hủy đơn.");
            if (lyDo.Length > 255) throw new BusinessException("Lý do hủy tối đa 255 ký tự.");
            int maQTV = Session.MaQTV;
            DBConnect.RunInTransaction(t =>
            {
                DonHangDTO dh = dal.GetForUpdate(t, maDH);
                if (dh == null) throw new BusinessException("Đơn hàng không tồn tại.");
                if (!TrangThaiDonHang.LaChoXuLy(dh.TrangThai))
                    throw new BusinessException("Chỉ hủy được đơn đang chờ xác nhận. Đơn #" + maDH + " đang là \"" + dh.TrangThai + "\".");
                dal.UpdateTrangThai(t, maDH, TrangThaiDonHang.DaHuy, maQTV, lyDo);
                if (dh.MaMGG.HasValue) mggDal.GiamLuotDung(t, dh.MaMGG.Value);
            });
        }

        /// <summary>Các trạng thái giao hàng có thể chuyển tới từ trạng thái hiện tại (Bảng 2.35)</summary>
        public static string[] TrangThaiTiepTheo(string hienTai)
        {
            if (hienTai == TrangThaiDonHang.DaXacNhan)
                return new[] { TrangThaiDonHang.DangGiao };
            if (hienTai == TrangThaiDonHang.DangGiao)
                return new[] { TrangThaiDonHang.DaGiao, TrangThaiDonHang.GiaoThatBai };
            return new string[0];
        }

        /// <summary>Cập nhật giao hàng; "Giao thất bại" bắt buộc lý do và hoàn lại tồn kho</summary>
        public void CapNhatGiaoHang(int maDH, string trangThaiMoi, string lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            if (trangThaiMoi == TrangThaiDonHang.GiaoThatBai && lyDo == "")
                throw new BusinessException("Vui lòng nhập lý do giao hàng thất bại.");
            if (lyDo.Length > 255) throw new BusinessException("Lý do / ghi chú tối đa 255 ký tự.");
            int maQTV = Session.MaQTV;
            DBConnect.RunInTransaction(t =>
            {
                DonHangDTO dh = dal.GetForUpdate(t, maDH);
                if (dh == null) throw new BusinessException("Đơn hàng không tồn tại.");
                if (!TrangThaiTiepTheo(dh.TrangThai).Contains(trangThaiMoi))
                    throw new BusinessException("Không thể chuyển đơn từ \"" + dh.TrangThai + "\" sang \"" + trangThaiMoi + "\".");
                if (trangThaiMoi == TrangThaiDonHang.GiaoThatBai)
                {
                    foreach (ChiTietDonHangDTO it in dal.GetChiTiet(t, maDH))
                        khoDal.CongTonKho(t, it.MaSP, it.SoLuong);
                }
                dal.UpdateTrangThai(t, maDH, trangThaiMoi, maQTV, lyDo == "" ? null : lyDo);
            });
        }
    }
}
