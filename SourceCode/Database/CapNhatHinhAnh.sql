/* =====================================================================
   Cập nhật ảnh cho 20 sản phẩm mẫu trên CSDL SportZoneDB ĐÃ CÓ SẴN (không xóa dữ liệu).
   Dùng khi CSDL được tạo trước khi bổ sung ảnh mẫu. Chỉ ghi vào sản phẩm đang chưa có ảnh,
   nên không ghi đè ảnh đã chọn trên phân hệ Admin.
   Cách chạy: mở bằng SSMS / Visual Studio (SQL Server Object Explorer) rồi Execute.
   ===================================================================== */
USE SportZoneDB;
GO
UPDATE SanPham SET HinhAnh = 'sp01-bong-da-ucv.svg' WHERE MaSP = 1 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp02-giay-da-bong-morelia.svg' WHERE MaSP = 2 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp03-gang-tay-thu-mon.svg' WHERE MaSP = 3 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp04-ao-dau-clb.svg' WHERE MaSP = 4 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp05-vot-cau-long-astrox.svg' WHERE MaSP = 5 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp06-hop-cau-long.svg' WHERE MaSP = 6 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp07-giay-cau-long.svg' WHERE MaSP = 7 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp08-bong-ro-bg4500.svg' WHERE MaSP = 8 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp09-bong-ro-tf500.svg' WHERE MaSP = 9 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp10-ta-tay-5kg.svg' WHERE MaSP = 10 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp11-tham-yoga.svg' WHERE MaSP = 11 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp12-day-khang-luc.svg' WHERE MaSP = 12 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp13-gang-tay-gym.svg' WHERE MaSP = 13 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp14-giay-chay-bo.svg' WHERE MaSP = 14 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp15-dong-ho-chay-bo.svg' WHERE MaSP = 15 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp16-dai-chay-bo.svg' WHERE MaSP = 16 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp17-kinh-boi.svg' WHERE MaSP = 17 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp18-mu-boi.svg' WHERE MaSP = 18 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp19-vot-bong-ban.svg' WHERE MaSP = 19 AND (HinhAnh IS NULL OR HinhAnh = '');
UPDATE SanPham SET HinhAnh = 'sp20-bong-ban-dhs.svg' WHERE MaSP = 20 AND (HinhAnh IS NULL OR HinhAnh = '');
GO
SELECT MaSP, TenSP, HinhAnh FROM SanPham ORDER BY MaSP;
GO
