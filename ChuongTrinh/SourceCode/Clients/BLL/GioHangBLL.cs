using System.Data;
using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;

namespace SportZoneWeb.BLL
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
                    MaDM = r.Int("MaDM"),
                    TenDM = r.Str("TenDM"),
                    DonGia = r.Dec("GiaBan"),
                    SoLuong = r.Int("SoLuong"),
                    SoLuongTon = r.Int("SoLuongTon")
                });
            }
            return list;
        }

        /// <summary>"Mua ngay": tạo danh sách 1 dòng hàng từ sản phẩm (không đọc / ghi bảng GioHang).
        /// Sản phẩm không tồn tại => danh sách rỗng.</summary>
        public List<GioHangItemVM> LayMuaNgay(int maSP, int soLuong)
        {
            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            if (sp == null) return new List<GioHangItemVM>();
            return new List<GioHangItemVM>
            {
                new GioHangItemVM
                {
                    MaSP = sp.MaSP,
                    TenSP = sp.TenSP,
                    HinhAnh = sp.HinhAnh,
                    MaDM = sp.MaDM,
                    TenDM = sp.TenDM,
                    DonGia = sp.GiaBan,
                    SoLuong = Math.Max(1, soLuong),
                    SoLuongTon = sp.SoLuongTon
                }
            };
        }

        /// <summary>Thông báo chuẩn khi số lượng vượt tồn kho (TC21)</summary>
        public static string ThongBaoVuotTonKho(int soLuongTon) =>
            "Số lượng vượt quá tồn kho hiện tại (chỉ còn " + soLuongTon + " sản phẩm)";

        /// <summary>Số lượng sản phẩm này đang có trong giỏ của khách</summary>
        public int LaySoLuong(int maKH, int maSP) => gioHangDAL.GetSoLuong(maKH, maSP);

        public KetQua ThemVaoGio(int maKH, int maSP, int sl)
        {
            if (sl < 1) return KetQua.Loi("Số lượng phải lớn hơn 0.");
            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            if (sp == null) return KetQua.Loi("Sản phẩm không tồn tại.");
            int dangCo = gioHangDAL.GetSoLuong(maKH, maSP);
            return ThemVaoGio(sp, maSP, sl, dangCo, moi => gioHangDAL.Upsert(maKH, maSP, moi));
        }

        public KetQua ThemVaoGioTam(Dictionary<int, int> gio, int maSP, int sl)
        {
            if (sl < 1) return KetQua.Loi("Số lượng phải lớn hơn 0.");
            int dangCo = gio.TryGetValue(maSP, out int soLuong) ? soLuong : 0;
            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            return ThemVaoGio(sp, maSP, sl, dangCo, moi => gio[maSP] = moi);
        }

        private KetQua ThemVaoGio(SanPhamDTO sp, int maSP, int sl, int dangCo, Action<int> luuSoLuong)
        {
            if (sl < 1) return KetQua.Loi("Số lượng phải lớn hơn 0.");
            if (sp == null) return KetQua.Loi("Sản phẩm không tồn tại.");
            if (sp.SoLuongTon <= 0) return KetQua.Loi("Sản phẩm \"" + sp.TenSP + "\" đã hết hàng.");

            int moi = dangCo + sl;
            int toiDa = Math.Min(sp.SoLuongTon, SoLuongToiDaMoiSanPham);
            if (moi > toiDa)
            {
                // BR01: chỉ kiểm tra tồn kho, không trừ SoLuongTon. Đưa giỏ về mức tối đa và báo rõ cho khách.
                if (dangCo != toiDa) luuSoLuong(toiDa);
                string lyDo = moi > sp.SoLuongTon
                    ? ThongBaoVuotTonKho(sp.SoLuongTon)
                    : "Mỗi sản phẩm chỉ đặt tối đa " + SoLuongToiDaMoiSanPham;
                return KetQua.Loi(lyDo + ". Giỏ hàng hiện có " + toiDa + " \"" + sp.TenSP + "\".");
            }
            luuSoLuong(moi);
            return KetQua.Ok("Đã thêm vào giỏ hàng: " + sp.TenSP + " (x" + sl + ")");
        }

        public List<GioHangItemVM> LayGioTam(Dictionary<int, int> gio)
        {
            var items = new List<GioHangItemVM>();
            foreach (KeyValuePair<int, int> item in gio)
            {
                if (item.Value < 1) continue;
                List<GioHangItemVM> dong = LayMuaNgay(item.Key, item.Value);
                if (dong.Count > 0) items.Add(dong[0]);
            }
            return items;
        }

        public KetQua CapNhatGioTam(Dictionary<int, int> gio, int maSP, int sl)
        {
            if (sl <= 0)
            {
                gio.Remove(maSP);
                return KetQua.Ok("Đã xóa sản phẩm khỏi giỏ hàng.");
            }

            SanPhamDTO sp = sanPhamDAL.GetById(maSP);
            if (sp == null)
            {
                gio.Remove(maSP);
                return KetQua.Loi("Sản phẩm không còn tồn tại nên đã được xóa khỏi giỏ.");
            }
            int toiDa = Math.Min(sp.SoLuongTon, SoLuongToiDaMoiSanPham);
            if (toiDa <= 0)
            {
                gio.Remove(maSP);
                return KetQua.Loi("\"" + sp.TenSP + "\" đã hết hàng nên đã được xóa khỏi giỏ.");
            }
            if (sl > toiDa)
            {
                gio[maSP] = toiDa;
                string lyDo = sl > sp.SoLuongTon
                    ? ThongBaoVuotTonKho(sp.SoLuongTon)
                    : "Mỗi sản phẩm chỉ đặt tối đa " + SoLuongToiDaMoiSanPham;
                return KetQua.Loi(lyDo + ". Số lượng \"" + sp.TenSP + "\" đã được điều chỉnh về " + toiDa + ".");
            }
            gio[maSP] = sl;
            return KetQua.Ok("Đã cập nhật số lượng.");
        }

        public List<string> DieuChinhGioTam(Dictionary<int, int> gio)
        {
            var thongBao = new List<string>();
            foreach (GioHangItemVM item in LayGioTam(gio))
            {
                if (item.SoLuongTon <= 0)
                {
                    gio.Remove(item.MaSP);
                    thongBao.Add("\"" + item.TenSP + "\" đã hết hàng và được xóa khỏi giỏ.");
                }
                else if (item.SoLuong > Math.Min(item.SoLuongTon, SoLuongToiDaMoiSanPham))
                {
                    int toiDa = Math.Min(item.SoLuongTon, SoLuongToiDaMoiSanPham);
                    gio[item.MaSP] = toiDa;
                    thongBao.Add("\"" + item.TenSP + "\" chỉ còn " + toiDa + " sản phẩm, số lượng đã được điều chỉnh.");
                }
            }
            return thongBao;
        }

        public int DemSoLuongGioTam(Dictionary<int, int> gio) => gio.Values.Where(sl => sl > 0).Sum();

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
                string lyDo = sl > sp.SoLuongTon
                    ? ThongBaoVuotTonKho(sp.SoLuongTon)
                    : "Mỗi sản phẩm chỉ đặt tối đa " + SoLuongToiDaMoiSanPham;
                return KetQua.Loi(lyDo + ". Số lượng \"" + sp.TenSP + "\" đã được điều chỉnh về " + toiDa + ".");
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
