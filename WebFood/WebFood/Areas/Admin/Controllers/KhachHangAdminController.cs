using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using WebFood.Areas.Admin.fliter;
using WebFood.Data;
using WebFood.Models;
using WebFood.Service;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminSession] // kiểm tra session
    public class KhachHangAdminController : BaseController
    {
        private readonly ConnectStr _connectStr;
        private readonly SendMail _sendMail;
        private readonly UserManager<KhachHang> _userManager;
        private readonly KhachHangService _khachHangService;

        public KhachHangAdminController(ConnectStr connectStr, SendMail sendMail, UserManager<KhachHang> userManager, KhachHangService khachHangService)
        {
            _connectStr = connectStr;
            _sendMail = sendMail;
            _userManager = userManager;
            _khachHangService = khachHangService;
        }


        #region Actions
        [HttpGet]
        public async Task<IActionResult> Index(bool? loc = null, string? email = null)
        {
            ViewBag.Loc = loc ?? false;
            if (loc == true)
            {
                var khachHangsTongTien = await _khachHangService.GetCustomersWithOrdersAbove100Async(email);
                return View(khachHangsTongTien);
            }
            else
            {
                var khachHangs = await _khachHangService.GetAllKhachHangsAsync(email);
                return View(khachHangs);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string Id, bool showRoles = false)
        {
            // Gọi API để lấy thông tin khách hàng (trả về `KhachHangDetailsViewModel`)
            var khachHangDetails = await _khachHangService.GetKhachHangByIdAsync(Id);

            if (khachHangDetails == null)
            {
                return NotFound("Không tìm thấy khách hàng.");
            }
            ViewBag.ShowRoles = showRoles;

            return View(khachHangDetails);
        }


        #endregion

        #region UpdateRoles
        [HttpPost]
        public async Task<IActionResult> UpdateRoles(string Id, List<string> roles)
        {
            var success = await _khachHangService.UpdateRolesAsync(Id, roles);
            if (!success)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật vai trò.");
                return RedirectToAction("Details", new { Id });
            }

            return RedirectToAction("Details", new { Id });
        }

        #endregion

        #region xoa chi tiet don hang

        [HttpPost]
        public async Task<IActionResult> DeleteAllOrderDetails(string customerId)
        {
            var success = await _khachHangService.DeleteAllOrderDetailsAsync(customerId);

            if (!success)
            {
                TempData["Message"] = "Không thể xóa chi tiết đơn hàng của khách hàng.";
                return RedirectToAction("Details", new { Id = customerId });
            }

            TempData["Message"] = "Đã xóa tất cả chi tiết đơn hàng của khách hàng thành công.";
            return RedirectToAction("Details", new { Id = customerId });
        }


        #endregion

        #region xoa khach hang vinh vien
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _khachHangService.DeleteKhachHangAsync(id);
            if (!success)
            {
                TempData["Message"] = "Không thể xóa khách hàng vì có đơn hàng liên quan.";
                return RedirectToAction("Details", new { Id = id });
            }

            TempData["Message"] = "Xóa khách hàng thành công.";
            return RedirectToAction("Index", "KhachHangAdmin");
        }


        #endregion

        #region Gửi voucher
        [HttpGet]
        public async Task<IActionResult> TangVoucher(string email)
        {
            // Gọi API để lấy thông tin khách hàng
            var khachHang = await _khachHangService.GetCustomerByEmailAsync(email);
            if (khachHang == null)
            {
                return NotFound("Email không tồn tại.");
            }

            ViewBag.KhEmail = khachHang;
            ViewBag.VoucherListAll = Enumerable.Range(1, 99).ToList(); // Danh sách voucher từ 1% đến 99%

            return View(khachHang);
        }

        [HttpPost]
        public async Task<IActionResult> TangVoucher(string email, int selectedVoucher)
        {
            // Kiểm tra email có tồn tại không
            var khachHang = await _khachHangService.GetCustomerByEmailAsync(email);
            if (khachHang == null)
            {
                ViewData["ErrorMessage"] = "Email không tồn tại.";
                return View("QuenMatKhau");
            }

            // Tạo mã voucher ngẫu nhiên (6 số từ 1 - 99)
            Random random = new Random();
            int[] MaNgauNhien = Enumerable.Range(0, 6).Select(_ => random.Next(1, 100)).ToArray();
            string maVoucher = string.Join("-", MaNgauNhien);

            // Gửi API để tặng voucher
            bool result = await _khachHangService.SendVoucherAsync(email, selectedVoucher);
            if (!result)
            {
                ViewData["ErrorMessage"] = "Gửi voucher thất bại!";
                return View();
            }

            // Gửi email cho khách hàng
            string subject = "Bạn đã nhận được ưu đãi đặc biệt của WebFood Quốc Thịnh";
            string body = $"Bạn nhận được Voucher giảm {selectedVoucher}% cho lần tham gia dùng dịch vụ tiếp theo. Mã Giảm của bạn là: {maVoucher}";

            _sendMail.SendEmail(khachHang.Email, subject, body);

            TempData["SuccessMessage"] = "Voucher đã được gửi thành công!";
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}
