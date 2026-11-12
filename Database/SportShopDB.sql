/* =====================================================================
   HỆ THỐNG QUẢN LÝ VÀ BÁN DỤNG CỤ THỂ THAO - NHÓM 7
   Script tạo CSDL SportShopDB (Microsoft SQL Server 2012 trở lên)
   - 11 bảng theo lược đồ quan hệ trong báo cáo (đạt 3NF)
   - Ràng buộc: PK, FK, UNIQUE, CHECK đúng như mục "Đặc tả chi tiết các bảng"
   - Dữ liệu mẫu để chạy thử phân hệ Quản trị (Desktop) và Bán hàng (Web)

   Tài khoản mẫu:
     Quản trị viên : admin / admin123      ,  quanly / admin123
     Khách hàng    : nguyenvana / 123456   (tương tự cho các khách khác)
   Mật khẩu lưu dạng  SALT$SHA256(SALT + MatKhau)  (HEX in hoa) - khớp PasswordHasher (C#)
   ===================================================================== */
USE master;
GO
IF DB_ID(N'SportShopDB') IS NOT NULL
BEGIN
    ALTER DATABASE SportShopDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SportShopDB;
END
GO
CREATE DATABASE SportShopDB;
GO
USE SportShopDB;
GO

/* ============================ 1. PhuongThucThanhToan ============================ */
CREATE TABLE PhuongThucThanhToan (
    MaPTTT   INT IDENTITY(1,1) CONSTRAINT PK_PhuongThucThanhToan PRIMARY KEY,
    TenPTTT  NVARCHAR(100) NOT NULL CONSTRAINT UQ_PTTT_Ten UNIQUE
);
GO

/* ============================ 2. QuanTriVien ============================ */
CREATE TABLE QuanTriVien (
    MaQTV        INT IDENTITY(1,1) CONSTRAINT PK_QuanTriVien PRIMARY KEY,
    HoTen        NVARCHAR(100) NOT NULL,
    TenDangNhap  VARCHAR(50)   NOT NULL CONSTRAINT UQ_QTV_TenDangNhap UNIQUE,
    MatKhau      VARCHAR(200)  NOT NULL
);
GO

/* ============================ 3. KhachHang ============================ */
CREATE TABLE KhachHang (
    MaKH         INT IDENTITY(1,1) CONSTRAINT PK_KhachHang PRIMARY KEY,
    HoTen        NVARCHAR(100) NOT NULL,
    TenDangNhap  VARCHAR(50)   NOT NULL CONSTRAINT UQ_KH_TenDangNhap UNIQUE,
    MatKhau      VARCHAR(200)  NOT NULL,
    SDT          VARCHAR(15)   NULL,
    Email        VARCHAR(100)  NOT NULL CONSTRAINT UQ_KH_Email UNIQUE,
    DiaChi       NVARCHAR(255) NULL,
    TrangThai    BIT NOT NULL CONSTRAINT DF_KH_TrangThai DEFAULT 1   -- 1: hoạt động, 0: đã xóa mềm
);
GO

/* ============================ 4. DanhMuc ============================ */
CREATE TABLE DanhMuc (
    MaDM   INT IDENTITY(1,1) CONSTRAINT PK_DanhMuc PRIMARY KEY,
    TenDM  NVARCHAR(100) NOT NULL CONSTRAINT UQ_DanhMuc_Ten UNIQUE,
    MoTa   NVARCHAR(500) NULL
);
GO

