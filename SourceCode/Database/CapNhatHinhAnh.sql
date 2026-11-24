/* =====================================================================
   Cập nhật ảnh (.jpg) cho 20 sản phẩm mẫu trên CSDL SportZoneDB ĐÃ CÓ SẴN (không xóa dữ liệu).
   Dùng khi CSDL được tạo trước khi chuyển sang ảnh .jpg (chưa có ảnh hoặc đang trỏ tới .svg/.png cũ),
   nên không ghi đè ảnh riêng đã chọn trên phân hệ Admin.
   Cách chạy: mở bằng SSMS / Visual Studio (SQL Server Object Explorer) rồi Execute.
   ===================================================================== */
USE SportZoneDB;
GO
UPDATE SanPham SET HinhAnh = 'sp01-bong-da-ucv.jpg' WHERE MaSP = 1 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp01-bong-da-ucv.svg', 'sp01-bong-da-ucv.png'));
UPDATE SanPham SET HinhAnh = 'sp02-giay-da-bong-morelia.jpg' WHERE MaSP = 2 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp02-giay-da-bong-morelia.svg', 'sp02-giay-da-bong-morelia.png'));
UPDATE SanPham SET HinhAnh = 'sp03-gang-tay-thu-mon.jpg' WHERE MaSP = 3 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp03-gang-tay-thu-mon.svg', 'sp03-gang-tay-thu-mon.png'));
UPDATE SanPham SET HinhAnh = 'sp04-ao-dau-clb.jpg' WHERE MaSP = 4 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp04-ao-dau-clb.svg', 'sp04-ao-dau-clb.png'));
UPDATE SanPham SET HinhAnh = 'sp05-vot-cau-long-astrox.jpg' WHERE MaSP = 5 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp05-vot-cau-long-astrox.svg', 'sp05-vot-cau-long-astrox.png'));
UPDATE SanPham SET HinhAnh = 'sp06-hop-cau-long.jpg' WHERE MaSP = 6 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp06-hop-cau-long.svg', 'sp06-hop-cau-long.png'));
UPDATE SanPham SET HinhAnh = 'sp07-giay-cau-long.jpg' WHERE MaSP = 7 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp07-giay-cau-long.svg', 'sp07-giay-cau-long.png'));
UPDATE SanPham SET HinhAnh = 'sp08-bong-ro-bg4500.jpg' WHERE MaSP = 8 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp08-bong-ro-bg4500.svg', 'sp08-bong-ro-bg4500.png'));
UPDATE SanPham SET HinhAnh = 'sp09-bong-ro-tf500.jpg' WHERE MaSP = 9 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp09-bong-ro-tf500.svg', 'sp09-bong-ro-tf500.png'));
UPDATE SanPham SET HinhAnh = 'sp10-ta-tay-5kg.jpg' WHERE MaSP = 10 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp10-ta-tay-5kg.svg', 'sp10-ta-tay-5kg.png'));
UPDATE SanPham SET HinhAnh = 'sp11-tham-yoga.jpg' WHERE MaSP = 11 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp11-tham-yoga.svg', 'sp11-tham-yoga.png'));
UPDATE SanPham SET HinhAnh = 'sp12-day-khang-luc.jpg' WHERE MaSP = 12 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp12-day-khang-luc.svg', 'sp12-day-khang-luc.png'));
UPDATE SanPham SET HinhAnh = 'sp13-gang-tay-gym.jpg' WHERE MaSP = 13 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp13-gang-tay-gym.svg', 'sp13-gang-tay-gym.png'));
UPDATE SanPham SET HinhAnh = 'sp14-giay-chay-bo.jpg' WHERE MaSP = 14 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp14-giay-chay-bo.svg', 'sp14-giay-chay-bo.png'));
UPDATE SanPham SET HinhAnh = 'sp15-dong-ho-chay-bo.jpg' WHERE MaSP = 15 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp15-dong-ho-chay-bo.svg', 'sp15-dong-ho-chay-bo.png'));
UPDATE SanPham SET HinhAnh = 'sp16-dai-chay-bo.jpg' WHERE MaSP = 16 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp16-dai-chay-bo.svg', 'sp16-dai-chay-bo.png'));
UPDATE SanPham SET HinhAnh = 'sp17-kinh-boi.jpg' WHERE MaSP = 17 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp17-kinh-boi.svg', 'sp17-kinh-boi.png'));
UPDATE SanPham SET HinhAnh = 'sp18-mu-boi.jpg' WHERE MaSP = 18 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp18-mu-boi.svg', 'sp18-mu-boi.png'));
UPDATE SanPham SET HinhAnh = 'sp19-vot-bong-ban.jpg' WHERE MaSP = 19 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp19-vot-bong-ban.svg', 'sp19-vot-bong-ban.png'));
UPDATE SanPham SET HinhAnh = 'sp20-bong-ban-dhs.jpg' WHERE MaSP = 20 AND (HinhAnh IS NULL OR HinhAnh = '' OR HinhAnh IN ('sp20-bong-ban-dhs.svg', 'sp20-bong-ban-dhs.png'));
GO
SELECT MaSP, TenSP, HinhAnh FROM SanPham ORDER BY MaSP;
GO
