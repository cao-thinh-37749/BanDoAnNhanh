using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebFood.Areas.Admin.fliter;
using WebFood.Controllers;
using WebFood.Data;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminSession]
	public class HomeAdminController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ConnectStr _connectStr;

        public HomeAdminController(ILogger<HomeController> logger, ConnectStr connectStr)
        {
            _logger = logger;
            _connectStr = connectStr;
        }

        public IActionResult Index()
        {
            // Tính ngày bắt đầu và ngày kết thúc của tuần hiện tại và tuần trước
            var endOfCurrentWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek).Date.AddDays(6);
            var startOfCurrentWeek = endOfCurrentWeek.AddDays(-6);

            var endOfLastWeek = startOfCurrentWeek.AddDays(-1);
            var startOfLastWeek = endOfLastWeek.AddDays(-6);

            // Số lượng sản phẩm còn hàng và hết hàng trong tuần hiện tại
            var tongSoLuongSanPham = _connectStr.SanPham.Where(tt => tt.SoLuong > 0).Count();
            var tongSoLuongSanPhamHetHang = _connectStr.SanPham.Where(tt => tt.SoLuong == 0).Count();

            // Số lượng đơn hàng trong tuần hiện tại và tuần trước
            var tongDonHangTrongTuan = _connectStr.ChiTietDonDatHang
                .Where(dh => dh.NgayThanhToan >= startOfCurrentWeek && dh.NgayThanhToan <= endOfCurrentWeek)
                .Count();

            var tongDonHangTrongTuanTruoc = _connectStr.ChiTietDonDatHang
                .Where(dh => dh.NgayThanhToan >= startOfLastWeek && dh.NgayThanhToan <= endOfLastWeek)
                .Count();

            // Doanh thu trong tuần hiện tại và tuần trước
            var doanhThuTrongTuan = _connectStr.ChiTietDonDatHang
                .Where(dh => dh.NgayThanhToan >= startOfCurrentWeek && dh.NgayThanhToan <= endOfCurrentWeek)
                .Sum(dh => dh.Gia);

            var doanhThuTrongTuanTruoc = _connectStr.ChiTietDonDatHang
                .Where(dh => dh.NgayThanhToan >= startOfLastWeek && dh.NgayThanhToan <= endOfLastWeek)
                .Sum(dh => dh.Gia);

            // Gán dữ liệu vào ViewBag để hiển thị trên View
            ViewBag.TongSoLuongSanPham = tongSoLuongSanPham;
            ViewBag.TongSoLuongSanPhamHetHang = tongSoLuongSanPhamHetHang;
            ViewBag.TongDonHangTrongTuan = tongDonHangTrongTuan;
            ViewBag.TongDonHangTrongTuanTruoc = tongDonHangTrongTuanTruoc;
            ViewBag.DoanhThuTrongTuan = doanhThuTrongTuan;
            ViewBag.DoanhThuTrongTuanTruoc = doanhThuTrongTuanTruoc;
            ViewBag.StartOfWeek = startOfCurrentWeek.ToString("dd/MM/yyyy");
            ViewBag.EndOfWeek = endOfCurrentWeek.ToString("dd/MM/yyyy");
            ViewBag.StartOfLastWeek = startOfLastWeek.ToString("dd/MM/yyyy");
            ViewBag.EndOfLastWeek = endOfLastWeek.ToString("dd/MM/yyyy");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