/* ============================ 5. SanPham ============================ */
CREATE TABLE SanPham (
    MaSP            INT IDENTITY(1,1) CONSTRAINT PK_SanPham PRIMARY KEY,
    TenSP           NVARCHAR(200) NOT NULL,
    GiaBan          DECIMAL(18,0) NOT NULL CONSTRAINT CK_SP_GiaBan CHECK (GiaBan >= 0),
    SoLuongTon      INT NOT NULL CONSTRAINT DF_SP_SoLuongTon DEFAULT 0
                         CONSTRAINT CK_SP_SoLuongTon CHECK (SoLuongTon >= 0),
    MucTonToiThieu  INT NOT NULL CONSTRAINT DF_SP_MucTon DEFAULT 5
                         CONSTRAINT CK_SP_MucTon CHECK (MucTonToiThieu >= 0),   -- ngưỡng cảnh báo sắp hết hàng
    HinhAnh         NVARCHAR(255) NULL,
    MoTa            NVARCHAR(MAX) NULL,
    MaDM            INT NOT NULL CONSTRAINT FK_SanPham_DanhMuc REFERENCES DanhMuc(MaDM)
);
CREATE INDEX IX_SanPham_MaDM ON SanPham(MaDM);
GO

/* ============================ 6. MaGiamGia ============================ */
CREATE TABLE MaGiamGia (
    MaMGG            INT IDENTITY(1,1) CONSTRAINT PK_MaGiamGia PRIMARY KEY,
    Code             VARCHAR(30) NOT NULL CONSTRAINT UQ_MGG_Code UNIQUE,
    PhanTramGiam     INT NOT NULL CONSTRAINT CK_MGG_PhanTram CHECK (PhanTramGiam BETWEEN 1 AND 100),
    SoTienGiamToiDa  DECIMAL(18,0) NOT NULL CONSTRAINT CK_MGG_GiamToiDa CHECK (SoTienGiamToiDa >= 0),
    DonToiThieu      DECIMAL(18,0) NOT NULL CONSTRAINT DF_MGG_DonToiThieu DEFAULT 0
                          CONSTRAINT CK_MGG_DonToiThieu CHECK (DonToiThieu >= 0),
    NgayBatDau       DATETIME NOT NULL,
    NgayKetThuc      DATETIME NOT NULL,
    SoLuotToiDa      INT NOT NULL CONSTRAINT CK_MGG_SoLuotToiDa CHECK (SoLuotToiDa > 0),
    SoLuotDaDung     INT NOT NULL CONSTRAINT DF_MGG_SoLuotDaDung DEFAULT 0,
    TrangThai        BIT NOT NULL CONSTRAINT DF_MGG_TrangThai DEFAULT 1,     -- 1: kích hoạt, 0: vô hiệu hóa
    CONSTRAINT CK_MGG_Ngay   CHECK (NgayKetThuc >= NgayBatDau),
    CONSTRAINT CK_MGG_SoLuot CHECK (SoLuotDaDung >= 0 AND SoLuotDaDung <= SoLuotToiDa)
);
GO

/* ============================ 7. DonHang ============================ */
CREATE TABLE DonHang (
    MaDH        INT IDENTITY(1,1) CONSTRAINT PK_DonHang PRIMARY KEY,
    NgayDat     DATETIME NOT NULL CONSTRAINT DF_DH_NgayDat DEFAULT GETDATE(),
    TrangThai   NVARCHAR(30) NOT NULL CONSTRAINT DF_DH_TrangThai DEFAULT N'Chờ xác nhận'
                CONSTRAINT CK_DH_TrangThai CHECK (TrangThai IN (N'Chờ thanh toán', N'Chờ xác nhận', N'Đã xác nhận',
                                                                N'Đang giao', N'Đã giao', N'Giao thất bại', N'Đã hủy')),
    DiaChiGiao  NVARCHAR(255) NOT NULL,
    TienGiam    DECIMAL(18,0) NOT NULL CONSTRAINT DF_DH_TienGiam DEFAULT 0 CONSTRAINT CK_DH_TienGiam CHECK (TienGiam >= 0),
    TongTien    DECIMAL(18,0) NOT NULL CONSTRAINT DF_DH_TongTien DEFAULT 0 CONSTRAINT CK_DH_TongTien CHECK (TongTien >= 0),
    GhiChu      NVARCHAR(500) NULL,                       -- lý do hủy / giao thất bại
    MaKH        INT NOT NULL CONSTRAINT FK_DonHang_KhachHang REFERENCES KhachHang(MaKH),
    MaPTTT      INT NOT NULL CONSTRAINT FK_DonHang_PTTT REFERENCES PhuongThucThanhToan(MaPTTT),
    MaQTV       INT NULL     CONSTRAINT FK_DonHang_QuanTriVien REFERENCES QuanTriVien(MaQTV),
    MaMGG       INT NULL     CONSTRAINT FK_DonHang_MaGiamGia REFERENCES MaGiamGia(MaMGG)
);
CREATE INDEX IX_DonHang_NgayDat   ON DonHang(NgayDat);
CREATE INDEX IX_DonHang_TrangThai ON DonHang(TrangThai);
CREATE INDEX IX_DonHang_MaKH      ON DonHang(MaKH);
GO

