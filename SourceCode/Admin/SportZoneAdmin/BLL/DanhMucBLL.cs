using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportZoneAdmin.DAL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.BLL
{
    public class DanhMucBLL
    {
        private readonly DanhMucDAL dal = new DanhMucDAL();

        public List<DanhMucDTO> GetAll(string keyword = "")
        {
            return dal.GetAll(keyword);
        }

        /// <summary>Thêm mới (MaDM = 0) hoặc cập nhật danh mục. Trả về mã danh mục.</summary>
        public int Save(DanhMucDTO dm)
        {
            dm.TenDM = (dm.TenDM ?? "").Trim();
            dm.MoTa = (dm.MoTa ?? "").Trim();
            if (dm.TenDM == "") throw new BusinessException("Tên danh mục không được để trống.", "TenDM");
            if (dm.TenDM.Length > 100) throw new BusinessException("Tên danh mục tối đa 100 ký tự.", "TenDM");
            if (dm.MoTa.Length > 500) throw new BusinessException("Mô tả tối đa 500 ký tự.", "MoTa");
            if (dal.ExistsTen(dm.TenDM, dm.MaDM)) throw new BusinessException("Tên danh mục đã tồn tại.", "TenDM");
            if (dm.MaDM == 0) return dal.Insert(dm);
            dal.Update(dm);
            return dm.MaDM;
        }

        public void Delete(int maDM)
        {
            int soSP = dal.CountSanPham(maDM);
            if (soSP > 0)
                throw new BusinessException("Không thể xóa: danh mục đang có " + soSP + " sản phẩm. Hãy chuyển hoặc xóa sản phẩm trước.");
            dal.Delete(maDM);
        }
    }
}
