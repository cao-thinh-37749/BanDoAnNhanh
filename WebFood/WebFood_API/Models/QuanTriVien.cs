using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebFood_API.Models
{

    public class KhachHang : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = "Cao Thinh";

        public string Hinh { get; set; }="~/images/UserImages/avatar.jpg";

        public string DiaChi { get; set; } = "100 binh thoi";

        [Required]
        public string TinhTrang { get; set; } = "Hoạt động";
    }


    public class DanhMucSanPham
    {
        [Key]
        public int MaDanhMuc { get; set; }        // Khóa chính

        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        public string TenDanhMuc { get; set; } = string.Empty;    // Tên danh mục
    }

    public class SanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSanPham { get; set; }         // Khóa chính

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        public string TenSanPham { get; set; } = string.Empty;   // Tên sản phẩm

        [Required(ErrorMessage = "Hình ảnh là bắt buộc.")]
        public string HinhAnh { get; set; } = string.Empty;      // Đường dẫn hình ảnh

        [NotMapped]
        public IFormFile? ImageFile { get; set; } // Tệp ảnh upload (Không lưu vào DB)

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [MaxLength(400 , ErrorMessage ="Mô tả không được dài quá 400 ký tự")]
        public string MoTa { get; set; } = string.Empty;        // Mô tả sản phẩm (không bắt buộc)

        [Required(ErrorMessage = "Giá là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ.")]
        public decimal Gia { get; set; }          // Giá sản phẩm

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ.")]
        public int SoLuong { get; set; }          // Số lượng còn

        [Required(ErrorMessage = "Danh mục là bắt buộc.")]
        public int DanhMuc { get; set; }          // ID danh mục sản phẩm
        public DateTime NgayTao { get; set; } = DateTime.Now;   // Ngày tạo
        public DateTime NgayCapNhat { get; set; } = DateTime.Now; // Ngày cập nhật

        // Khóa ngoại liên kết đến DanhMucSanPham
        [ForeignKey("DanhMuc")]
        public virtual DanhMucSanPham? DanhMucSanPham { get; set; }
    }

    public class SanPhamYeuThich
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaYeuThich { get; set; } // Khóa chính

        [Required(ErrorMessage = "Khách hàng là bắt buộc.")]
        public string Id{ get; set; } // ID khách hàng

        [Required(ErrorMessage = "Sản phẩm là bắt buộc.")]
        public int MaSanPham { get; set; } // ID sản phẩm

		[ForeignKey("Id")]
		public virtual KhachHang? KhachHang { get; set; } // Điều hướng đến KhachHang

		[ForeignKey("MaSanPham")]
		public virtual SanPham? SanPham { get; set; } // Điều hướng đến SanPham
	}

    public class GioHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGioHang { get; set; }        // Khóa chính

        [Required(ErrorMessage = "Khách hàng là bắt buộc.")]
        public string Id{ get; set; }      // Khóa ngoại, liên kết đến khách hàng

        [Required(ErrorMessage = "Sản phẩm là bắt buộc.")]
        public int MaSanPham { get; set; }        // Khóa ngoại, liên kết đến sản phẩm

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }          // Số lượng sản phẩm trong giỏ

        [Required(ErrorMessage = "Tổng tiền là bắt buộc.")]
        public decimal TongTien { get; set; }     // Tổng tiền sản phẩm

		[ForeignKey("Id")]
		public virtual KhachHang? KhachHang { get; set; } // Điều hướng đến KhachHang

		[ForeignKey("MaSanPham")]
		public virtual SanPham? SanPham{ get; set; } // Điều hướng đến SanPham
	}

    public class ChiTietDonDatHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTiet { get; set; }        // Khóa chính

        [Required(ErrorMessage = "Đơn đặt hàng là bắt buộc.")]
        public string Id{ get; set; }     // Khóa ngoại, liên kết đến đơn đặt hàng

        [Required(ErrorMessage = "Sản phẩm là bắt buộc.")]
        public int MaSanPham { get; set; }        // Khóa ngoại, liên kết đến sản phẩm

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }          // Số lượng sản phẩm đặt

        [Required(ErrorMessage = "Giá là bắt buộc.")]
        public decimal Gia { get; set; }          // Giá sản phẩm

        [Required(ErrorMessage = "Ngày giao hàng là bắt buộc.")]
        public DateTime? NgayGiao { get; set; } = DateTime.Now;     // Ngày giao

        [Required(ErrorMessage = "Ngày nhan hàng là bắt buộc.")]
        public DateTime? NgayNhan { get; set; } = DateTime.Now;     //Ngay nhan

        [Required(ErrorMessage = "Ngày thanh toan hàng là bắt buộc.")]
        public DateTime? NgayThanhToan { get; set; } = DateTime.Now;     // Ngày đặt hàng

        public string TrangThai { get; set; } = "Đang xử lý";

        [ForeignKey("Id")]
        public virtual KhachHang? KhachHang { get; set; } // Điều hướng đến KhachHang

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; } // Điều hướng đến SanPham
    }

    //model chứa thông tin sản phẩm
    public class CartItemViewModel
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal Gia { get; set; }
        public decimal TongTien { get; set; }
        public string Id{ get; set; }
    }

    // model hỗ trợ lọc khách hàng
    public class KhachHangTongTien
    {
        public KhachHang? KhachHang { get; set; }

        public decimal TongTien { get; set; }
    }

    // model hỗ trợ chi tiết khác hàng và chi tiết đơn hàng
    public class KhachHangDetailsViewModel
    {
        public KhachHang? KhachHang { get; set; } // Thông tin khách hàng
        public List<ChiTietDonDatHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonDatHang>(); // Danh sách chi tiết đơn hàng
        public List<string> Roles { get; set; } = new List<string>(); // Thêm danh sách vai trò
        public List<string> AllRoles { get; set; } = new(); // Tất cả vai trò có sẵn
    }

}