/* ============================ 8. ChiTietDonHang ============================ */
CREATE TABLE ChiTietDonHang (
    MaDH     INT NOT NULL CONSTRAINT FK_CTDH_DonHang REFERENCES DonHang(MaDH) ON DELETE CASCADE,
    MaSP     INT NOT NULL CONSTRAINT FK_CTDH_SanPham REFERENCES SanPham(MaSP),
    SoLuong  INT NOT NULL CONSTRAINT CK_CTDH_SoLuong CHECK (SoLuong > 0),
    DonGia   DECIMAL(18,0) NOT NULL CONSTRAINT CK_CTDH_DonGia CHECK (DonGia >= 0),
    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY (MaDH, MaSP)
);
GO

/* ============================ 9. GioHang ============================ */
CREATE TABLE GioHang (
    MaKH     INT NOT NULL CONSTRAINT FK_GioHang_KhachHang REFERENCES KhachHang(MaKH) ON DELETE CASCADE,
    MaSP     INT NOT NULL CONSTRAINT FK_GioHang_SanPham REFERENCES SanPham(MaSP) ON DELETE CASCADE,
    SoLuong  INT NOT NULL CONSTRAINT CK_GioHang_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT PK_GioHang PRIMARY KEY (MaKH, MaSP)
);
GO

/* ============================ 10. PhieuNhapKho ============================ */
CREATE TABLE PhieuNhapKho (
    MaPN          INT IDENTITY(1,1) CONSTRAINT PK_PhieuNhapKho PRIMARY KEY,
    NgayNhap      DATETIME NOT NULL CONSTRAINT DF_PN_NgayNhap DEFAULT GETDATE(),
    TongTienNhap  DECIMAL(18,0) NOT NULL CONSTRAINT DF_PN_TongTien DEFAULT 0 CONSTRAINT CK_PN_TongTien CHECK (TongTienNhap >= 0),
    GhiChu        NVARCHAR(500) NULL,
    MaQTV         INT NOT NULL CONSTRAINT FK_PhieuNhap_QuanTriVien REFERENCES QuanTriVien(MaQTV)
);
CREATE INDEX IX_PhieuNhapKho_NgayNhap ON PhieuNhapKho(NgayNhap);
GO

/* ============================ 11. ChiTietPhieuNhap ============================ */
CREATE TABLE ChiTietPhieuNhap (
    MaPN         INT NOT NULL CONSTRAINT FK_CTPN_PhieuNhap REFERENCES PhieuNhapKho(MaPN) ON DELETE CASCADE,
    MaSP         INT NOT NULL CONSTRAINT FK_CTPN_SanPham REFERENCES SanPham(MaSP),
    SoLuongNhap  INT NOT NULL CONSTRAINT CK_CTPN_SoLuong CHECK (SoLuongNhap > 0),
    GiaNhap      DECIMAL(18,0) NOT NULL CONSTRAINT CK_CTPN_GiaNhap CHECK (GiaNhap > 0),
    CONSTRAINT PK_ChiTietPhieuNhap PRIMARY KEY (MaPN, MaSP)
);
GO

