# SportShopWeb – Phân hệ Bán hàng (ASP.NET Core MVC)

Website cho **Khách hàng** trong đề tài *"Xây dựng hệ thống quản lý và bán dụng cụ thể thao"* – Nhóm 7.
Dùng chung CSDL `SportShopDB` (SQL Server) với phân hệ Quản trị (`Admin`).

## 1. Yêu cầu
- Visual Studio 2022 (17.14+) / Visual Studio 2026, workload **ASP.NET and web development**
- **.NET 10 SDK** (đi kèm Visual Studio mới)
- SQL Server bất kỳ: Express / Developer / **LocalDB** (cài qua Visual Studio Installer)
- Lần build đầu cần Internet để Visual Studio tải gói NuGet `Microsoft.Data.SqlClient`.

## 2. Chạy chương trình
1. Mở `C_Sharp_Team7\SportShop.sln` (cả Admin + Web) **hoặc** `Clients\SportShopWeb.sln` (chỉ Web).
2. Nếu mở `SportShop.sln`: chuột phải **SportShopWeb** → **Set as Startup Project**
   (hoặc chuột phải Solution → *Configure Startup Projects* → *Multiple startup projects* để chạy cả Admin và Web).
3. Bấm **F5** → trình duyệt mở `http://localhost:5080`.

Khi khởi động, Web sẽ **tự dò máy chủ SQL** (`.` → `.\SQLEXPRESS` → `(localdb)\MSSQLLocalDB` → `localhost`)
và **tự tạo CSDL SportShopDB** từ `Database/SportShopDB.sql` nếu chưa có (giống phân hệ Admin).
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
- **Xem danh mục** (1.2.7.1–1.2.7.4) – danh sách danh mục, sản phẩm theo danh mục, **lọc** theo khoảng giá / còn hàng, **sắp xếp** mới nhất / bán chạy / giá / tên.
- **Tìm kiếm** (1.2.8.1–1.2.8.5) – báo lỗi khi để trống, **gợi ý AJAX** khi gõ (`/Search/Suggest`), lọc theo giá / loại sản phẩm / còn hàng, sắp xếp; tìm không phân biệt dấu.
- **Chi tiết sản phẩm** (1.2.9) – hết hàng thì khóa nút "Thêm vào giỏ"; mã sản phẩm sai → trang "Sản phẩm không tồn tại" và tự về trang chủ.
- **Giỏ hàng & đặt hàng** (1.2.10) – chưa đăng nhập → chuyển tới Đăng nhập rồi quay lại; giỏ lưu trong bảng `GioHang`; kiểm tra tồn kho lần cuối, sản phẩm hết hàng bị xóa khỏi giỏ; đơn mới ở trạng thái **Chờ xác nhận**.
- **Thanh toán** (1.2.11) – bắt buộc chọn phương thức (COD / Chuyển khoản / MoMo).
- **Áp dụng mã giảm giá** (1.2.14) – kiểm tra mã tồn tại, hiệu lực, còn lượt, đơn tối thiểu; giảm = min(tạm tính × %, mức tối đa); khi đặt hàng kiểm tra lại và tăng `SoLuotDaDung` trong cùng transaction (hết lượt → rollback).
- **Đơn hàng của tôi** – xem lịch sử và chi tiết đơn, trạng thái do Admin cập nhật.

> Tồn kho **chỉ bị trừ khi Quản trị viên xác nhận đơn** trên ứng dụng Desktop (UC 1.2.4.3).
> CSDL không có cột thương hiệu, nên bộ lọc dùng: khoảng giá, loại sản phẩm (danh mục), còn hàng.
> Gửi email xác nhận chưa được cài đặt (cần máy chủ SMTP); trang "Đặt hàng thành công" hiển thị mã đơn thay thế.

## 5. Ảnh sản phẩm
Ảnh nằm trong `wwwroot/images`, cột `HinhAnh` chỉ lưu tên file. Admin đã được cấu hình chép ảnh vào đúng thư mục này
(`App.config` → `ImageFolder`), nên ảnh chọn trên Admin sẽ hiện trên Web. Sản phẩm chưa có ảnh hiển thị khung màu theo danh mục.
