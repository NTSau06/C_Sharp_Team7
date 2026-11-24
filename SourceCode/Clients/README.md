# SportZoneWeb – Phân hệ Bán hàng (ASP.NET Core MVC)

Website cho **Khách hàng** trong đề tài *"Xây dựng hệ thống quản lý và bán dụng cụ thể thao"* – Nhóm 7.
Dùng chung CSDL `SportZoneDB` (SQL Server) với phân hệ Quản trị (`Admin`).

## 1. Yêu cầu
- Visual Studio 2022 (17.14+) / Visual Studio 2026, workload **ASP.NET and web development**
- **.NET 10 SDK** (đi kèm Visual Studio mới)
- SQL Server bất kỳ: Express / Developer / **LocalDB** (cài qua Visual Studio Installer)
- Lần build đầu cần Internet để Visual Studio tải gói NuGet `Microsoft.Data.SqlClient`.

## 2. Chạy chương trình
1. Mở `C_Sharp_Team7\SportZone.sln` (cả Admin + Web) **hoặc** `Clients\SportZoneWeb.sln` (chỉ Web).
2. Nếu mở `SportZone.sln`: chuột phải **SportZoneWeb** → **Set as Startup Project**
   (hoặc chuột phải Solution → *Configure Startup Projects* → *Multiple startup projects* để chạy cả Admin và Web).
3. Bấm **F5** → trình duyệt mở `http://localhost:5080`.

Khi khởi động, Web sẽ **tự dò máy chủ SQL** (`.` → `.\SQLEXPRESS` → `(localdb)\MSSQLLocalDB` → `localhost`)
và **tự tạo CSDL SportZoneDB** từ `Database/SportZoneDB.sql` nếu chưa có (giống phân hệ Admin).
Muốn chỉ định máy chủ: sửa `Data Source` trong `appsettings.json`.

Tài khoản khách hàng mẫu: **nguyenvana / 123456** (tương tự: tranthibinh, lehoangcuong... mật khẩu 123456).
Mã giảm giá mẫu: `SPORT10` (đơn từ 500.000đ), `CHAOHE20` (đơn từ 1.000.000đ).

## 3. Cấu trúc (4 tầng – theo báo cáo)
| Thư mục | Vai trò |
|---|---|
| `DTO/` | `SanPhamDTO`, `DanhMucDTO`, `KhachHangDTO`, `GioHangDTO`, `DonHangDTO`, `ChiTietDonHangDTO`, `MaGiamGiaDTO`, `PhuongThucThanhToanDTO` |
| `DAL/` | `DBConnect` (SqlConnection, SqlParameter, SqlTransaction), `DatabaseInitializer`, `SanPhamDAL`, `DanhMucDAL`, `TaiKhoanDAL`, `GioHangDAL`, `DonHangDAL`, `MaGiamGiaDAL`, `PhuongThucThanhToanDAL` |
| `BLL/` | `SanPhamBLL`, `DanhMucBLL`, `TaiKhoanBLL`, `GioHangBLL`, `MaGiamGiaBLL.KiemTraMa()`, `ThanhToanBLL`, `DonHangBLL.DatHang()` (transaction) |
| `Controllers/` | `HomeController`, `CategoryController`, `ProductController`, `SearchController`, `CartController`, `CheckoutController`, `AccountController` |
| `Models/` | ViewModel: `ProductListVM`, `SanPhamFilter`, `GioHangItemVM`, `CartVM`, `CheckoutVM`, `LoginVM`, `RegisterVM` |
| `Views/`, `wwwroot/` | Giao diện Razor, CSS/JS thuần (không cần thư viện ngoài) |

## 4. Chức năng (ứng với Use Case)
- **Đăng nhập / Đăng ký** (1.2.1.1, 1.2.1.2) – mật khẩu `SALT$SHA256` giống Admin; kiểm tra trùng tên đăng nhập, email, SĐT.
- **Xem danh mục** (1.2.7.1–1.2.7.4) – danh sách danh mục, sản phẩm theo danh mục, **lọc** theo khoảng giá / còn hàng, **sắp xếp** mới nhất / bán chạy / giá / tên, **phân trang** 8 sản phẩm/trang (giữ nguyên bộ lọc khi chuyển trang).
- **Tìm kiếm** (1.2.8.1–1.2.8.5) – báo lỗi khi để trống, **gợi ý AJAX** khi gõ (`/Search/Suggest`), lọc theo giá / loại sản phẩm / còn hàng, sắp xếp, phân trang; tìm không phân biệt dấu.
- **Chi tiết sản phẩm** (1.2.9) – hết hàng thì khóa nút "Thêm vào giỏ"; mã sản phẩm sai → trang "Sản phẩm không tồn tại" và tự về trang chủ.
- **Giỏ hàng & đặt hàng** (1.2.10) – chưa đăng nhập → chuyển tới Đăng nhập rồi quay lại; giỏ lưu trong bảng `GioHang`; kiểm tra tồn kho lần cuối, sản phẩm hết hàng bị xóa khỏi giỏ; đơn mới ở trạng thái **Chờ xác nhận**.
- **Thanh toán** (1.2.11) – bắt buộc chọn phương thức thanh toán (TC23). Phạm vi đề tài (mục 1.5): chỉ hỗ trợ **Thanh toán khi nhận hàng (COD)** – đúng bảng `PhuongThucThanhToan` trong `SportZoneDB.sql`.
- **Áp dụng mã giảm giá** (1.2.14) – kiểm tra mã tồn tại, hiệu lực, còn lượt, đơn tối thiểu; giảm = min(tạm tính × %, mức tối đa); khi đặt hàng kiểm tra lại và tăng `SoLuotDaDung` trong cùng transaction (hết lượt → rollback).
- **Đơn hàng của tôi** (FR18) – xem lịch sử và chi tiết đơn, **lọc nhanh theo 6 trạng thái**; trạng thái do Admin cập nhật.
- **Ngoại lệ nghiệp vụ** – `BusinessException` → `OutOfStockException` (vượt tồn kho khi đặt hàng), `VoucherExpiredException` (mã hết hạn / hết lượt khi đặt hàng), `GioHangThayDoiException`.

