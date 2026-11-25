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
- **Giỏ hàng & đặt hàng** (1.2.10) – khách chưa đăng nhập có thể thêm/sửa/xóa giỏ trong Session và dùng "Mua ngay"; khi gửi đặt hàng mới cần đăng nhập/đăng ký, giỏ khách được gộp vào tài khoản và thông tin checkout được giữ lại; giỏ tài khoản lưu trong bảng `GioHang`, kiểm tra tồn kho lần cuối.
- **Thanh toán** (1.2.11) – bắt buộc chọn phương thức thanh toán (TC23); hỗ trợ COD và thanh toán sandbox VNPay / MoMo. Cần điền thông tin cổng ở biến môi trường (mục 4c) để bật thanh toán online.
- **Áp dụng mã giảm giá** (1.2.14) – kiểm tra mã tồn tại, hiệu lực, còn lượt, đơn tối thiểu; giảm = min(tạm tính × %, mức tối đa); khi đặt hàng kiểm tra lại và tăng `SoLuotDaDung` trong cùng transaction (hết lượt → rollback).
- **Đơn hàng của tôi** (FR18) – xem lịch sử và chi tiết đơn, **lọc nhanh theo 6 trạng thái**; trạng thái do Admin cập nhật.
- **Ngoại lệ nghiệp vụ** – `BusinessException` → `OutOfStockException` (vượt tồn kho khi đặt hàng), `VoucherExpiredException` (mã hết hạn / hết lượt khi đặt hàng), `GioHangThayDoiException`.

> Tồn kho **chỉ bị trừ khi Quản trị viên xác nhận đơn** trên ứng dụng Desktop (UC 1.2.4.3).
> CSDL không có cột thương hiệu, nên bộ lọc dùng: khoảng giá, loại sản phẩm (danh mục), còn hàng.
> Email xác nhận đơn hàng được gửi qua SMTP sau khi đặt hàng; cần cấu hình SMTP (mục 4c).

## 4b. Chức năng bổ sung (tài khoản, đơn hàng, sản phẩm)

Khi khởi động, Web tự chạy `Database/CapNhatChucNangWeb.sql` (không xóa dữ liệu, chạy lại nhiều lần được) để bổ sung các bảng và cột:

| Bảng | Dùng cho |
|---|---|
| `DiaChiGiaoHang` | Sổ địa chỉ (tối đa 5, một địa chỉ mặc định, chọn nhanh khi đặt hàng) |
| `YeuThich` | Danh sách sản phẩm yêu thích (cần đăng nhập) |
| `DanhGia` | Đánh giá 1-5 sao + nhận xét, chỉ khách có đơn "Đã giao hàng" chứa sản phẩm |
| `LichSuDonHang` + trigger `TR_DonHang_LichSu` | Mốc thời gian đổi trạng thái đơn (ghi tự động cho cả Web và Admin) |
| `DonHang.TrangThaiThanhToan`, `MaGiaoDich` | Kết quả thanh toán online; phương thức VNPay / MoMo được thêm tự động |
| `ThongBao` + trigger `TR_DonHang_ThongBao` | Thông báo cho khách khi trạng thái đơn hàng đổi |

- **Hồ sơ cá nhân / Đổi mật khẩu** (`/Account/Profile`, `/Account/ChangePassword`): cập nhật bảng `KhachHang`; đổi mật khẩu cần mật khẩu hiện tại.
- **Quên mật khẩu** (`/Account/ForgotPassword`): mã OTP 6 số, hiệu lực 5 phút, sai quá 5 lần phải lấy mã mới. Chưa cấu hình SMTP nên mã được hiển thị trên trang để chạy thử.
- **Đơn hàng**: trang chi tiết `/Account/OrderDetail/{id}` có thanh theo dõi trạng thái; **Hủy đơn** khi còn "Chờ xác nhận" (hoàn lượt dùng mã giảm giá trong cùng transaction); **Mua lại** thêm sản phẩm của đơn cũ vào giỏ.
- **Sản phẩm**: sản phẩm liên quan, đã xem gần đây (lưu trong Session), lọc theo thương hiệu / kích cỡ / màu (CSDL chưa có các cột này nên suy ra trong `Utils/ThuocTinhSanPham.cs`).
- **Hỗ trợ**: trang Liên hệ, Chính sách đổi trả, Câu hỏi thường gặp; thông báo đơn hàng; tài khoản bị vô hiệu hóa sẽ bị đăng xuất và chặn ở mọi request.
- Admin bấm "Tạo lại CSDL mẫu" có thể xóa dữ liệu ở các bảng mở rộng; chạy lại Web để áp dụng script cập nhật và tạo lại cấu trúc.

## 4c. Cấu hình email và thanh toán online

Các cấu hình mẫu để trống nằm trong `SportZoneWeb/appsettings.json`; không đưa mật khẩu SMTP, khóa VNPay hay khóa MoMo vào Git. Có thể đặt bằng User Secrets khi phát triển hoặc biến môi trường khi triển khai, ví dụ:

| Tên cấu hình | Biến môi trường | Mục đích |
|---|---|---|
| `Email:Host`, `Email:Port`, `Email:User`, `Email:Password`, `Email:From`, `Email:ShopEmail` | `Email__Host`, `Email__Port`, `Email__User`, `Email__Password`, `Email__From`, `Email__ShopEmail` | SMTP nhận thư liên hệ và gửi email xác nhận đơn |
| `VnPay:TmnCode`, `VnPay:HashSecret` | `VnPay__TmnCode`, `VnPay__HashSecret` | Thông tin tích hợp VNPay sandbox |
| `MoMo:PartnerCode`, `MoMo:AccessKey`, `MoMo:SecretKey` | `MoMo__PartnerCode`, `MoMo__AccessKey`, `MoMo__SecretKey` | Thông tin tích hợp MoMo sandbox |

Khi cổng chưa cấu hình, website chặn gửi đơn online và báo lỗi ngay tại bước checkout. Khi đã cấu hình, website chuyển khách tới trang thanh toán chính thức để quét QR; chỉ callback hợp lệ, đúng số tiền mới đánh dấu đơn đã thanh toán. Đơn online chưa thanh toán không thể được Admin xác nhận hoặc trừ tồn kho. IPN của cổng cần trỏ tới URL HTTPS công khai; địa chỉ localhost không truy cập được từ cổng.

Trong môi trường Development, `DemoPayment:Enabled` mặc định bật để thử luồng QR mà không cần tài khoản cổng: mở liên kết QR trên điện thoại sẽ giả lập thanh toán và hoàn tất đơn, không có giao dịch tiền thật. Để quét từ điện thoại, đặt `DemoPayment__BaseUrl` thành địa chỉ LAN của máy chạy Web (ví dụ `http://192.168.1.10:5080`) và cho phép máy khác truy cập cổng đó. Demo bị vô hiệu hóa ngoài Development.

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
