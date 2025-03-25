using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood.Areas.Admin.fliter;
using WebFood.Data;
using WebFood.Models;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminSession] // kiểm tra session
    public class DonHangAdminController : BaseController //lấy session 
    {
        private readonly ConnectStr _connectStr;

        public DonHangAdminController(ConnectStr connectStr)
        {
            _connectStr = connectStr;
        }

        private IQueryable<ChiTietDonDatHang> LocDonHang(string? tenKhach, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            var query = _connectStr.ChiTietDonDatHang
                .Include(dh => dh.KhachHang)
                .Include(dh => dh.SanPham)
                .Where(dh => dh.TrangThai == "Đã giao" &&
                    (string.IsNullOrEmpty(tenKhach) || dh.KhachHang!.HoTen.ToUpper().Contains(tenKhach.ToUpper())));

            if (ngayBatDau.HasValue)
            {
                query = query.Where(dh => dh.NgayThanhToan >= ngayBatDau.Value.Date);
            }

            if (ngayKetThuc.HasValue)
            {
                query = query.Where(dh => dh.NgayThanhToan <= ngayKetThuc.Value.Date);
            }

            return query;
        }


        public IActionResult Index(string? TenKhach, DateTime? NgayBatDau, DateTime? NgayKetThuc, string sort)
        {
            var donHangDaLoc = LocDonHang(TenKhach, NgayBatDau, NgayKetThuc).ToList();

            // Sắp xếp đơn hàng theo giá
            if (sort == "asc")
            {
                donHangDaLoc = donHangDaLoc.OrderBy(dh => dh.SoLuong * dh.Gia).ToList(); // Giá tăng dần
            }
            else if (sort == "desc")
            {
                donHangDaLoc = donHangDaLoc.OrderByDescending(dh => dh.SoLuong * dh.Gia).ToList(); // Giá giảm dần
            }

            var tongTien = donHangDaLoc.Sum(dh => dh.Gia);

            ViewData["TongTien"] = tongTien;
            ViewData["TenKhach"] = TenKhach;
            ViewData["NgayBatDau"] = NgayBatDau;
            ViewData["NgayKetThuc"] = NgayKetThuc;
            ViewData["SortList"] = sort;

            return View(donHangDaLoc);
        }



        public async Task<IActionResult> Details(int? MaChiTiet)
        {
            if (!MaChiTiet.HasValue)
                return NotFound();

            var donHang = await _connectStr.ChiTietDonDatHang
                .Include(dh => dh.KhachHang)
                .Include(dh => dh.SanPham)
                .FirstOrDefaultAsync(dh => dh.MaChiTiet == MaChiTiet);

            if (donHang == null)
                return NotFound();

            return View(donHang);
        }
    }
}
