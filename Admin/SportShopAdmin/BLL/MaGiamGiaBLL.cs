using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportShopAdmin.DAL;
using SportShopAdmin.DTO;
using SportShopAdmin.Utils;

namespace SportShopAdmin.BLL
{
    public class MaGiamGiaBLL
    {
        private readonly MaGiamGiaDAL dal = new MaGiamGiaDAL();

        public List<MaGiamGiaDTO> GetAll(string keyword = "")
        {
            return dal.GetAll(keyword);
        }

        public int Save(MaGiamGiaDTO m)
        {
            m.Code = (m.Code ?? "").Trim().ToUpperInvariant();
            if (!Regex.IsMatch(m.Code, @"^[A-Z0-9_-]{3,30}$"))
                throw new BusinessException("Code 3-30 ký tự, chỉ gồm chữ in hoa không dấu, số, '-' hoặc '_'.");
            if (m.PhanTramGiam < 1 || m.PhanTramGiam > 100) throw new BusinessException("Phần trăm giảm phải từ 1 đến 100.");
            if (m.SoTienGiamToiDa <= 0) throw new BusinessException("Số tiền giảm tối đa phải lớn hơn 0.");
            if (m.DonToiThieu < 0) throw new BusinessException("Giá trị đơn tối thiểu không được âm.");
            if (m.NgayKetThuc < m.NgayBatDau) throw new BusinessException("Ngày kết thúc phải sau ngày bắt đầu.");
            if (m.SoLuotToiDa < 1) throw new BusinessException("Số lượt sử dụng tối đa phải lớn hơn 0.");
            if (dal.ExistsCode(m.Code, m.MaMGG)) throw new BusinessException("Mã code \"" + m.Code + "\" đã tồn tại.");

            if (m.MaMGG == 0) return dal.Insert(m);

            MaGiamGiaDTO cu = dal.GetById(m.MaMGG);
            if (cu == null) throw new BusinessException("Mã giảm giá không còn tồn tại.");
            if (m.SoLuotToiDa < cu.SoLuotDaDung)
                throw new BusinessException("Số lượt tối đa không được nhỏ hơn số lượt đã dùng (" + cu.SoLuotDaDung + ").");
            dal.Update(m);
            return m.MaMGG;
        }

        /// <summary>Xóa mã; nếu mã đã được dùng trong đơn hàng thì chỉ vô hiệu hóa. Trả về thông báo kết quả.</summary>
        public string Delete(int maMGG)
        {
            if (dal.CountDonHang(maMGG) > 0)
            {
                dal.SetTrangThai(maMGG, false);
                return "Mã đã được dùng trong đơn hàng nên không thể xóa vĩnh viễn. Đã chuyển sang trạng thái \"Vô hiệu hóa\".";
            }
            dal.Delete(maMGG);
            return "Đã xóa mã giảm giá.";
        }

        /// <summary>Kiểm tra mã khi khách áp dụng (dùng cho Web - CartController)</summary>
        public ApDungMaResult KiemTraMa(string code, decimal tamTinh)
        {
            var kq = new ApDungMaResult();
            MaGiamGiaDTO m = dal.GetByCode((code ?? "").Trim().ToUpperInvariant());
            if (m == null || !m.TrangThai) { kq.ThongBao = "Mã không tồn tại"; return kq; }
            DateTime now = DateTime.Now;
            if (now < m.NgayBatDau || now > m.NgayKetThuc || m.SoLuotDaDung >= m.SoLuotToiDa)
            {
                kq.ThongBao = "Mã đã hết hạn hoặc hết lượt sử dụng";
                return kq;
            }
            if (tamTinh < m.DonToiThieu)
            {
                kq.ThongBao = "Đơn hàng tối thiểu " + m.DonToiThieu.ToString("N0") + " đ để dùng mã này";
                return kq;
            }
            kq.HopLe = true;
            kq.Ma = m;
            kq.TienGiam = Math.Min(Math.Round(tamTinh * m.PhanTramGiam / 100m, 0), m.SoTienGiamToiDa);
            kq.ThongBao = "Áp dụng mã thành công";
            return kq;
        }
    }
}