/* =====================================================================
                              DỮ LIỆU MẪU
   ===================================================================== */
INSERT INTO PhuongThucThanhToan(TenPTTT) VALUES
 (N'Thanh toán khi nhận hàng (COD)'),
 (N'Chuyển khoản ngân hàng'),
 (N'Ví điện tử MoMo');

-- Mật khẩu admin123
INSERT INTO QuanTriVien(HoTen, TenDangNhap, MatKhau) VALUES
 (N'Nguyễn Tam Sáu', 'admin',
   'A1B2C3D4E5F60718$' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST('A1B2C3D4E5F60718' + 'admin123' AS VARCHAR(200))), 2)),
 (N'Mai Tấn Thiện Nhân', 'quanly',
   '0F1E2D3C4B5A6978$' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST('0F1E2D3C4B5A6978' + 'admin123' AS VARCHAR(200))), 2));

-- Mật khẩu 123456
INSERT INTO KhachHang(HoTen, TenDangNhap, MatKhau, SDT, Email, DiaChi, TrangThai)
SELECT v.HoTen, v.TDN,
       v.Salt + '$' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(v.Salt + '123456' AS VARCHAR(200))), 2),
       v.SDT, v.Email, v.DiaChi, 1
FROM (VALUES
 (N'Nguyễn Văn An',   'nguyenvana', '1A2B3C4D5E6F7081', '0901234567', 'an.nguyen@gmail.com',   N'12 Nguyễn Trãi, Q.5, TP.HCM'),
 (N'Trần Thị Bình',   'tranthibinh','2B3C4D5E6F708192', '0912345678', 'binh.tran@gmail.com',   N'45 Lê Lợi, Q.1, TP.HCM'),
 (N'Lê Hoàng Cường',  'lehoangcuong','3C4D5E6F708192A3','0923456789', 'cuong.le@gmail.com',    N'78 Cách Mạng Tháng 8, Q.3, TP.HCM'),
 (N'Phạm Minh Dũng',  'phamminhdung','4D5E6F708192A3B4','0934567890', 'dung.pham@gmail.com',   N'9 Võ Văn Ngân, TP. Thủ Đức'),
 (N'Hoàng Thu Hà',    'hoangthuha', '5E6F708192A3B4C5', '0945678901', 'ha.hoang@gmail.com',    N'210 Phan Xích Long, Q. Phú Nhuận'),
 (N'Vũ Quốc Khánh',   'vuquockhanh','6F708192A3B4C5D6', '0956789012', 'khanh.vu@gmail.com',    N'33 Trường Chinh, Q. Tân Bình')
) v(HoTen, TDN, Salt, SDT, Email, DiaChi);

INSERT INTO DanhMuc(TenDM, MoTa) VALUES
 (N'Bóng đá',        N'Bóng, giày, găng tay thủ môn, áo đấu'),
 (N'Cầu lông',       N'Vợt, cầu, giày và phụ kiện cầu lông'),
 (N'Bóng rổ',        N'Bóng rổ và phụ kiện'),
 (N'Gym & Fitness',  N'Tạ, thảm tập, dây kháng lực, phụ kiện phòng tập'),
 (N'Chạy bộ',        N'Giày chạy, đồng hồ, phụ kiện chạy bộ'),
 (N'Bơi lội',        N'Kính bơi, mũ bơi, đồ bơi'),
 (N'Bóng bàn',       N'Vợt, bóng và phụ kiện bóng bàn');

