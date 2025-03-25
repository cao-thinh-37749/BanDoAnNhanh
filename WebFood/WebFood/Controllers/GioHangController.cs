using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood.Data;
using WebFood.Models;

namespace WebFood.Controllers
{
    public class GioHangController : Controller
    {
        private readonly ConnectStr connectStr;
        private readonly GetCount _getCount;

        public GioHangController(ConnectStr connectStr)
        {
            this.connectStr = connectStr;
            this._getCount = new GetCount(connectStr);
        }

        // Kiểm tra nếu người dùng đã đăng nhập
        private bool IsUserLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));

        // Lấy thông tin khách hàng từ email trong session
        private KhachHang? GetCustomerEmail()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.UserEmail = userEmail;
            return string.IsNullOrEmpty(userEmail) ? null : connectStr.KhachHang.FirstOrDefault(k => k.Email == userEmail);
        }

        #region Index Action
        public IActionResult Index()
        {

            var customer = GetCustomerEmail();
            if (!IsUserLoggedIn()|| customer == null) return RedirectToAction("Login", "Login");

            var gioHang = connectStr.GioHang
                .Include(g => g.SanPham)
                .Where(k => k.Id == customer.Id)
                .ToList();

            // Tính tổng số lượng sản phẩm trong giỏ hàng
            ViewBag.SoluongHang = gioHang.Sum(g => g.SoLuong);
            ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(customer.Email);

            return View(gioHang); // Trả về giỏ hàng của khách hàng
        }
        #endregion

        #region Add Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int maSanPham)
        {
            if (!IsUserLoggedIn()) return RedirectToAction("Login", "Login");

            var result = await UpdateProductQuantityInCart(maSanPham, 1);
            if (!result)
            {
                TempData["Message"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Message"] = "Sản phẩm đã được thêm vào giỏ hàng.";
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Delete Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int maSanPham)
        {
            var gioHangItem = connectStr.GioHang.FirstOrDefault(g => g.MaSanPham == maSanPham);
            if (gioHangItem != null)
            {
                connectStr.GioHang.Remove(gioHangItem);
                TempData["Message"] = "Sản phẩm đã xóa khỏi giỏ hàng.";
                await connectStr.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Update Quantity Actions
        public async Task<IActionResult> TangSoLuong(int maSanPham) => await UpdateQuantity(maSanPham, 1);
        public async Task<IActionResult> GiamSoLuong(int maSanPham) => await UpdateQuantity(maSanPham, -1);

        private async Task<IActionResult> UpdateQuantity(int maSanPham, int quantityChange)
        {
            var result = await UpdateProductQuantityInCart(maSanPham, quantityChange);
            if (!result)
            {
                TempData["Message"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Hàm Methods
        private async Task<bool> UpdateProductQuantityInCart(int maSanPham, int SoLuong)
        {
            var customer = GetCustomerEmail();
            var sanPham = connectStr.SanPham.FirstOrDefault(sp => sp.MaSanPham == maSanPham);

            if (!IsUserLoggedIn() || customer == null || sanPham == null)
                return false;

            var gioHangItem = connectStr.GioHang.FirstOrDefault(g => g.Id == customer.Id && g.MaSanPham == maSanPham);

            if (gioHangItem != null)
            {
                gioHangItem.SoLuong += SoLuong;
                if (gioHangItem.SoLuong <= 0)
                    connectStr.GioHang.Remove(gioHangItem);
                else
                    gioHangItem.TongTien = gioHangItem.SoLuong * sanPham.Gia;
            }
            else if (SoLuong > 0)
            {
                var newGioHangItem = new GioHang
                {
                    Id = customer.Id,
                    MaSanPham = maSanPham,
                    SoLuong = SoLuong,
                    TongTien = sanPham.Gia * SoLuong
                };
                connectStr.GioHang.Add(newGioHangItem);
            }

            await connectStr.SaveChangesAsync();
            return true;
        }
        #endregion
    }
}
