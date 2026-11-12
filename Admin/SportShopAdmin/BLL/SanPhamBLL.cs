using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportShopAdmin.DAL;
using SportShopAdmin.DTO;
using SportShopAdmin.Utils;

namespace SportShopAdmin.BLL
{
    public class SanPhamBLL
    {
        private readonly SanPhamDAL dal = new SanPhamDAL();

        public List<SanPhamDTO> GetAll(string keyword = "", int? maDM = null)
        {
            return dal.GetAll(keyword, maDM);
        }

        public SanPhamDTO GetById(int maSP)
        {
            return dal.GetById(maSP);
        }

        public int Save(SanPhamDTO sp)
        {
            sp.TenSP = (sp.TenSP ?? "").Trim();
            sp.MoTa = (sp.MoTa ?? "").Trim();
            if (sp.TenSP == "") throw new BusinessException("Tên sản phẩm không được để trống.");
            if (sp.TenSP.Length > 200) throw new BusinessException("Tên sản phẩm tối đa 200 ký tự.");
            if (sp.MaDM <= 0) throw new BusinessException("Vui lòng chọn danh mục cho sản phẩm.");
            if (sp.GiaBan <= 0) throw new BusinessException("Giá bán phải lớn hơn 0.");
            if (sp.SoLuongTon < 0) throw new BusinessException("Số lượng tồn không được âm.");
            if (sp.MucTonToiThieu < 0) throw new BusinessException("Mức tồn tối thiểu không được âm.");
            if (string.IsNullOrWhiteSpace(sp.HinhAnh)) sp.HinhAnh = null;
            if (sp.MaSP == 0) return dal.Insert(sp);
            dal.Update(sp);
            return sp.MaSP;
        }

        public void Delete(int maSP)
        {
            if (dal.CountReferences(maSP) > 0)
                throw new BusinessException("Không thể xóa: sản phẩm đã phát sinh đơn hàng hoặc phiếu nhập kho (cần giữ lịch sử).");
            dal.Delete(maSP);
        }
    }
}
