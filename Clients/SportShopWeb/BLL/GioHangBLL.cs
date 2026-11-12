using System.Data;
using SportShopWeb.DAL;
using SportShopWeb.DTO;
using SportShopWeb.Models;

namespace SportShopWeb.BLL
{
    /// <summary>UC 1.2.10 - Giỏ hàng (lưu trong bảng GioHang)</summary>
    public class GioHangBLL
    {
        public const int SoLuongToiDaMoiSanPham = 99;

        private readonly GioHangDAL gioHangDAL = new GioHangDAL();
        private readonly SanPhamDAL sanPhamDAL = new SanPhamDAL();

        public List<GioHangItemVM> LayGioHang(int maKH)
        {
            DataTable dt = gioHangDAL.GetChiTiet(maKH);
            var list = new List<GioHangItemVM>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new GioHangItemVM
                {
                    MaSP = r.Int("MaSP"),
                    TenSP = r.Str("TenSP"),
                    HinhAnh = r.Str("HinhAnh"),
                    TenDM = r.Str("TenDM"),
                    DonGia = r.Dec("GiaBan"),
                    SoLuong = r.Int("SoLuong"),
                    SoLuongTon = r.Int("SoLuongTon")
                });
            }
            return list;
        }

        public KetQua ThemVaoGio(int maKH, int maSP, int sl)
        {
            if (sl < 1) return KetQua.Loi("Số lượng phải lớn hơn 0.");
            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            if (sp == null) return KetQua.Loi("Sản phẩm không tồn tại.");
            if (sp.SoLuongTon <= 0) return KetQua.Loi("Sản phẩm \"" + sp.TenSP + "\" đã hết hàng.");

            int moi = gioHangDAL.GetSoLuong(maKH, maSP) + sl;
            int toiDa = Math.Min(sp.SoLuongTon, SoLuongToiDaMoiSanPham);
            if (moi > toiDa)
            {
                gioHangDAL.Upsert(maKH, maSP, toiDa);
                return KetQua.Ok("Chỉ còn " + sp.SoLuongTon + " sản phẩm \"" + sp.TenSP + "\". Giỏ hàng đã được cập nhật tối đa " + toiDa + ".");
            }
            gioHangDAL.Upsert(maKH, maSP, moi);
            return KetQua.Ok("Đã thêm vào giỏ hàng: " + sp.TenSP + " (x" + sl + ")");
        }

        public KetQua CapNhatSoLuong(int maKH, int maSP, int sl)
        {
            if (sl <= 0)
            {
                gioHangDAL.Delete(maKH, maSP);
                return KetQua.Ok("Đã xóa sản phẩm khỏi giỏ hàng.");
            }
            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            if (sp == null)
            {
                gioHangDAL.Delete(maKH, maSP);
                return KetQua.Loi("Sản phẩm không còn tồn tại nên đã được xóa khỏi giỏ.");
            }
            int toiDa = Math.Min(sp.SoLuongTon, SoLuongToiDaMoiSanPham);
            if (toiDa <= 0)
            {
                gioHangDAL.Delete(maKH, maSP);
                return KetQua.Loi("\"" + sp.TenSP + "\" đã hết hàng nên đã được xóa khỏi giỏ.");
            }
            if (sl > toiDa)
            {
                gioHangDAL.Upsert(maKH, maSP, toiDa);
                return KetQua.Loi("Chỉ còn " + sp.SoLuongTon + " sản phẩm \"" + sp.TenSP + "\" trong kho.");
            }
            gioHangDAL.Upsert(maKH, maSP, sl);
            return KetQua.Ok("Đã cập nhật số lượng.");
        }

        public bool XoaKhoiGio(int maKH, int maSP) => gioHangDAL.Delete(maKH, maSP);

        public decimal TinhTamTinh(List<GioHangItemVM> items) => items.Sum(i => i.ThanhTien);

        public decimal TinhTamTinh(int maKH) => TinhTamTinh(LayGioHang(maKH));

        public int DemSoLuong(int maKH) => gioHangDAL.Count(maKH);

        /// <summary>
        /// Kiểm tra tồn kho trước khi đặt hàng: sản phẩm hết hàng bị xóa khỏi giỏ,
        /// sản phẩm vượt tồn kho được giảm về mức tối đa. Trả về danh sách thông báo (rỗng = hợp lệ).
        /// </summary>
        public List<string> DieuChinhTheoTonKho(int maKH)
        {
            var thongBao = new List<string>();
            foreach (GioHangItemVM i in LayGioHang(maKH))
            {
                if (i.SoLuongTon <= 0)
                {
                    gioHangDAL.Delete(maKH, i.MaSP);
                    thongBao.Add("\"" + i.TenSP + "\" đã hết hàng và được xóa khỏi giỏ.");
                }
                else if (i.SoLuong > i.SoLuongTon)
                {
                    gioHangDAL.Upsert(maKH, i.MaSP, i.SoLuongTon);
                    thongBao.Add("\"" + i.TenSP + "\" chỉ còn " + i.SoLuongTon + " sản phẩm, số lượng đã được điều chỉnh.");
                }
            }
            return thongBao;
        }
    }
}