INSERT INTO SanPham(TenSP, GiaBan, SoLuongTon, MucTonToiThieu, HinhAnh, MoTa, MaDM) VALUES
 (N'Bóng đá Động Lực UCV 3.05 số 5',          450000, 40, 10, NULL, N'Bóng thi đấu tiêu chuẩn FIFA Quality', 1),
 (N'Giày đá bóng Mizuno Morelia Neo',        2150000, 12,  5, NULL, N'Da kangaroo, đế TF cho sân cỏ nhân tạo', 1),
 (N'Găng tay thủ môn Adidas Predator',        650000,  2,  5, NULL, N'Mút bám dính cao, có xương bảo vệ ngón', 1),
 (N'Áo đấu câu lạc bộ mùa giải mới',          250000, 60, 15, NULL, N'Vải thun lạnh thoáng khí', 1),
 (N'Vợt cầu lông Yonex Astrox 77',           3200000,  8,  3, NULL, N'Vợt thiên công, khung carbon', 2),
 (N'Hộp cầu lông Thành Công (12 quả)',        230000, 50, 10, NULL, N'Cầu lông vũ tập luyện', 2),
 (N'Giày cầu lông Lining AYTS',              1350000, 15,  5, NULL, N'Đế cao su chống trượt', 2),
 (N'Bóng rổ Molten BG4500 số 7',             1450000, 10,  3, NULL, N'Da PU thi đấu FIBA', 3),
 (N'Bóng rổ Spalding TF-500',                 890000, 14,  4, NULL, N'Dùng trong nhà và ngoài trời', 3),
 (N'Tạ tay cao su 5kg (cặp)',                 420000, 25,  6, NULL, N'Bọc cao su chống trầy sàn', 4),
 (N'Thảm tập Yoga TPE 6mm',                   280000, 30,  8, NULL, N'Chống trượt hai mặt, kèm túi', 4),
 (N'Dây kháng lực (bộ 5 dây)',                190000, 35, 10, NULL, N'5 mức kháng lực khác nhau', 4),
 (N'Găng tay tập Gym',                        150000, 22,  8, NULL, N'Có quấn cổ tay', 4),
 (N'Giày chạy bộ Nike Pegasus 41',           3290000,  9,  3, NULL, N'Đệm ReactX êm ái', 5),
 (N'Đồng hồ chạy bộ Garmin Forerunner 165',  6990000,  1,  3, NULL, N'GPS, đo nhịp tim, màn AMOLED', 5),
 (N'Đai chạy bộ đựng điện thoại',             120000, 40, 10, NULL, N'Chống nước, co giãn', 5),
 (N'Kính bơi Speedo Futura',                  390000, 18,  5, NULL, N'Chống mờ, chống tia UV', 6),
 (N'Mũ bơi silicon',                           90000,  4, 10, NULL, N'Silicon co giãn, bền', 6),
 (N'Vợt bóng bàn Butterfly',                 1150000,  7,  3, NULL, N'Cốt gỗ 5 lớp, mặt Tenergy', 7),
 (N'Bóng bàn DHS 3 sao (hộp 6)',               95000, 45, 10, NULL, N'Bóng thi đấu tiêu chuẩn ITTF', 7);

INSERT INTO MaGiamGia(Code, PhanTramGiam, SoTienGiamToiDa, DonToiThieu, NgayBatDau, NgayKetThuc, SoLuotToiDa, SoLuotDaDung, TrangThai) VALUES
 ('SPORT10',   10, 100000,  500000, DATEADD(DAY, -30, CAST(GETDATE() AS DATE)), DATEADD(DAY, 60, CAST(GETDATE() AS DATE)), 100, 2, 1),
 ('CHAOHE20',  20, 200000, 1000000, DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), DATEADD(DAY, 20, CAST(GETDATE() AS DATE)),  50, 1, 1),
 ('TET2026',   15, 300000, 2000000, '20260120', '2026-02-20T23:59:59', 30, 0, 1),
 ('FREESHIP50',50,  50000,  300000, DATEADD(DAY, -5, CAST(GETDATE() AS DATE)), DATEADD(DAY, 30, CAST(GETDATE() AS DATE)), 200, 0, 0);

