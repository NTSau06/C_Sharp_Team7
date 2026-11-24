/* =====================================================================
   BỔ SUNG CSDL SportZoneDB CHO CÁC CHỨC NĂNG MỚI CỦA WEB (không xóa dữ liệu cũ, chạy lại nhiều lần được)
     12. DiaChiGiaoHang  - sổ địa chỉ giao hàng của khách (tối đa 5, một địa chỉ mặc định)
     13. YeuThich        - danh sách sản phẩm yêu thích
     14. DanhGia         - đánh giá 1-5 sao + nhận xét (chỉ khách đã mua và đã nhận hàng)
     15. LichSuDonHang   - mốc thời gian đổi trạng thái đơn hàng (ghi tự động bằng trigger,
                           áp dụng cho cả Web và phân hệ Admin)
   Web tự chạy script này khi khởi động. Có thể chạy tay bằng SSMS: mở file rồi Execute.
   Khóa ngoại tới SanPham / KhachHang dùng ON DELETE CASCADE để Admin xóa sản phẩm không bị chặn.
   ===================================================================== */
USE SportZoneDB;
GO

/* ============================ 12. DiaChiGiaoHang ============================ */
IF OBJECT_ID(N'dbo.DiaChiGiaoHang', N'U') IS NULL
BEGIN
    CREATE TABLE DiaChiGiaoHang (
        MaDC       INT IDENTITY(1,1) CONSTRAINT PK_DiaChiGiaoHang PRIMARY KEY,
        MaKH       INT NOT NULL CONSTRAINT FK_DiaChi_KhachHang REFERENCES KhachHang(MaKH) ON DELETE CASCADE,
        HoTenNhan  NVARCHAR(100) NOT NULL,
        SDT        VARCHAR(15)   NOT NULL,
        DiaChi     NVARCHAR(255) NOT NULL,
        MacDinh    BIT NOT NULL CONSTRAINT DF_DiaChi_MacDinh DEFAULT 0
    );
    CREATE INDEX IX_DiaChiGiaoHang_MaKH ON DiaChiGiaoHang(MaKH);
END
GO

/* ============================ 13. YeuThich ============================ */
IF OBJECT_ID(N'dbo.YeuThich', N'U') IS NULL
BEGIN
    CREATE TABLE YeuThich (
        MaKH      INT NOT NULL CONSTRAINT FK_YeuThich_KhachHang REFERENCES KhachHang(MaKH) ON DELETE CASCADE,
        MaSP      INT NOT NULL CONSTRAINT FK_YeuThich_SanPham REFERENCES SanPham(MaSP) ON DELETE CASCADE,
        NgayThem  DATETIME NOT NULL CONSTRAINT DF_YeuThich_NgayThem DEFAULT GETDATE(),
        CONSTRAINT PK_YeuThich PRIMARY KEY (MaKH, MaSP)
    );
END
GO

/* ============================ 14. DanhGia ============================ */
IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NULL
BEGIN
    CREATE TABLE DanhGia (
        MaDG         INT IDENTITY(1,1) CONSTRAINT PK_DanhGia PRIMARY KEY,
        MaSP         INT NOT NULL CONSTRAINT FK_DanhGia_SanPham REFERENCES SanPham(MaSP) ON DELETE CASCADE,
        MaKH         INT NOT NULL CONSTRAINT FK_DanhGia_KhachHang REFERENCES KhachHang(MaKH) ON DELETE CASCADE,
        SoSao        TINYINT NOT NULL CONSTRAINT CK_DanhGia_SoSao CHECK (SoSao BETWEEN 1 AND 5),
        NoiDung      NVARCHAR(500) NOT NULL,
        NgayDanhGia  DATETIME NOT NULL CONSTRAINT DF_DanhGia_Ngay DEFAULT GETDATE(),
        CONSTRAINT UQ_DanhGia_SP_KH UNIQUE (MaSP, MaKH)          -- mỗi khách đánh giá 1 lần / sản phẩm
    );
