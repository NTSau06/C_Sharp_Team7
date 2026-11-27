<p align="center">
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/brand/logo.svg" alt="SportZone" width="320" />
</p>

<p align="center">
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/banners/banner-khuyen-mai.svg" alt="Banner SportZone" width="100%" />
</p>

<h3 align="center">Hệ thống quản lý và bán dụng cụ thể thao – Nhóm 7</h3>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/ASP.NET%20Core-.NET%2010-512BD4" />
  <img alt=".NET Framework 4.8" src="https://img.shields.io/badge/WinForms-.NET%20Framework%204.8-5C2D91" />
  <img alt="SQL Server" src="https://img.shields.io/badge/SQL%20Server-Express%20%2F%20LocalDB-CC2927" />
</p>

## Danh mục

<p>
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/bong-da.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/cau-long.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/bong-ro.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/gym-fitness.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/chay-bo.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/boi-loi.svg" width="90" />
  <img src="ChuongTrinh/SourceCode/Clients/wwwroot/images/categories/bong-ban.svg" width="90" />
</p>

## Cấu trúc thư mục

| Thành phần | Công nghệ / Mô tả | Đường dẫn |
|---|---|---|
| **Web bán hàng** (khách hàng) | ASP.NET Core MVC, .NET 10 | `ChuongTrinh/SourceCode/Clients/` |
| **Quản trị** (admin) | Windows Forms, .NET Framework 4.8 | `ChuongTrinh/SourceCode/Admin/` |
| **Cơ sở dữ liệu** | SQL Server (`SportZoneDB`, 11 bảng) | `ChuongTrinh/Database/SportZoneDB.sql` |
| **Báo cáo đồ án** | Báo cáo Word & PDF Nhóm 7 | `BaoCao/` |

## Chạy nhanh

1. Cài **Visual Studio 2022 (17.14+) / 2026** với workload *ASP.NET and web development* và *.NET desktop development*, cùng **SQL Server Express hoặc LocalDB**.
2. Mở **`SportZone.sln`** → chuột phải solution → *Configure Startup Projects* → *Multiple startup projects* → chọn **Start** cho cả `SportZoneAdmin` và `SportZoneWeb`.
3. Bấm **F5**. CSDL được **tự tạo** lần chạy đầu.

| | Địa chỉ / tài khoản |
|---|---|
| Web | http://localhost:5080 — `nguyenvana` / `123456` |
| Admin | `admin` / `admin123` |
| Mã giảm giá | `SPORT10`, `CHAOHE20` |
