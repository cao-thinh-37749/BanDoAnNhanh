using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood.Data;
using WebFood.Models;

namespace WebFood.Controllers
{
    public class SanPhamYeuThichController : Controller
    {
        private readonly ConnectStr connectStr;
        private readonly GetCount _getCount;

        public SanPhamYeuThichController(ConnectStr context)
        {
            connectStr = context;
            _getCount = new GetCount(context);
        }

        // Kiểm tra session và lấy khách hàng
        private KhachHang? GetCustomerEmail()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.UserEmail = userEmail;
            return string.IsNullOrEmpty(userEmail) ? null : connectStr.KhachHang.FirstOrDefault(k => k.Email == userEmail);
        }

        // Kiểm tra xem người dùng đã đăng nhập chưa
        private bool IsUserLoggedIn() => GetCustomerEmail() != null;

        #region Index

        public IActionResult Index()
        {
            var customer = GetCustomerEmail();
            if (!IsUserLoggedIn() || customer == null)
                return RedirectToAction("Login", "Login");

            var favoriteProducts = connectStr.SanPhamYeuThich
                .Include(y => y.SanPham)
                .Where(y => y.Id == customer.Id)
                .Select(y => y.SanPham)
                .ToList();

            ViewBag.SoLuongYeuThich = favoriteProducts.Count();
            ViewBag.SoluongHang = _getCount.GetCartCount(customer.Email);
            return View(favoriteProducts);
        }

        #endregion

        #region Add

        [HttpPost]
        public IActionResult Add(int maSanPham)
        {
            var customer = GetCustomerEmail();

            if (!IsUserLoggedIn() || customer == null)
                return RedirectToAction("Login", "Login");

            if (connectStr.SanPhamYeuThich.Any(y => y.Id == customer.Id && y.MaSanPham == maSanPham))
            {
                TempData["Message"] = "Sản phẩm đã có trong danh sách yêu thích.";
                return RedirectToAction("Index", "Home");
            }

            connectStr.SanPhamYeuThich.Add(new SanPhamYeuThich
            {
                Id = customer.Id,
                MaSanPham = maSanPham
            });
            connectStr.SaveChanges();

            TempData["Message"] = "Thêm sản phẩm yêu thích thành công!";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Delete

        public IActionResult Delete(int maSanPham)
        {
            var customer = GetCustomerEmail();
            if (!IsUserLoggedIn() || customer == null)
                return RedirectToAction("Login", "Login");

            var deleteFavorite = connectStr.SanPhamYeuThich
                .FirstOrDefault(y => y.Id == customer.Id && y.MaSanPham == maSanPham);

            if (deleteFavorite == null)
                return BadRequest("Sản phẩm không tồn tại trong danh sách yêu thích.");

            connectStr.SanPhamYeuThich.Remove(deleteFavorite);
            connectStr.SaveChanges();
            TempData["Message"] = "Sản phẩm đã xóa trong danh sách yêu thích.";

            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}
