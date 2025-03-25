using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood.Data;
using WebFood.Models;

namespace WebFood.Controllers
{
    public class ChiTietDonDatHangController : Controller
    {
        private readonly ConnectStr connectStr;
        private readonly GetCount getCount;
        public ChiTietDonDatHangController (ConnectStr connectStr)
        {
            this.connectStr = connectStr;
            this.getCount = new GetCount(connectStr);
        }
        private bool IsUserLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));

        private KhachHang? GetCustomerEmail()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.MaCusTomer = HttpContext.Session.GetInt32("UserMaKhachHang");
            ViewBag.UserEmail = userEmail;
            return string.IsNullOrEmpty(userEmail) ? null : connectStr.KhachHang.FirstOrDefault(k => k.Email == userEmail);
        }

        public async Task<IActionResult> Index()
        {
            var khachHang = GetCustomerEmail();
            if (!IsUserLoggedIn() || khachHang == null)
                return RedirectToAction("Login", "Login");

            // Lấy danh sách chi tiết đơn hàng có trạng thái "Đang xử lý" hoặc "Đang giao"
            var chiTietDon = await connectStr.ChiTietDonDatHang.Include(g => g.SanPham)
                .Where(k => k.Id == khachHang.Id && (k.TrangThai == "Đang xử lý" || k.TrangThai == "Đang giao"))
                .ToListAsync();

            // Cập nhật trạng thái đơn hàng
            await CapNhatTrangThaiDonHang(chiTietDon);

            // Lấy số lượng sản phẩm trong giỏ hàng và lưu vào ViewBag
            ViewBag.SoluongHang = getCount.GetCartCount(khachHang.Email);
            ViewBag.SoLuongYeuThich = getCount.GetFavoriteProductCount(khachHang.Email);
            return View(chiTietDon);
        }

        //cập nhật trạng thái chi tiết
        private async Task CapNhatTrangThaiDonHang(List<ChiTietDonDatHang> chiTietDon)
        {
            foreach (var chiTiet in chiTietDon)
            {
                if (chiTiet.NgayThanhToan.HasValue)
                {
                    var DangGiao = (DateTime.Now - chiTiet.NgayThanhToan.Value).TotalMinutes;
                    var DaGiao = (DateTime.Now - chiTiet.NgayGiao!.Value).TotalMinutes;
                    if (chiTiet.TrangThai == "Đang xử lý" && DangGiao >= 1)
                    {
                        chiTiet.TrangThai = "Đang giao";
                        chiTiet.NgayGiao = chiTiet.NgayGiao.Value.AddMinutes(1);
                        await connectStr.SaveChangesAsync(); 
                    }
                    else if (chiTiet.TrangThai == "Đang giao" && DaGiao >= 1)
                    {
                        chiTiet.TrangThai = "Đã giao";
                        chiTiet.NgayNhan = DateTime.Now;
						TempData["Message"] = "Đơn hàng đã giao thành công.";
						await connectStr.SaveChangesAsync(); 
                    }
                }
            }
            await connectStr.SaveChangesAsync(); // Lưu tất cả thay đổi sau khi cập nhật trạng thái
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhan(int maChiTiet)
        {
            var chiTiet = await connectStr.ChiTietDonDatHang.FindAsync(maChiTiet);

            // Kiểm tra nếu chi tiết đơn hàng không tồn tại
            if (chiTiet == null)
                return NotFound();

            // Chỉ khi trạng thái là "Đang giao" thì mới có thể bấm xác nhận để chuyển thành "Đã giao"
            if (chiTiet.TrangThai == "Đang giao")
            {
                chiTiet.TrangThai = "Đã giao";
                chiTiet.NgayNhan = DateTime.Now;
                chiTiet.NgayThanhToan = DateTime.Now;
                await connectStr.SaveChangesAsync(); // Cập nhật trạng thái "Đã giao" vào cơ sở dữ liệu
            }

            // Thông báo và chuyển hướng về trang chủ
            TempData["Message"] = "Đơn hàng đã giao thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> LichSuDon()
        {
			var khachHang = GetCustomerEmail();
			if (!IsUserLoggedIn() || khachHang == null)
				return RedirectToAction("Login", "Login");

            //Lọc đơn hàng với điều kiện đã giao trong vòng 1 ngày
            var lichSuDon = await connectStr.ChiTietDonDatHang.Include(g => g.SanPham)
                .Where(k => k.Id == khachHang.Id && (k.TrangThai == "Đã giao") && k.NgayNhan >= DateTime.Now.AddDays(-7)).ToListAsync();

			ViewBag.SoluongHang = getCount.GetCartCount(khachHang.Email);
			ViewBag.SoLuongYeuThich = getCount.GetFavoriteProductCount(khachHang.Email);

            return View(lichSuDon);
		}
    }
}
