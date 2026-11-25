using SportZoneWeb.DAL;
using SportZoneWeb.DTO;

namespace SportZoneWeb.BLL
{
    /// <summary>Sổ địa chỉ giao hàng (bảng DiaChiGiaoHang)</summary>
    public class DiaChiBLL
    {
        public const int SoDiaChiToiDa = 5;
        private readonly DiaChiDAL dal = new DiaChiDAL();
        private readonly TaiKhoanDAL taiKhoanDAL = new TaiKhoanDAL();

        /// <summary>Danh sách địa chỉ; khách chưa có sổ địa chỉ thì lấy địa chỉ trong tài khoản làm mặc định</summary>
        public List<DiaChiGiaoHangDTO> LayDanhSach(int maKH)
        {
            List<DiaChiGiaoHangDTO> ds = dal.GetByKhachHang(maKH);
            if (ds.Count == 0)
            {
                KhachHangDTO kh = taiKhoanDAL.GetKHById(maKH);
                if (kh != null && !string.IsNullOrWhiteSpace(kh.DiaChi))
                {
                    DBConnect.RunInTransaction(t => dal.Insert(t, new DiaChiGiaoHangDTO
                    {
                        MaKH = maKH, HoTenNhan = kh.HoTen, SDT = kh.SDT, DiaChi = kh.DiaChi.Trim(), MacDinh = true
                    }));
                    ds = dal.GetByKhachHang(maKH);
                }
            }
            return ds;
        }

        public DiaChiGiaoHangDTO MacDinh(int maKH) => LayDanhSach(maKH).FirstOrDefault(d => d.MacDinh);

        /// <summary>Thêm mới (MaDC = 0) hoặc sửa địa chỉ; datMacDinh = true thì đặt làm mặc định</summary>
        public void Luu(DiaChiGiaoHangDTO dc, bool datMacDinh)
        {
            dc.HoTenNhan = (dc.HoTenNhan ?? "").Trim();
            dc.SDT = (dc.SDT ?? "").Trim();
            dc.DiaChi = (dc.DiaChi ?? "").Trim();
            int soHienCo = dal.Count(dc.MaKH);
            if (dc.MaDC == 0 && soHienCo >= SoDiaChiToiDa)
                throw new BusinessException("Chỉ lưu tối đa " + SoDiaChiToiDa + " địa chỉ. Hãy xóa bớt địa chỉ không dùng.");

            DBConnect.RunInTransaction(t =>
            {
                int maDC = dc.MaDC;
                if (maDC == 0) maDC = dal.Insert(t, dc);
                else if (!dal.Update(t, dc)) throw new BusinessException("Không tìm thấy địa chỉ cần sửa.");
                if (datMacDinh || soHienCo == 0) dal.SetMacDinh(t, dc.MaKH, maDC);
                return maDC;
            });
        }

        public void DatMacDinh(int maKH, int maDC) => DBConnect.RunInTransaction(t => { dal.SetMacDinh(t, maKH, maDC); return 0; });

        public bool Xoa(int maKH, int maDC) => DBConnect.RunInTransaction(t => dal.Delete(t, maKH, maDC));
    }

    /// <summary>Danh sách yêu thích (bảng YeuThich)</summary>
    public class YeuThichBLL
    {
        private readonly YeuThichDAL dal = new YeuThichDAL();

        public List<int> LayMaSP(int maKH) => dal.GetMaSP(maKH);

        public bool LaYeuThich(int maKH, int maSP) => dal.Exists(maKH, maSP);

        /// <summary>Thêm / bỏ sản phẩm khỏi yêu thích. Trả về true nếu vừa thêm.</summary>
        public bool Doi(int maKH, int maSP)
        {
            if (dal.Exists(maKH, maSP))
            {
                dal.Delete(maKH, maSP);
                return false;
            }
            dal.Insert(maKH, maSP);
            return true;
        }
    }

    /// <summary>Đánh giá sản phẩm (bảng DanhGia) - chỉ khách đã mua và đã nhận hàng</summary>
    public class DanhGiaBLL
    {
        private readonly DanhGiaDAL dal = new DanhGiaDAL();

        public List<DanhGiaDTO> TheoSanPham(int maSP) => dal.GetBySanPham(maSP);

        public bool DuocDanhGia(int maKH, int maSP) => dal.DaMua(maKH, maSP);

        public void Gui(int maKH, int maSP, int soSao, string noiDung)
        {
            noiDung = (noiDung ?? "").Trim();
            if (!dal.DaMua(maKH, maSP))
                throw new BusinessException("Chỉ khách hàng đã mua và nhận sản phẩm này mới được đánh giá.");
            if (soSao < 1 || soSao > 5)
                throw new BusinessException("Vui lòng chọn số sao từ 1 đến 5.");
            if (noiDung.Length < 10 || noiDung.Length > 500)
                throw new BusinessException("Nhận xét từ 10 đến 500 ký tự.");
            dal.Upsert(maSP, maKH, soSao, noiDung);
        }
    }
}
