using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebFood.Areas.Admin.fliter;
using WebFood.Data;
using WebFood.Models;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]

	public class LoginAdminController : BaseController
	{
        private readonly ConnectStr _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly SendMail _sendMail;
        private readonly PasswordService _passwordService;
        public LoginAdminController(ConnectStr context, IWebHostEnvironment environment, SendMail sendMail, PasswordService passwordService)
        {
            _context = context;
            _webHostEnvironment = environment;
            _sendMail = sendMail;
            _passwordService = passwordService;
        }

        #region Kiểm tra đăng nhập và thông tin
        // Kiểm tra quản trị viên đã đăng nhập
        private bool IsAdminLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("adminEmail"));

        // Kiểm tra định dạng email hợp lệ
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Đăng nhập và đăng xuất
        [HttpGet]
        public IActionResult LoginAd() => View();

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult LoginAd(string email, string PasswordHash)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(PasswordHash))
            {
                ModelState.AddModelError("", "Email và mật khẩu không được để trống.");
                return View();
            }

            var khachHang = _context.KhachHang.FirstOrDefault(k => k.Email == email);

            if (khachHang == null || !IdentityPasswordHelper.VerifyPassword(khachHang.PasswordHash, PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View();
            }
            return View();
        }

        public IActionResult LogoutAd()
        {
            ClearSession();
            return RedirectToAction("Login", "Login", new { area = "" });
        }
        #endregion

        #region Chỉnh sửa thông tin quản trị viên
        [HttpGet]
        public IActionResult Edit(string emailAdmin)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("LoginAd");

            var admin = _context.KhachHang.FirstOrDefault(ad => ad.Email == emailAdmin);
            if (admin == null) return RedirectToAction("LoginAd");

            ViewBag.Email = HttpContext.Session.GetString("adminEmail");
            ViewBag.Hinh = HttpContext.Session.GetString("adminImage");
            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KhachHang quanTriVien, IFormFile imageFile)
        {
            // Kiểm tra dữ liệu nhập vào
            if (!ValidateAdminData(quanTriVien))
            {
                return View(quanTriVien);
            }

            var adminInDb = _context.KhachHang.FirstOrDefault(id => id.Id == quanTriVien.Id);
            if (adminInDb == null) return View(quanTriVien);

            // Kiểm tra và chỉ cập nhật khi có thay đổi
            bool isChanged = false;

            if (adminInDb.HoTen != quanTriVien.HoTen)
            {
                adminInDb.HoTen = quanTriVien.HoTen;
                isChanged = true;
            }

            if (adminInDb.Email != quanTriVien.Email)
            {
                adminInDb.Email = quanTriVien.Email;
                isChanged = true;
            }

            if (adminInDb.PasswordHash != quanTriVien.PasswordHash)
            {
                adminInDb.PasswordHash = quanTriVien.PasswordHash;
                isChanged = true;
            }

            if (adminInDb.TinhTrang != quanTriVien.TinhTrang)
            {
                adminInDb.TinhTrang = quanTriVien.TinhTrang;
                isChanged = true;
            }

            // Kiểm tra ảnh có thay đổi không
            if (imageFile != null && imageFile.Length > 0)
            {
                adminInDb.Hinh = await SaveNewImageAndDeleteOld(adminInDb.Hinh, imageFile);
                isChanged = true;
            }

            // Nếu có thay đổi, thực hiện cập nhật vào DB
            if (isChanged)
            {
                _context.KhachHang.Update(adminInDb);
                await _context.SaveChangesAsync();
                UpdateSession(adminInDb);
            }

            return RedirectToAction("Index", "HomeAdmin", new { areas = "Admin" });
        }
        #endregion

        #region  Hàm kiểm tra dữ liệu nhập vào của quản trị viên
        // Hàm kiểm tra dữ liệu nhập vào của quản trị viên
        private bool ValidateAdminData(KhachHang quanTriVien)
        {
            // Kiểm tra họ tên
            if (string.IsNullOrEmpty(quanTriVien.HoTen))
            {
                ModelState.AddModelError("HoTen", "Họ tên không được để trống.");
                return false;
            }

            // Kiểm tra email
            if (string.IsNullOrEmpty(quanTriVien.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống.");
            }
            else if (!IsValidEmail(quanTriVien.Email))
            {
                ModelState.AddModelError("Email", "Email không hợp lệ.");
            }

            // Kiểm tra mật khẩu
            if (string.IsNullOrEmpty(quanTriVien.PasswordHash))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống.");
                return false;
            }

            // Kiểm tra trạng thái
            if (quanTriVien.TinhTrang == null)
            {
                ModelState.AddModelError("TinhTrang", "Trạng thái không được để trống.");
                return false;
            }

            return true;
        }
        #endregion

        #region Lưu ảnh và cập nhật session
        // Lưu ảnh mới và xóa ảnh cũ
        private async Task<string> SaveNewImageAndDeleteOld(string oldImage, IFormFile imageFile)
        {
            try
            {
                var directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "UserImages");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var imagePath = Path.Combine(directoryPath, uniqueFileName);

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(oldImage))
                {
                    var oldFilePath = Path.Combine(directoryPath, oldImage);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                return "/images/UserImages/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu ảnh: " + ex.Message);
                return oldImage;
            }
        }

        // Cập nhật session
        private void UpdateSession(KhachHang admin)
        {
            HttpContext.Session.SetString("adminEmail", admin.Email ?? "");
            HttpContext.Session.SetString("adminImage", admin.Hinh ?? "");
        }

        // Xóa session
        private void ClearSession()
        {
            HttpContext.Session.Remove("adminEmail");
            HttpContext.Session.Remove("adminImage");
        }
        #endregion

        #region Quên mật khẩu
        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View(); // Trả về view chứa form nhập email
        }

        [HttpPost]
        public IActionResult SendPasswordResetEmail(string email)
        {
            // Kiểm tra nếu email tồn tại trong cơ sở dữ liệu
            var admin = _context.KhachHang.FirstOrDefault(ad => ad.Email == email);
            if (admin == null)
            {
                ViewData["ErrorMessage"] = "Email không tồn tại.";
                return View("QuenMatKhau");
            }

            // Tạo mật khẩu mới
            var newPassword = "123456789";

            // Cập nhật mật khẩu mới vào cơ sở dữ liệu
            admin.PasswordHash = newPassword;
            _context.KhachHang.Update(admin);
            _context.SaveChanges();

            // Gửi email với mật khẩu mới
            string subject = "Mật khẩu mới của bạn";
            string body = $"Mật khẩu mới của bạn là: {newPassword}";

            _sendMail.SendEmail(admin.Email, subject, body);

            return RedirectToAction("LoginAd","LoginAdmin", new {area = "Admin"});
        }
        #endregion

        #region Đổi mật khẩu
        public IActionResult ChangePassword(string email)
        {
            var userEmail = HttpContext.Session.GetString("adminEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Login", new { area = "" });

            var khachHang = _context.KhachHang.FirstOrDefault(k => k.Email == email);
            if (khachHang == null)
                return NotFound();

            SetViewBag(userEmail);
            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var userEmail = HttpContext.Session.GetString("adminEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Login", new { area = "" });

            var user = _context.KhachHang.FirstOrDefault(u => u.Email == userEmail);
            if (!_passwordService.ValidatePassword(OldPassword, NewPassword, ConfirmPassword, user, ModelState))
            {
                SetViewBag(userEmail);
                return View();
            }

            _passwordService.UpdatePassword(user!, NewPassword);
            return RedirectToAction("Login", "Login", new { area = "" });
        }

        private void SetViewBag(string userEmail)
        {
            ViewBag.UserEmail = userEmail;
            ViewBag.UserImage = HttpContext.Session.GetString("adminImage");
        }
        #endregion
    }
}
