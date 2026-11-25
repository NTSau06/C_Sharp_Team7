THƯ MỤC ẢNH DÙNG CHUNG ADMIN <-> WEB
====================================
- Ảnh sản phẩm nằm NGAY TẠI thư mục này (không vào thư mục con). Cột SanPham.HinhAnh chỉ lưu TÊN FILE.
  + 20 sản phẩm mẫu: sp01-bong-da-ucv.jpg ... sp20-bong-ban-dhs.jpg (dùng chung cho Web và Admin).
  + Ảnh chọn trên Admin được chép vào đây với tên sp_xxxxxxxxxxxx.jpg/.png
    (Admin\SportZoneAdmin\App.config > ImageFolder = ..\..\..\..\Clients\SportZoneWeb\wwwroot\images).
- banners/       : nền banner khuyến mãi trang chủ (W1)
- categories/    : ảnh đại diện 7 danh mục (bong-da, cau-long, bong-ro, gym-fitness, chay-bo, boi-loi, bong-ban; the-thao = mặc định)
- brand/         : logo.svg (nền tối), logo-dark.svg (nền sáng), favicon.svg / favicon.ico / apple-touch-icon.png
- placeholders/  : no-image.svg – hiển thị khi sản phẩm chưa có ảnh hoặc file ảnh bị lỗi/không tồn tại.
