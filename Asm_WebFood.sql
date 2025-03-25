-- Tạo cơ sở dữ liệu
CREATE DATABASE ASM_C4_WEBFOOD;
GO

USE ASM_C4_WEBFOOD_New;
GO
--********************************************************--
--				Chú thích và luồng hoạt động			  --
--********************************************************--
GO
/*
Chú thích hoạt động của dữ liệu trong các bảng:

1. **Bảng Quản trị viên (QuanTriVien)**: 
   - Lưu trữ thông tin của các quản trị viên trong hệ thống.
   - Mỗi quản trị viên có một ID duy nhất, email, mật khẩu, họ tên và tình trạng tài khoản (hoạt động hoặc không).

2. **Bảng Khách viếng thăm (KhachVienTham)**: 
   - Lưu trữ thông tin của khách viếng thăm trang web.
   - Thông tin bao gồm ID, email, mật khẩu, họ tên, số điện thoại, địa chỉ, và tình trạng tài khoản.
   - Không có quyền truy cập quản trị.

3. **Bảng Khách hàng (KhachHang)**: 
   - Chứa thông tin của khách hàng đã đăng ký, cho phép họ thực hiện giao dịch trên nền tảng.
   - Tương tự như bảng quản trị viên và khách viếng thăm.

4. **Bảng Danh mục sản phẩm (DanhMucSanPham)**: 
   - Lưu trữ danh sách các danh mục sản phẩm.
   - Giúp phân loại sản phẩm trong hệ thống.

5. **Bảng Sản phẩm (SanPham)**: 
   - Chứa thông tin chi tiết về sản phẩm.
   - Bao gồm tên, hình ảnh, mô tả, giá, số lượng còn và danh mục mà sản phẩm thuộc về.

6. **Bảng Giỏ hàng (GioHang)**: 
   - Lưu trữ thông tin về giỏ hàng của khách hàng.
   - Bao gồm sản phẩm đã chọn, số lượng và tổng tiền.

7. **Bảng Đơn đặt hàng (DonDatHang)**: 
   - Ghi nhận thông tin về đơn đặt hàng của khách hàng.
   - Bao gồm thông tin về khách hàng, giỏ hàng và tổng tiền.

8. **Bảng Chi tiết đơn hàng Đã Đặt (ChiTietDonDatHang)**: 
   - Lưu trữ thông tin chi tiết về từng sản phẩm trong đơn đặt hàng.
   - Bao gồm số lượng và giá của sản phẩm.

	**Tổng quan quy trình từ sản phẩm -> mua hàng**:

1. Khách hàng duyệt và chọn sản phẩm.
   - Khách hàng truy cập trang sản phẩm và lựa chọn sản phẩm mà họ muốn.

2. Thêm sản phẩm vào giỏ hàng.
   - Sau khi chọn sản phẩm, khách hàng có thể thêm sản phẩm đó vào giỏ hàng.

3. Xem giỏ hàng và tiến hành đặt hàng.
   - Khách hàng có thể xem lại giỏ hàng của mình, kiểm tra các sản phẩm đã chọn và tổng số tiền.
   - Khi đã sẵn sàng, họ chọn tùy chọn để tiến hành đặt hàng.

4. Tạo đơn đặt hàng từ giỏ hàng.
   - Hệ thống tạo một đơn đặt hàng mới dựa trên thông tin trong giỏ hàng của khách hàng.

5. Lưu chi tiết đơn hàng cho mỗi sản phẩm trong đơn hàng.
   - Các thông tin chi tiết về từng sản phẩm trong đơn hàng (như số lượng, giá) được lưu trữ.

6. Xác nhận đơn hàng cho khách hàng.
   - Sau khi đơn hàng được tạo thành công, hệ thống gửi thông báo xác nhận đơn hàng đến khách hàng.
*/
Go
--********************************************************--
--				Tạo bảng cho các thực thể				  --
--********************************************************--

-- Bảng Quản trị viên
CREATE TABLE QuanTriVien (
    MaQuanTri INT PRIMARY KEY IDENTITY(1,1),		  -- Khóa chính
	HoTen NVARCHAR(255) NOT NULL,                     -- Họ và tên của quản trị viên
    Email NVARCHAR(255) NOT NULL UNIQUE,              -- Email duy nhất của quản trị viên
    MatKhau NVARCHAR(255) NOT NULL,                   -- Mật khẩu của quản trị viên
    Hinh NVARCHAR(255),			                      -- Hinh của quản trị viên
    TinhTrang NVARCHAR(50) NOT NULL DEFAULT 'Hoạt động'  -- Tình trạng tài khoản
);
GO

