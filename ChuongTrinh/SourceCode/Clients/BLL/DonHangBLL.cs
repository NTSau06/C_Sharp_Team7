using System.Text.RegularExpressions;
using SportZoneWeb.DAL;
using SportZoneWeb.DTO;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

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

            List<GioHangItemVM> items;
            if (vm.LaMuaNgay)
            {
                // ----- "Mua ngay": chỉ đặt 1 sản phẩm, giỏ hàng giữ nguyên
                if (vm.MuaNgaySoLuong < 1 || vm.MuaNgaySoLuong > GioHangBLL.SoLuongToiDaMoiSanPham)
                    throw new BusinessException("Số lượng không hợp lệ.");
                items = gioHangBLL.LayMuaNgay(vm.MuaNgayMaSP.Value, vm.MuaNgaySoLuong);
                if (items.Count == 0)
                    throw new GioHangThayDoiException("Sản phẩm không còn tồn tại.");
            }
            else
            {
                // ----- kiểm tra giỏ hàng + tồn kho (A2: hết hàng thì xóa khỏi giỏ, yêu cầu xem lại giỏ)
                if (gioHangBLL.LayGioHang(maKH).Count == 0)
                    throw new GioHangThayDoiException("Giỏ hàng của bạn đang trống");
                List<string> thayDoi = gioHangBLL.DieuChinhTheoTonKho(maKH);
                if (thayDoi.Count > 0)
                    throw new GioHangThayDoiException(string.Join(" ", thayDoi) + " Vui lòng xem lại giỏ hàng.");
                items = gioHangBLL.LayGioHang(maKH);
                if (items.Count == 0)
                    throw new GioHangThayDoiException("Giỏ hàng của bạn đang trống");
            }

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

                if (!vm.LaMuaNgay) gioHangDAL.Clear(maKH, t);
                return maDH;
            });
        }

        /// <summary>Lấy đơn hàng của chính khách hàng (không cho xem đơn của người khác)</summary>
        public DonHangDTO LayDonHang(int maKH, int maDH)
        {
            DonHangDTO dh = donHangDAL.GetById(maDH);
            return dh != null && dh.MaKH == maKH ? dh : null;
        }

        /// <summary>Gửi email xác nhận đơn tới email tài khoản (chạy nền; lỗi SMTP không ảnh hưởng đơn hàng)</summary>
        public void GuiEmailXacNhan(int maKH, int maDH)
        {
            try
            {
                DonHangDTO dh = LayDonHang(maKH, maDH);
                KhachHangDTO kh = new TaiKhoanDAL().GetKHById(maKH);
                if (dh == null || kh == null || string.IsNullOrWhiteSpace(kh.Email)) return;
                EmailService.GuiNen(kh.Email, "[SportZone] Xác nhận đơn hàng #DH" + maDH, EmailService.NoiDungXacNhanDon(dh, kh.HoTen));
            }
            catch (Exception ex)
            {
                EmailService.Logger?.LogError(ex, "Không gửi được email xác nhận đơn #DH{MaDH}", maDH);
            }
        }

        public List<DonHangDTO> LichSu(int maKH) => donHangDAL.GetByKhachHang(maKH);

        private readonly LichSuDonHangDAL lichSuDAL = new LichSuDonHangDAL();

        /// <summary>Các mốc đổi trạng thái của đơn (bảng LichSuDonHang, ghi bằng trigger)</summary>
        public List<LichSuDonHangDTO> LichSuTrangThai(int maDH) => lichSuDAL.GetByDonHang(maDH);

        /// <summary>
        /// Khách tự hủy đơn: chỉ khi đơn còn "Chờ xác nhận"; hoàn 1 lượt dùng mã giảm giá trong cùng transaction.
        /// Tồn kho chưa bị trừ ở trạng thái này (Admin trừ khi xác nhận) nên không cần hoàn kho.
        /// </summary>
        public void HuyDon(int maKH, int maDH, string lyDo)
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo == "") throw new BusinessException("Vui lòng chọn hoặc nhập lý do hủy đơn.");
            if (lyDo.Length > 200) lyDo = lyDo.Substring(0, 200);
            DonHangDTO dh = LayDonHang(maKH, maDH);
            if (dh == null) throw new BusinessException("Không tìm thấy đơn hàng #DH" + maDH + ".");
            bool ok = DBConnect.RunInTransaction(t => donHangDAL.HuyDon(t, maKH, maDH, "Khách hàng hủy: " + lyDo));
            if (!ok)
                throw new BusinessException("Chỉ hủy được đơn đang ở trạng thái \"Chờ xác nhận\". Đơn #DH" + maDH + " hiện: " + dh.TrangThai + ".");
        }

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

        /// <summary>
        /// Danh sách địa chỉ & SĐT giao hàng khác từng dùng ở các đơn trước (loại trùng với thông tin tài khoản mặc định)
        /// </summary>
        public List<DiaChiGiaoItem> DiaChiDaDung(int maKH, KhachHangDTO kh)
        {
            var result = new List<DiaChiGiaoItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (kh != null && !string.IsNullOrWhiteSpace(kh.DiaChi))
                seen.Add(KhoaDiaChi(kh.HoTen, kh.SDT, kh.DiaChi));

            foreach (string raw in donHangDAL.GetDiaChiDaDung(maKH, 8))
            {
                DiaChiGiaoItem item = TachDiaChi(raw, kh);
                if (string.IsNullOrWhiteSpace(item.DiaChi)) continue;
                string key = KhoaDiaChi(item.HoTen, item.SDT, item.DiaChi);
                if (seen.Add(key))
                {
                    result.Add(item);
                    if (result.Count >= 4) break;
                }
            }
            return result;
        }

        private static string KhoaDiaChi(string hoTen, string sdt, string diaChi) =>
            (hoTen ?? "").Trim() + "|" + (sdt ?? "").Trim() + "|" + (diaChi ?? "").Trim();

        private static DiaChiGiaoItem TachDiaChi(string raw, KhachHangDTO kh)
        {
            raw = (raw ?? "").Trim();
            string[] parts = raw.Split(new[] { " - " }, 3, StringSplitOptions.None);
            if (parts.Length == 3 && Regex.IsMatch(parts[1].Trim(), @"^0\d{9,10}$"))
            {
                return new DiaChiGiaoItem
                {
                    HoTen = parts[0].Trim(),
                    SDT = parts[1].Trim(),
                    DiaChi = parts[2].Trim()
                };
            }
            return new DiaChiGiaoItem
            {
                HoTen = (kh?.HoTen ?? "").Trim(),
                SDT = (kh?.SDT ?? "").Trim(),
                DiaChi = raw
            };
        }
    }
}
