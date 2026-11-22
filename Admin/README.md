# SportShopAdmin – Phân hệ Quản trị (Windows Forms)

Ứng dụng Desktop cho **Quản trị viên** trong đề tài *"Xây dựng hệ thống quản lý và bán dụng cụ thể thao"* – Nhóm 7.
Dùng chung CSDL `SportShopDB` (SQL Server) với ứng dụng Web bán hàng (thư mục `Clients`).

## 1. Yêu cầu
- Visual Studio 2019/2022 (workload **.NET desktop development**), .NET Framework **4.8**
- SQL Server 2012 trở lên (Express / Developer / LocalDB) + SSMS
- Không cần cài thêm gói NuGet nào.

## 2. Chạy chương trình (CSDL được tạo tự động)
1. Mở **`SportShop.sln`** (ở thư mục gốc `C_Sharp_Team7`) hoặc `Admin/SportShopAdmin.sln` bằng Visual Studio.
   ⚠ Mở file **.sln**, không dùng *File → Open → Folder*.
2. Bấm **F5** (hoặc nút ▶ Start).
3. Ở màn hình đăng nhập bấm **Đăng nhập** (admin / admin123). Chương trình sẽ:
   - Kết nối máy chủ trong ô **Máy chủ SQL**; nếu không được thì **tự dò** lần lượt
     `.` → `.\SQLEXPRESS` → `.\SQLEXPRESS01` → `.\SQLEXPRESS02` → `(localdb)\MSSQLLocalDB` → `localhost` và hỏi dùng máy chủ tìm thấy.
   - Nếu chưa có CSDL `SportShopDB` → **tự chạy script `Database/SportShopDB.sql`** (nhúng sẵn trong .exe)
     để tạo 11 bảng + dữ liệu mẫu, rồi đăng nhập.
   - Tên máy chủ dùng được sẽ được lưu lại cho các lần chạy sau.
4. Muốn đưa dữ liệu về như ban đầu (trước khi demo): bấm link **"Tạo lại CSDL mẫu"** ở màn hình đăng nhập.

Tài khoản: **admin / admin123** (hoặc quanly / admin123). Khách hàng mẫu: nguyenvana / 123456.

### Nếu máy chưa có SQL Server
Cách nhanh nhất: **Visual Studio Installer → Modify → Individual components →** tick
**SQL Server Express LocalDB** → Modify. Sau đó chạy lại chương trình (máy chủ `(localdb)\MSSQLLocalDB`).

### Tạo CSDL bằng tay (không bắt buộc)
Mở `Database/SportShopDB.sql` trong SSMS (hoặc Visual Studio: *View → SQL Server Object Explorer*,
chuột phải máy chủ → *New Query*, dán script) → **Execute**. Script xóa (nếu có) và tạo lại `SportShopDB`.

> Các Form được dựng hoàn toàn bằng code (không dùng Designer). Trong Solution Explorer, chuột phải → **View Code (F7)** để xem/sửa.

## 3. Cấu trúc 4 tầng (theo báo cáo)
| Thư mục | Vai trò |
|---|---|
| `DTO/` | Lớp thực thể ánh xạ 11 bảng (`SanPhamDTO`, `DonHangDTO`, `MaGiamGiaDTO`, `PhieuNhapKhoDTO`...) |
| `DAL/` | `DBConnect` (SqlConnection, SqlParameter chống SQL Injection, SqlTransaction), `DatabaseInitializer` (dò máy chủ, tự tạo CSDL) + các lớp `...DAL` |
| `BLL/` | Quy tắc nghiệp vụ: kiểm tra dữ liệu, xác nhận đơn – trừ kho, hủy đơn – hoàn lượt mã, giao thất bại – hoàn kho, nhập kho, thống kê |
| `GUI/` | Các Form `frm...` |
| `Utils/` | `PasswordHasher` (SALT + SHA-256), `ReportExporter` (Excel/PDF/CSV), `ImageHelper`, `Session` |

## 4. Chức năng (ứng với Use Case)
- **Đăng nhập / Đổi mật khẩu** (1.2.1.1) – mật khẩu băm SALT$SHA256.
- **Quản lý danh mục** (1.2.2) – thêm/sửa/xóa/tìm; không cho xóa danh mục còn sản phẩm.
- **Quản lý sản phẩm** (1.2.3) – thêm/sửa/xóa/tìm/lọc, chọn ảnh; tô đỏ sản phẩm sắp hết hàng. Số lượng tồn chỉ đọc (sản phẩm mới có tồn = 0, chỉ tăng qua phiếu nhập – BR04). Lỗi nhập liệu hiện cạnh ô nhập bằng ErrorProvider (cũng áp dụng cho Danh mục, Khách hàng, Mã giảm giá).
- **Quản lý đơn hàng** (1.2.4) – lọc theo từ khóa/trạng thái/ngày, xem chi tiết, **Xác nhận** (trừ tồn kho trong transaction), **Hủy/Từ chối** (lưu lý do, hoàn lượt mã giảm giá), **Cập nhật giao hàng** (Đã xác nhận → Đang giao → Đã giao hàng / Giao thất bại – bắt buộc lý do, hoàn kho; đúng Bảng 2.35).
- **Tài khoản khách hàng** (1.2.5) – thêm/sửa/xóa mềm (`TrangThai = 0`)/khôi phục/tìm; kiểm tra trùng tên đăng nhập, email, SĐT; không xóa khách còn đơn chưa hoàn tất. Khi tạo mới, gửi thông tin đăng nhập qua `EmailService` (cấu hình `Smtp*` trong App.config; để trống thì bỏ qua).
- **Thống kê doanh thu** (1.2.6) – theo ngày/tuần/tháng/quý/năm kèm biểu đồ; báo cáo theo sản phẩm/danh mục/mã giảm giá/khách hàng/phương thức thanh toán; xuất **Excel (.xlsx)**, **PDF** (qua "Microsoft Print to PDF"), CSV.
- **Quản lý tồn kho** (1.2.12) – tồn hiện tại + cảnh báo, lập phiếu nhập `frmNhapKho` (cộng dồn tồn kho; mở nhanh qua menu **Nhập kho**), xem phiếu nhập, lịch sử nhập/xuất.
- **Mã giảm giá** (1.2.13) – thêm/sửa/xóa; mã đã dùng chỉ được vô hiệu hóa. `MaGiamGiaBLL.KiemTraMa()` sẵn sàng cho Web (UC 1.2.14).

## 5. Quy ước dữ liệu dùng chung với Web
- Trạng thái đơn (6 trạng thái chuẩn hóa, mục 2.7.2): `Chờ xác nhận`, `Đã xác nhận`, `Đang giao`, `Đã giao hàng`, `Giao thất bại`, `Đã hủy`. CSDL tạo bằng script cũ được tự nâng cấp khi đăng nhập.
- `DonHang.TongTien = Tạm tính − TienGiam`. Doanh thu chỉ tính đơn **Đã giao hàng**.
- Web đặt hàng: tạo đơn `Chờ xác nhận` + tăng `SoLuotDaDung` của mã (trong 1 transaction); **tồn kho bị trừ khi Admin xác nhận**.
- Ảnh sản phẩm: cột `HinhAnh` chỉ lưu tên file; thư mục ảnh cấu hình ở `ImageFolder` trong `App.config`.
- Mật khẩu: `SALT$HEX(SHA256(SALT + MatKhau))` – xem `Utils/PasswordHasher.cs`.