-- Phiếu nhập kho
INSERT INTO PhieuNhapKho(NgayNhap, TongTienNhap, GhiChu, MaQTV) VALUES
 (DATEADD(DAY, -200, GETDATE()), 0, N'Nhập hàng khai trương cửa hàng', 1),
 (DATEADD(DAY, -25,  GETDATE()), 0, N'Bổ sung hàng bán chạy', 2);

INSERT INTO ChiTietPhieuNhap(MaPN, MaSP, SoLuongNhap, GiaNhap)
SELECT v.MaPN, v.MaSP, v.SL, ROUND(sp.GiaBan * 0.7, -3)
FROM (VALUES (1,1,40),(1,2,15),(1,3,10),(1,4,70),(1,5,10),(1,6,60),(1,7,15),(1,8,10),(1,9,15),(1,10,25),
             (1,11,30),(1,12,35),(1,13,20),(1,14,10),(1,15,3),(1,16,40),(1,17,20),(1,18,10),(1,19,8),(1,20,45),
             (2,1,10),(2,4,10),(2,6,5),(2,13,5),(2,14,2),(2,16,5),(2,20,5)) v(MaPN, MaSP, SL)
JOIN SanPham sp ON sp.MaSP = v.MaSP;

UPDATE pn SET TongTienNhap = x.Tong
FROM PhieuNhapKho pn
JOIN (SELECT MaPN, SUM(SoLuongNhap * GiaNhap) AS Tong FROM ChiTietPhieuNhap GROUP BY MaPN) x ON x.MaPN = pn.MaPN;

-- Đơn hàng mẫu (đủ các trạng thái, trải đều 6 tháng gần nhất để thống kê)
INSERT INTO DonHang(NgayDat, TrangThai, DiaChiGiao, TienGiam, GhiChu, MaKH, MaPTTT, MaQTV, MaMGG) VALUES
 (DATEADD(DAY,-170,GETDATE()), N'Đã giao',        N'', 0,      NULL, 1, 1, 1, NULL),   -- 1
 (DATEADD(DAY,-160,GETDATE()), N'Đã giao',        N'', 0,      NULL, 2, 2, 1, NULL),   -- 2
 (DATEADD(DAY,-150,GETDATE()), N'Đã giao',        N'', 0,      NULL, 3, 1, 1, NULL),   -- 3
 (DATEADD(DAY,-135,GETDATE()), N'Đã giao',        N'', 0,      NULL, 4, 3, 2, NULL),   -- 4
 (DATEADD(DAY,-120,GETDATE()), N'Đã giao',        N'', 0,      NULL, 1, 1, 1, NULL),   -- 5
 (DATEADD(DAY,-105,GETDATE()), N'Đã giao',        N'', 0,      NULL, 5, 2, 2, NULL),   -- 6
 (DATEADD(DAY,-95, GETDATE()), N'Đã giao',        N'', 0,      NULL, 6, 1, 1, NULL),   -- 7
 (DATEADD(DAY,-80, GETDATE()), N'Đã giao',        N'', 0,      NULL, 2, 1, 1, NULL),   -- 8
 (DATEADD(DAY,-65, GETDATE()), N'Đã giao',        N'', 0,      NULL, 3, 3, 2, NULL),   -- 9
 (DATEADD(DAY,-50, GETDATE()), N'Đã giao',        N'', 0,      NULL, 4, 1, 1, NULL),   -- 10
 (DATEADD(DAY,-40, GETDATE()), N'Đã hủy',         N'', 0,      N'Khách hàng đổi ý, yêu cầu hủy đơn', 5, 2, 1, NULL), -- 11
 (DATEADD(DAY,-30, GETDATE()), N'Đã giao',        N'', 0,      NULL, 6, 1, 2, NULL),   -- 12
 (DATEADD(DAY,-20, GETDATE()), N'Giao thất bại',  N'', 0,      N'Không liên lạc được với khách hàng', 1, 1, 1, NULL), -- 13
 (DATEADD(DAY,-12, GETDATE()), N'Đã giao',        N'', 0,      NULL, 2, 2, 1, NULL),   -- 14
 (DATEADD(DAY,-6,  GETDATE()), N'Đang giao',      N'', 0,      NULL, 3, 1, 1, NULL),   -- 15
 (DATEADD(DAY,-4,  GETDATE()), N'Đã xác nhận',    N'', 75000,  NULL, 4, 1, 2, 1),      -- 16 SPORT10
 (DATEADD(DAY,-2,  GETDATE()), N'Chờ xác nhận',   N'', 98000,  NULL, 5, 3, NULL, 1),   -- 17 SPORT10
 (DATEADD(DAY,-1,  GETDATE()), N'Chờ xác nhận',   N'', 200000, NULL, 6, 2, NULL, 2),   -- 18 CHAOHE20
 (GETDATE(),                   N'Chờ xác nhận',   N'', 0,      NULL, 1, 1, NULL, NULL);-- 19 (thiếu tồn kho - để demo)

