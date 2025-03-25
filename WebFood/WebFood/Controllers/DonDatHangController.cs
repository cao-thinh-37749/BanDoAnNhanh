using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebFood.Data;
using WebFood.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebFood.Controllers
{
    public class DonDatHangController : Controller
    {
        private readonly ConnectStr _connectStr;
        private readonly GetCount _getCount;

        public DonDatHangController(ConnectStr connectStr)
        {
            _connectStr = connectStr;
            _getCount = new GetCount(connectStr);
        }

        #region Kiểm Tra và Lấy Thông Tin Người Dùng

        // Kiểm tra nếu người dùng đã đăng nhập
        private bool KiemTraDangNhap() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail"));

        // Lấy thông tin khách hàng từ email trong session
        private KhachHang? LayKhachHangTuEmail()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.MaCusTomer = HttpContext.Session.GetInt32("UserId");
            ViewBag.UserEmail = userEmail;
            return string.IsNullOrEmpty(userEmail) ? null : _connectStr.KhachHang.FirstOrDefault(k => k.Email == userEmail);
        }

        #endregion

        #region Lấy Danh Sách Đơn Hàng

        // Lấy danh sách các sản phẩm từ giỏ hàng để thanh toán
        private List<CartItemViewModel> LayDanhSachDonHang(string maKhachHang, int? maSanPham = null)
        {
            var query = _connectStr.GioHang.Where(g => g.Id == maKhachHang);

            if (maSanPham.HasValue)
                query = query.Where(g => g.MaSanPham == maSanPham.Value);

            return query
                .Join(_connectStr.SanPham, g => g.MaSanPham, sp => sp.MaSanPham, (g, sp) => new CartItemViewModel
                {
                    MaSanPham = g.MaSanPham,
                    TenSanPham = sp.TenSanPham,
                    HinhAnh = sp.HinhAnh,
                    SoLuong = g.SoLuong,
                    Gia = sp.Gia,
                    TongTien = g.SoLuong * sp.Gia,
                    Id = g.Id
                }).ToList();
        }

        #endregion

        #region Xử Lý Đơn Hàng

        // Kiểm tra nếu sản phẩm đã có trong ChiTietDonDatHang và xử lý việc thêm mới hoặc cập nhật
        private async Task XuLyChiTietDonDatHang(int maSanPham, string maKhachHang, int soLuong, decimal tongTien)
        {
            // Lấy chi tiết sản phẩm
            var sanPham = _connectStr.SanPham.FirstOrDefault(sp => sp.MaSanPham == maSanPham);
            if (sanPham == null)
            {
                TempData["Message"] = "Sản phẩm không tồn tại.";
                return;
            }

            // Kiểm tra số lượng tồn kho
            if (sanPham.SoLuong <= 0)
            {
                TempData["Message"] = "Số lượng sản phẩm trong kho không đủ.";
                return;
            }

            // Trừ số lượng tồn kho
            sanPham.SoLuong -= soLuong;

            // Lưu cập nhật vào database
            _connectStr.SanPham.Update(sanPham);

            var chiTietCu = _connectStr.ChiTietDonDatHang
                .Where(c => c.Id == maKhachHang && c.MaSanPham == maSanPham)
                .OrderByDescending(c => c.MaChiTiet)
                .FirstOrDefault();

            if (chiTietCu != null && chiTietCu.TrangThai == "Đang xử lý")
            {
                // Cập nhật sản phẩm trong đơn hàng
                chiTietCu.SoLuong += soLuong;
                chiTietCu.Gia = chiTietCu.SoLuong * tongTien;
                _connectStr.ChiTietDonDatHang.Update(chiTietCu);
                TempData["Message"] = "Cập nhật số lượng sản phẩm thành công. Đang xử lý";
            }
            else
            {
                // Tạo chi tiết đơn hàng mới
                var chiTietMoi = new ChiTietDonDatHang
                {
                    Id = maKhachHang,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    Gia = tongTien,
                    NgayThanhToan = DateTime.Now,
                    TrangThai = "Đang xử lý"
                };
                _connectStr.ChiTietDonDatHang.Add(chiTietMoi);
                TempData["Message"] = "Thanh toán đơn hàng thành công. Đang xử lý! ";
            }

            await _connectStr.SaveChangesAsync();
        }

        // Xóa sản phẩm đã thanh toán khỏi giỏ hàng
        private async Task XoaSanPhamKhoiGioHang(int maSanPham, string maKhachHang)
        {
            var gioHangItem = _connectStr.GioHang.FirstOrDefault(g => g.Id == maKhachHang && g.MaSanPham == maSanPham);
            if (gioHangItem != null)
            {
                _connectStr.GioHang.Remove(gioHangItem);
                await _connectStr.SaveChangesAsync();
            }
        }

        #endregion

        #region Thanh Toán Sản Phẩm

        [HttpGet]
        public IActionResult ThanhToanMotSanPham(int maSanPham)
        {
            var customer = LayKhachHangTuEmail();
            if (!KiemTraDangNhap() || customer == null)
                return RedirectToAction("Login", "Login");

            var orderDetails = LayDanhSachDonHang(customer.Id, maSanPham);
            if (!orderDetails.Any())
            {
                TempData["Message"] = "Không có sản phẩm để thanh toán.";
                return RedirectToAction("Index", "GioHang");
            }

            ViewBag.Id = customer.Id;
            ViewBag.SoluongHang = _getCount.GetCartCount(customer.Email);
            ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(customer.Email);
            return View(orderDetails);
        }

        [HttpGet]
        public IActionResult ThanhToanTatCaSanPham()
        {
            var customer = LayKhachHangTuEmail();
            if (!KiemTraDangNhap() || customer == null)
                return RedirectToAction("Login", "Login");

            var orderDetails = LayDanhSachDonHang(customer.Id);
            if (!orderDetails.Any())
            {
                TempData["Message"] = "Giỏ hàng của bạn hiện tại không có sản phẩm để thanh toán.";
                return RedirectToAction("Index", "GioHang");
            }

            ViewBag.Id = customer.Id;
            ViewBag.SoluongHang = _getCount.GetCartCount(customer.Email);
            ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(customer.Email);
            return View(orderDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanMotSanPhamPost(int maSanPham, string Id)
        {
            var customer = LayKhachHangTuEmail();
            if (!KiemTraDangNhap() || customer == null)
            {
                return RedirectToAction("Login", "LoginAdmin");
            }

            var gioHangItem = _connectStr.GioHang.FirstOrDefault(g => g.Id == Id && g.MaSanPham == maSanPham);
            if (gioHangItem == null)
            {
                TempData["Message"] = "Không có sản phẩm này trong giỏ hàng.";
                return RedirectToAction("Index", "GioHang");
            }

            await XuLyChiTietDonDatHang(maSanPham, Id, gioHangItem.SoLuong, gioHangItem.TongTien);
            await XoaSanPhamKhoiGioHang(maSanPham, Id);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToanTatCaSanPhamPost(string Id)
        {
            var customer = LayKhachHangTuEmail();
            if (!KiemTraDangNhap() || customer == null)
            {
                return RedirectToAction("Login", "LoginAdmin");
            }

            var gioHangItems = _connectStr.GioHang.Where(g => g.Id == Id).ToList();
            if (!gioHangItems.Any())
            {
                TempData["Message"] = "Giỏ hàng của bạn hiện tại không có sản phẩm.";
                return RedirectToAction("Index", "GioHang");
            }

            foreach (var item in gioHangItems)
            {
                await XuLyChiTietDonDatHang(item.MaSanPham, item.Id, item.SoLuong, item.TongTien);
            }

            // Xóa tất cả sản phẩm trong giỏ hàng sau khi thanh toán
            _connectStr.GioHang.RemoveRange(gioHangItems);
            await _connectStr.SaveChangesAsync();

            TempData["Message"] = "Thanh toán giỏ hàng thành công!";
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