END
GO

/* ============================ 15. LichSuDonHang ============================ */
IF OBJECT_ID(N'dbo.LichSuDonHang', N'U') IS NULL
BEGIN
    CREATE TABLE LichSuDonHang (
        MaLS       INT IDENTITY(1,1) CONSTRAINT PK_LichSuDonHang PRIMARY KEY,
        MaDH       INT NOT NULL CONSTRAINT FK_LichSu_DonHang REFERENCES DonHang(MaDH),
        TrangThai  NVARCHAR(30) NOT NULL,
        ThoiGian   DATETIME NOT NULL CONSTRAINT DF_LichSu_ThoiGian DEFAULT GETDATE(),
        GhiChu     NVARCHAR(255) NULL
    );
    CREATE INDEX IX_LichSuDonHang_MaDH ON LichSuDonHang(MaDH);
END
GO

-- Đơn hàng đã có trước khi tạo bảng: dựng mốc thời gian ước tính từ NgayDat (không vượt quá hiện tại)
INSERT INTO LichSuDonHang(MaDH, TrangThai, ThoiGian, GhiChu)
SELECT dh.MaDH, m.TrangThai,
       CASE WHEN DATEADD(MINUTE, m.SauPhut, dh.NgayDat) > GETDATE() THEN GETDATE() ELSE DATEADD(MINUTE, m.SauPhut, dh.NgayDat) END,
       CASE WHEN m.TrangThai = dh.TrangThai THEN dh.GhiChu END
FROM DonHang dh
CROSS APPLY (VALUES
    (1, N'Chờ xác nhận', 0),
    (2, N'Đã xác nhận', 240),
    (3, N'Đang giao', 1440),
    (4, N'Đã giao hàng', 4320),
    (4, N'Giao thất bại', 4320),
    (2, N'Đã hủy', 120)) m(Buoc, TrangThai, SauPhut)
WHERE NOT EXISTS (SELECT 1 FROM LichSuDonHang ls WHERE ls.MaDH = dh.MaDH)
  AND (   m.TrangThai = dh.TrangThai
       OR (m.Buoc = 1)
       OR (m.Buoc IN (2, 3) AND m.TrangThai IN (N'Đã xác nhận', N'Đang giao')
           AND m.Buoc < CASE dh.TrangThai WHEN N'Đang giao' THEN 3 WHEN N'Đã giao hàng' THEN 4 WHEN N'Giao thất bại' THEN 4 ELSE 0 END));
GO

-- Trigger: mỗi lần thêm đơn hoặc đổi trạng thái (Web hoặc Admin) => ghi một mốc vào LichSuDonHang
IF OBJECT_ID(N'dbo.TR_DonHang_LichSu', N'TR') IS NULL
    EXEC(N'
CREATE TRIGGER TR_DonHang_LichSu ON DonHang AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LichSuDonHang(MaDH, TrangThai, ThoiGian, GhiChu)
    SELECT i.MaDH, i.TrangThai, GETDATE(),
           CASE WHEN i.TrangThai IN (N''Đã hủy'', N''Giao thất bại'') THEN i.GhiChu END
    FROM inserted i
    LEFT JOIN deleted d ON d.MaDH = i.MaDH
    WHERE d.MaDH IS NULL OR d.TrangThai <> i.TrangThai;
END');
GO

-- Sổ địa chỉ: lấy địa chỉ trong tài khoản làm địa chỉ mặc định cho khách chưa có sổ địa chỉ
INSERT INTO DiaChiGiaoHang(MaKH, HoTenNhan, SDT, DiaChi, MacDinh)
SELECT kh.MaKH, kh.HoTen, kh.SDT, kh.DiaChi, 1
FROM KhachHang kh
WHERE kh.DiaChi IS NOT NULL AND LTRIM(RTRIM(kh.DiaChi)) <> ''
  AND NOT EXISTS (SELECT 1 FROM DiaChiGiaoHang dc WHERE dc.MaKH = kh.MaKH);
GO