> Tồn kho **chỉ bị trừ khi Quản trị viên xác nhận đơn** trên ứng dụng Desktop (UC 1.2.4.3).
> CSDL không có cột thương hiệu, nên bộ lọc dùng: khoảng giá, loại sản phẩm (danh mục), còn hàng.
> Gửi email xác nhận chưa được cài đặt (cần máy chủ SMTP); trang "Đặt hàng thành công" hiển thị mã đơn thay thế.

## 4b. Chức năng bổ sung (tài khoản, đơn hàng, sản phẩm)

Khi khởi động, Web tự chạy `Database/CapNhatChucNangWeb.sql` (không xóa dữ liệu, chạy lại nhiều lần được) để thêm 4 bảng:

| Bảng | Dùng cho |
|---|---|
| `DiaChiGiaoHang` | Sổ địa chỉ (tối đa 5, một địa chỉ mặc định, chọn nhanh khi đặt hàng) |
| `YeuThich` | Danh sách sản phẩm yêu thích (cần đăng nhập) |
| `DanhGia` | Đánh giá 1-5 sao + nhận xét, chỉ khách có đơn "Đã giao hàng" chứa sản phẩm |
| `LichSuDonHang` + trigger `TR_DonHang_LichSu` | Mốc thời gian đổi trạng thái đơn (ghi tự động cho cả Web và Admin) |

- **Hồ sơ cá nhân / Đổi mật khẩu** (`/Account/Profile`, `/Account/ChangePassword`): cập nhật bảng `KhachHang`; đổi mật khẩu cần mật khẩu hiện tại.
- **Quên mật khẩu** (`/Account/ForgotPassword`): mã OTP 6 số, hiệu lực 5 phút, sai quá 5 lần phải lấy mã mới. Chưa cấu hình SMTP nên mã được hiển thị trên trang để chạy thử.
- **Đơn hàng**: trang chi tiết `/Account/OrderDetail/{id}` có thanh theo dõi trạng thái; **Hủy đơn** khi còn "Chờ xác nhận" (hoàn lượt dùng mã giảm giá trong cùng transaction); **Mua lại** thêm sản phẩm của đơn cũ vào giỏ.
- **Sản phẩm**: sản phẩm liên quan, đã xem gần đây (lưu trong Session), lọc theo thương hiệu / kích cỡ / màu (CSDL chưa có các cột này nên suy ra trong `Utils/ThuocTinhSanPham.cs`).
- Admin bấm "Tạo lại CSDL mẫu" sẽ xóa cả 4 bảng mới; chạy lại Web để tạo lại.

## 5. Ảnh & giao diện
```
wwwroot/images/
├── sp01-bong-da-ucv.jpg … sp20-bong-ban-dhs.jpg   ảnh 20 sản phẩm mẫu (dùng chung cho Web và Admin)
├── banners/       banner khuyến mãi trang chủ
├── categories/    ảnh đại diện 7 danh mục
├── brand/         logo.svg, logo-dark.svg, favicon.svg/.ico
└── placeholders/  no-image.svg
```
- Cột `SanPham.HinhAnh` chỉ lưu **tên file**. Admin chép ảnh vào đúng thư mục này (`App.config` → `ImageFolder`),
  nên ảnh chọn trên Admin hiện ngay trên Web (cả Web và WinForms Admin đều đọc trực tiếp ảnh `.jpg`).
- `HinhAnh = NULL` hoặc file ảnh lỗi → hiển thị `placeholders/no-image.svg` (không bao giờ vỡ ảnh).
- CSDL tạo **trước** khi có ảnh mẫu: chạy `Database/CapNhatHinhAnh.sql` (không xóa dữ liệu) hoặc bấm
  "Tạo lại CSDL mẫu" ở màn hình đăng nhập Admin (xóa toàn bộ dữ liệu).
- Bảng màu theo Bảng 3.19: xanh đậm `#2C4A6B` (phụ `#1E3550`), nút tìm kiếm vàng `#F4B400`, giá & lỗi đỏ `#D32F2F`,
  thành công `#15803D`, cảnh báo `#FFF8E1`. Lưới sản phẩm 4 cột (máy tính) / 3 cột (≤ 992px) / 2 cột (≤ 576px);
  trên điện thoại (≤ 768px) dùng nút ☰ để mở menu danh mục và tài khoản.