-- Bảng Khách hàng
CREATE TABLE KhachHang (
    MaKhachHang INT PRIMARY KEY IDENTITY(1,1),         -- Khóa chính
	HoTen NVARCHAR(255) NOT NULL,                       -- Họ và tên của khách hàng
	Hinh NVARCHAR(400),									-- Hình ảnh khách hàng
    Email NVARCHAR(255) NOT NULL UNIQUE,                -- Email duy nhất của khách hàng
    MatKhau NVARCHAR(255) NOT NULL,                     -- Mật khẩu của khách hàng
    SoDienThoai NVARCHAR(20),                           -- Số điện thoại của khách hàng
    DiaChi NVARCHAR(255),                                -- Địa chỉ của khách hàng
    TinhTrang NVARCHAR(50) NOT NULL DEFAULT 'Hoạt động'  -- Tình trạng tài khoản
);
GO

-- Bảng Danh mục sản phẩm
CREATE TABLE DanhMucSanPham (
    MaDanhMuc INT PRIMARY KEY IDENTITY(1,1),           -- Khóa chính
    TenDanhMuc NVARCHAR(255) NOT NULL UNIQUE           -- Tên danh mục
);
GO

-- Bảng Sản phẩm
CREATE TABLE SanPham (
    MaSanPham INT PRIMARY KEY IDENTITY(1,1),           -- Khóa chính
    TenSanPham NVARCHAR(255) NOT NULL,                 -- Tên sản phẩm
    HinhAnh NVARCHAR(200) NOT NULL,                     -- Đường dẫn hình ảnh sản phẩm
    MoTa NVARCHAR(500),                                 -- Mô tả sản phẩm
    Gia DECIMAL(10, 2) NOT NULL,                        -- Giá sản phẩm
    SoLuong INT NOT NULL,                               -- Số lượng còn 
    DanhMuc INT,                                        -- ID danh mục sản phẩm (khóa ngoại)
    NgayTao DATETIME DEFAULT GETDATE(),                 -- Ngày tạo sản phẩm
    NgayCapNhat DATETIME DEFAULT GETDATE()              -- Ngày cập nhật sản phẩm
);
GO

-- Bảng Sản phẩm yêu thích
CREATE TABLE SanPhamYeuThich (
    MaYeuThich INT PRIMARY KEY IDENTITY(1,1),          -- Khóa chính
    MaKhachHang INT NOT NULL,                           -- Khóa ngoại, liên kết đến khách hàng
    MaSanPham INT NOT NULL,                             -- Khóa ngoại, liên kết đến sản phẩm        
);
GO

-- Bảng Giỏ hàng
CREATE TABLE GioHang (
    MaGioHang INT PRIMARY KEY IDENTITY(1,1),           -- Khóa chính
    MaKhachHang INT NOT NULL,                           -- Khóa ngoại, liên kết đến khách hàng
    MaSanPham INT NOT NULL,                             -- Khóa ngoại, liên kết đến sản phẩm
    SoLuong INT NOT NULL,                               -- Số lượng sản phẩm trong giỏ
    TongTien DECIMAL(10, 2) NOT NULL                   -- Tổng tiền sản phẩm
);
GO

-- Bảng Chi tiết đơn hàng Đã Đặt
CREATE TABLE ChiTietDonDatHang (
    MaChiTiet INT PRIMARY KEY IDENTITY(1,1),           -- Khóa chính
    MaKhachHang INT NOT NULL,                           -- Khóa ngoại, liên kết đến đơn đặt hàng
    MaSanPham INT NOT NULL,                             -- Khóa ngoại, liên kết đến sản phẩm
    SoLuong INT NOT NULL,                               -- Số lượng sản phẩm đặt
    Gia DECIMAL(10, 2) NOT NULL,                        -- Giá sản phẩm
	NgayGiao DATETIME,
	NgayNhan DATETIME,
	NgayThanhToan DATETIME,
	TrangThai Nvarchar(50) DEFAULT N'Đang xử lý'
);
GO

--********************************************************--
--						Các Khóa Ngoại					  --
--********************************************************--
Go

-- Khóa ngoại cho bảng Sản phẩm
ALTER TABLE SanPham ADD FOREIGN KEY (DanhMuc) REFERENCES DanhMucSanPham(MaDanhMuc);
GO

ALTER TABLE SanPhamYeuThich ADD FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang);
ALTER TABLE SanPhamYeuThich ADD FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham);
GO