UPDATE dh SET DiaChiGiao = kh.DiaChi
FROM DonHang dh JOIN KhachHang kh ON kh.MaKH = dh.MaKH;

INSERT INTO ChiTietDonHang(MaDH, MaSP, SoLuong, DonGia)
SELECT v.MaDH, v.MaSP, v.SL, sp.GiaBan
FROM (VALUES (1,1,2),(1,4,2),  (2,5,1),(2,6,2),  (3,14,1),  (4,10,2),(4,11,1),  (5,8,1),(5,9,1),
             (6,2,1),(6,3,1),  (7,17,2),(7,18,3), (8,15,1), (9,19,1),(9,20,2), (10,7,1),(10,6,1),
             (11,5,1), (12,12,2),(12,13,1), (13,16,2), (14,14,1),(14,16,1), (15,1,1), (16,4,3),
             (17,11,2),(17,10,1), (18,5,1), (19,3,3)) v(MaDH, MaSP, SL)
JOIN SanPham sp ON sp.MaSP = v.MaSP;

-- TongTien = Tạm tính - Tiền giảm
UPDATE dh SET TongTien = x.TamTinh - dh.TienGiam
FROM DonHang dh
JOIN (SELECT MaDH, SUM(SoLuong * DonGia) AS TamTinh FROM ChiTietDonHang GROUP BY MaDH) x ON x.MaDH = dh.MaDH;

-- Giỏ hàng mẫu
INSERT INTO GioHang(MaKH, MaSP, SoLuong) VALUES (2, 11, 1), (2, 12, 2), (3, 6, 1);
GO

PRINT N'Tạo CSDL SportShopDB thành công!';
SELECT 'PhuongThucThanhToan' AS Bang, COUNT(*) AS SoDong FROM PhuongThucThanhToan UNION ALL
SELECT 'QuanTriVien', COUNT(*) FROM QuanTriVien UNION ALL
SELECT 'KhachHang', COUNT(*) FROM KhachHang UNION ALL
SELECT 'DanhMuc', COUNT(*) FROM DanhMuc UNION ALL
SELECT 'SanPham', COUNT(*) FROM SanPham UNION ALL
SELECT 'MaGiamGia', COUNT(*) FROM MaGiamGia UNION ALL
SELECT 'DonHang', COUNT(*) FROM DonHang UNION ALL
SELECT 'ChiTietDonHang', COUNT(*) FROM ChiTietDonHang UNION ALL
SELECT 'GioHang', COUNT(*) FROM GioHang UNION ALL
SELECT 'PhieuNhapKho', COUNT(*) FROM PhieuNhapKho UNION ALL
SELECT 'ChiTietPhieuNhap', COUNT(*) FROM ChiTietPhieuNhap;
GO
