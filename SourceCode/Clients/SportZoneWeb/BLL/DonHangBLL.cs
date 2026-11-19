using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;

namespace SportZoneWeb.BLL
{
    /// <summary>
    /// UC 1.2.10 Đặt hàng + UC 1.2.11 Thanh toán + UC 1.2.14 Áp dụng mã giảm giá.
    /// DatHang(): kiểm tra tồn kho lần cuối, áp mã giảm giá, ghi đơn + chi tiết, tăng lượt dùng mã,
    /// xóa giỏ hàng - tất cả trong MỘT SqlTransaction. Tồn kho được trừ khi Quản trị viên xác nhận đơn.
    /// </summary>
    public class DonHangBLL
    {
        private readonly DonHangDAL donHangDAL = new DonHangDAL();
        private readonly SanPhamDAL sanPhamDAL = new SanPhamDAL();
        private readonly GioHangDAL gioHangDAL = new GioHangDAL();
        private readonly GioHangBLL gioHangBLL = new GioHangBLL();
        private readonly MaGiamGiaBLL maGiamGiaBLL = new MaGiamGiaBLL();
        private readonly ThanhToanBLL thanhToanBLL = new ThanhToanBLL();

        public int DatHang(int maKH, CheckoutVM vm)
        {
            // ----- kiểm tra thông tin giao hàng và phương thức thanh toán
            string hoTen = (vm.HoTenNhan ?? "").Trim();
            string sdt = (vm.SDT ?? "").Trim();
            string diaChi = (vm.DiaChiGiao ?? "").Trim();
            if (hoTen == "" || sdt == "" || diaChi == "")
                throw new BusinessException("Vui lòng nhập đầy đủ thông tin giao hàng.");
            if (!thanhToanBLL.KiemTraPhuongThuc(vm.MaPTTT))
                throw new BusinessException("Vui lòng chọn một phương thức thanh toán", "MaPTTT");

            // ----- kiểm tra giỏ hàng + tồn kho (A2: hết hàng thì xóa khỏi giỏ, yêu cầu xem lại giỏ)
            if (gioHangBLL.LayGioHang(maKH).Count == 0)
                throw new GioHangThayDoiException("Giỏ hàng của bạn đang trống");
            List<string> thayDoi = gioHangBLL.DieuChinhTheoTonKho(maKH);
            if (thayDoi.Count > 0)
                throw new GioHangThayDoiException(string.Join(" ", thayDoi) + " Vui lòng xem lại giỏ hàng.");
            List<GioHangItemVM> items = gioHangBLL.LayGioHang(maKH);
            if (items.Count == 0)
                throw new GioHangThayDoiException("Giỏ hàng của bạn đang trống");

            string code = MaGiamGiaBLL.ChuanHoaCode(vm.MaGiamGia);
            string diaChiGiao = hoTen + " - " + sdt + " - " + diaChi;
            if (diaChiGiao.Length > 255) diaChiGiao = diaChiGiao.Substring(0, 255);

            return DBConnect.RunInTransaction(t =>
            {
                // Kiểm tra tồn kho lần cuối (khóa dòng sản phẩm) và lấy giá bán hiện tại
                decimal tamTinh = 0;
                var dong = new List<(int MaSP, int SoLuong, decimal DonGia)>();
                foreach (GioHangItemVM i in items)
                {
                    SanPhamDTO sp = sanPhamDAL.GetForUpdate(t, i.MaSP);
                    if (sp == null)
                        throw new GioHangThayDoiException("Sản phẩm \"" + i.TenSP + "\" không còn tồn tại. Vui lòng xem lại giỏ hàng.");
                    // BR01-BR02: CHỈ kiểm tra tồn kho, KHÔNG trừ SoLuongTon (Admin trừ khi xác nhận đơn)
                    if (sp.SoLuongTon < i.SoLuong)
                        throw new OutOfStockException(sp.MaSP, sp.TenSP, sp.SoLuongTon);
                    dong.Add((sp.MaSP, i.SoLuong, sp.GiaBan));
                    tamTinh += sp.GiaBan * i.SoLuong;
                }

                // BR06-BR08: kiểm tra lại mã + tăng lượt dùng trong cùng giao dịch (5a: hết lượt => VoucherExpiredException => rollback)
                int? maMGG = null;
                decimal tienGiam = 0;
                if (code != "")
                {
                    ApDungMaResult kq = maGiamGiaBLL.ApDungKhiDatHang(t, code, tamTinh);
                    maMGG = kq.Ma.MaMGG;
                    tienGiam = kq.TienGiam;
                }

                var dh = new DonHangDTO
                {
                    TrangThai = TrangThaiDonHang.ChoXacNhan,
                    DiaChiGiao = diaChiGiao,
                    TienGiam = tienGiam,
                    TongTien = Math.Max(0, tamTinh - tienGiam),
                    MaKH = maKH,
                    MaPTTT = vm.MaPTTT.Value,
                    MaMGG = maMGG
                };
                int maDH = donHangDAL.Insert(t, dh);
                foreach (var d in dong)
                    donHangDAL.InsertChiTiet(t, maDH, d.MaSP, d.SoLuong, d.DonGia);

                gioHangDAL.Clear(maKH, t);
                return maDH;
            });
        }

        /// <summary>Lấy đơn hàng của chính khách hàng (không cho xem đơn của người khác)</summary>
        public DonHangDTO LayDonHang(int maKH, int maDH)
        {
            DonHangDTO dh = donHangDAL.GetById(maDH);
            return dh != null && dh.MaKH == maKH ? dh : null;
        }

        public List<DonHangDTO> LichSu(int maKH) => donHangDAL.GetByKhachHang(maKH);

        /// <summary>FR18 - Lịch sử đơn hàng, lọc theo 1 trong 6 trạng thái (rỗng / không hợp lệ = tất cả)</summary>
        public OrdersVM LichSu(int maKH, string trangThai)
        {
            List<DonHangDTO> tatCa = donHangDAL.GetByKhachHang(maKH);
            string tt = TrangThaiDonHang.TatCa.Contains(trangThai) ? trangThai : null;
            return new OrdersVM
            {
                TrangThai = tt,
                TongSo = tatCa.Count,
                DonHangs = tt == null ? tatCa : tatCa.Where(d => d.TrangThai == tt).ToList(),
                SoLuongTheoTrangThai = tatCa.GroupBy(d => d.TrangThai).ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}