-- Khóa ngoại cho bảng Giỏ hàng
ALTER TABLE GioHang ADD FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang);
ALTER TABLE GioHang ADD FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham);
GO
-- ChiTietDonDatHang để chỉ liên kết với SanPham và KhachHang
ALTER TABLE ChiTietDonDatHang ADD FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham);
ALTER TABLE ChiTietDonDatHang ADD FOREIGN KEY (MaKhachHang) REFERENCES KhachHang(MaKhachHang);
GO

--********************************************************--
--						Dữ Liệu Mẫu						  --
--********************************************************--
go
-- Dữ liệu mẫu cho bảng Quản trị viên
INSERT INTO QuanTriVien (HoTen ,Email, MatKhau, Hinh, TinhTrang) VALUES
(N'Quản trị viên Một', N'admin1@123.com', N'thinh123', N'images/UserImages/avatar.jpg', N'Hoạt động'),
(N'Quản trị viên Hai', N'admin2@123.com', N'thinh123', N'images/UserImages/avatar.jpg', N'Hoạt động');

-- Dữ liệu mẫu cho bảng Khách viếng thăm
INSERT INTO KhachVienTham (Email, MatKhau, HoTen, SoDienThoai, DiaChi, TinhTrang) VALUES
(N'visitor1@123.com', N'visitorpass1', N'Khách Viếng Thăm Một', N'0123456789', N'789 Đường Khách', N'Hoạt động'),
(N'visitor2@123.com', N'visitorpass2', N'Khách Viếng Thăm Hai', N'9876543210', N'321 Đường Khách', N'Hoạt động');

-- Dữ liệu mẫu cho bảng Khách hàng
INSERT INTO KhachHangs (HoTen, Hinh, Email, PasswordHash, PhoneNumber, DiaChi, TinhTrang) VALUES
(N'Khách Hàng Một','',N'caothinh467@gmail.com', N'1231', N'0123456789', N'123 Đường Chính', N'Hoạt động'),
(N'Khách Hàng Hai','',N'customer2@123.com', N'1232', N'0987654321', N'456 Đường Phụ', N'Hoạt động');

-- Dữ liệu mẫu cho bảng Danh mục sản phẩm
-- Dữ liệu mẫu cho bảng Danh mục sản phẩm
INSERT INTO DanhMucSanPham (TenDanhMuc) VALUES
(N'Bánh Mì Kẹp'),
(N'Combo'),
(N'Gà Rán'),
(N'Rau Trộn'),
(N'Tráng Miệng'),
(N'Nước Uống');

-- Dữ liệu mẫu cho bảng Sản phẩm
INSERT INTO SanPham (TenSanPham, HinhAnh, MoTa, Gia, SoLuong, DanhMuc, NgayTao, NgayCapNhat) VALUES
(N'Bánh mì kẹp thịt', N'/images/banhmiKep/banhmi1.jpg', N'Bánh mì kẹp thịt bò thơm ngon, hấp dẫn.', 45.00, 50, 1, GETDATE(), GETDATE()),
(N'Combo Bánh mì và nước ngọt', N'/images/combo/combo8.jpg', N'Combo bánh mì kẹp và nước ngọt, tiết kiệm và ngon miệng.', 60.00, 100, 2, GETDATE(), GETDATE()),
(N'Gà rán giòn', N'/images/garan/garan3.jpg', N'Gà rán giòn, thơm phức, ăn là ghiền.', 120.00, 40, 3, GETDATE(), GETDATE()),
(N'Salad rau củ', N'/images/rautron/salad2.jpg', N'Salad rau củ tươi ngon, bổ dưỡng.', 35.00, 60, 4, GETDATE(), GETDATE()),
(N'Tiramisu', N'/images/trangmieng/trangmieng5.jpg', N'Tiramisu truyền thống, ngọt ngào và béo ngậy.', 50.00, 30, 5, GETDATE(), GETDATE()),
(N'Gà rán combo', N'/images/combo/combo5.jpg', N'Combo gà rán, khoai tây chiên và nước ngọt.', 150.00, 25, 2, GETDATE(), GETDATE()),
(N'Bánh Plan Dâu', N'/images/trangmieng/trangmieng8.jpg', N'Ngon khó cưỡng.', 25.00, 100, 5, GETDATE(), GETDATE()),
(N'Rau trái cây', N'/images/rautron/salad10.jpg', N'Chè trái cây tươi ngon, mát lạnh.', 20.00, 80, 4, GETDATE(), GETDATE()),
(N'Coca', N'/images/nuocuong/nuocuong1.jpg', N'Coca uống thả ga.', 20.00, 80, 6, GETDATE(), GETDATE()),
(N'Ép Cam', N'/images/nuocuong/nuocuong3.jpg', N'Cam Ép ngon tuyệt .', 25.00, 80, 6, GETDATE(), GETDATE());


