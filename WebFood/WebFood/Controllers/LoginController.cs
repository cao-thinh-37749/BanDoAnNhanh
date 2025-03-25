using Microsoft.AspNetCore.Mvc;
using WebFood.Data;
using WebFood.Models;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace WebFood.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<KhachHang> _userManager; // Quản lý tài khoản người dùng
        private readonly SignInManager<KhachHang> _signInManager; // Quản lý đăng nhập, đăng xuất
        private readonly ConnectStr _connectStr;
        private readonly IPasswordHasher<KhachHang> _passwordHasher;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly GetCount _getCount;
        private readonly SendMail _sendMail;
        private readonly PasswordService _passwordService;

        public LoginController(UserManager<KhachHang> userManager, SignInManager<KhachHang> signInManager,ConnectStr connectStr, IWebHostEnvironment webHostEnvironment, SendMail sendMail, IPasswordHasher<KhachHang> passwordHasher, PasswordService passwordService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _connectStr = connectStr;
            _webHostEnvironment = webHostEnvironment;
            _getCount = new GetCount(connectStr);
            _sendMail = sendMail;
            _passwordHasher = passwordHasher;
            _passwordService = passwordService;
        }

        #region Đăng Nhập
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Login(string email, string PasswordHash)
        {
            var khachHang = _connectStr.KhachHang.FirstOrDefault(k => k.Email == email);

            if (khachHang == null || !IdentityPasswordHelper.VerifyPassword(khachHang.PasswordHash, PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View();
            }

            if (khachHang.TinhTrang != "Hoạt động")
            {
                ModelState.AddModelError("", "Email này đã ngừng hoạt động!");
                return View();
            }

            // Kiểm tra vai trò của người dùng
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("admin"))
                {
					UpdateSessionAdmin(user);
                    UpdateSessionUser(user);
                    // Trả về view login với modal lựa chọn giao diện
                    ViewBag.ShowAdminChoiceModal = true;
                    return View();
				}
                else if (roles.Count == 0) // Kiểm tra nếu không có vai trò nào
                {
                    ModelState.AddModelError("", "Lỗi hệ thống! ");
                    return View();
                }
                else
                {
					// Đăng nhập thành công
					UpdateSessionUser(user);
					HttpContext.Session.SetString("LoginMethodnormal", "normal"); // Đăng nhập bình thường

					return RedirectToAction("Index", "Home"); // Điều hướng khách hàng
                }
			}

			return View();
        }

		#region sesion
		private void UpdateSessionAdmin(KhachHang admin)
		{
			HttpContext.Session.SetString("adminEmail", admin.Email ?? "");
			HttpContext.Session.SetString("adminImage", admin.Hinh ?? "");
		}

		private void UpdateSessionUser(KhachHang user)
		{
			HttpContext.Session.SetString("UserMaKhachHang", user.Id);
			HttpContext.Session.SetString("UserEmail", user.Email);
			HttpContext.Session.SetString("UserImage", user.Hinh);
		}

		private void GetSessionUser()
		{
			HttpContext.Session.GetString("UserMaKhachHang");
			HttpContext.Session.GetString("UserEmail");
			HttpContext.Session.GetString("UserImage");
		}

        private void GetCrad(string email)
        {
            // Lấy số lượng sản phẩm trong giỏ hàng và lưu vào ViewBag
            ViewBag.SoluongHang = _getCount.GetCartCount(email);
            ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(email);
        }

		#endregion


		// Đường dẫn để đăng nhập qua Google
		public IActionResult LoginGoogle()
		{
			// Thêm returnUrl để đảm bảo Google trả về đúng trang sau khi xác thực
			var redirectUrl = Url.Action("GoogleResponse", "Login", null, Request.Scheme);
			var properties = new AuthenticationProperties
			{
				RedirectUri = redirectUrl
			};
			return Challenge(properties, GoogleDefaults.AuthenticationScheme);
		}


		// Đường dẫn để xử lý khi Google trả về dữ liệu
		public async Task<IActionResult> GoogleResponse()
		{
            var info = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (info?.Principal != null)
			{
				var name = info.Principal.Identity?.Name;
				var email = info.Principal.FindFirstValue(ClaimTypes.Email);

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
				{
					return NotFound();
				}

				HttpContext.Session.SetString("UserEmail", email);

				var user = _connectStr.KhachHang.FirstOrDefault(k => k.Email == email);

				if (user == null)
				{
					user = new KhachHang
					{
						Email = email,
						HoTen = name,
						Hinh = Url.Content("~/images/UserImages/avatar.jpg"),
						TinhTrang = "Hoạt động"
					};

					_connectStr.KhachHang.Add(user);
					await _connectStr.SaveChangesAsync();
				}

				HttpContext.Session.SetString("UserImage", user.Hinh);
				HttpContext.Session.SetString("LoginMethod", "google");

				return RedirectToAction("Index", "Home"); // Đảm bảo điều hướng đúng
			}

			return RedirectToAction("Login");
		}

		#endregion

		#region Đăng Xuất
		public async Task<IActionResult> Logout()
		{
			// Lấy phương thức đăng nhập từ Session
			var loginMethod = HttpContext.Session.GetString("LoginMethod") ?? TempData["LoginMethodnormal"]?.ToString(); ;

			// Đăng xuất khỏi hệ thống (xóa cookie)
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			// Xóa tất cả thông tin trong session
			HttpContext.Session.Clear();

			// Lưu phương thức đăng nhập vào TempData để sử dụng trong View
			TempData["LoginMethod"] = loginMethod;

			if (loginMethod == "google")
			{
				// Nếu đăng nhập qua Google, chuyển hướng đến trang đăng xuất của Google
				var googleLogoutUrl = "https://accounts.google.com/Logout";
				return Redirect(googleLogoutUrl);
			}

			// Nếu đăng nhập bình thường, chuyển hướng về trang chủ
			return RedirectToAction("Index", "Home");
		}
		#endregion

		#region Đăng Ký
		[HttpGet]
        public IActionResult Register() => View();


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(KhachHang khachHang, IFormFile imageFile)
        {
            if (!ValidateUserData(khachHang) || (imageFile != null && !IsValidImage(imageFile)))
                return View(khachHang);

            if (imageFile != null)
                khachHang.Hinh = await SaveUserImage(imageFile);

            // Tạo tài khoản mới trong bảng Identity
            var user = new KhachHang
            {
                HoTen = khachHang.HoTen,
                UserName = khachHang.Email,
                PhoneNumber = khachHang.PhoneNumber,
                Email = khachHang.Email
            };

            var createUser = await _userManager.CreateAsync(user, khachHang.PasswordHash);
            if (createUser.Succeeded)
            {
                // Gán quyền 'customer' cho tài khoản mới
                await _userManager.AddToRoleAsync(user, "customer");

                // Gửi mật khẩu về email
                string subject = "Chào mừng bạn đến với WebFood Quốc Thịnh";
                string body = $"Mật khẩu của bạn là: {khachHang.PasswordHash}";
                _sendMail.SendEmail(khachHang.Email, subject, body);

                return RedirectToAction("Login");
            }

            foreach (var error in createUser.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(khachHang);
        }

        #endregion

        #region Xử lý kiểm tra dữ liệu
        private bool IsValidImage(IFormFile file)
        {
            // Các định dạng file cho phép
            var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Kiểm tra định dạng file
            if (!permittedExtensions.Contains(extension))
            {
                // Thêm lỗi vào ModelState để hiển thị cho người dùng
                ModelState.AddModelError("Image", "Định dạng file không hợp lệ! Vui lòng tải lên ảnh JPG, JPEG hoặc PNG.");
                return false;
            }

            // Kiểm tra kích thước file (giới hạn 2MB)
            if (file.Length > 2 * 1024 * 1024) // 2MB
            {
                ModelState.AddModelError("Image", "Kích thước file không được vượt quá 2MB.");
                return false;
            }

            return true;
        }

        private bool ValidateUserData(KhachHang user)
        {
            bool isValid = true;

            // Kiểm tra email có tồn tại chưa
            if (IsEmailExist(user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                isValid = false;
            }

            if (!IsValidName(user.HoTen, ModelState)) 
            {
                ModelState.AddModelError("HoTen", "Họ tên không đúng.");
                isValid = false;
            }

            // Kiểm tra số điện thoại có tồn tại chưa
            if (!IsPhoneExist(user.PhoneNumber, ModelState))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                isValid = false;
            }

            // Kiểm tra mật khẩu
            if (!IsValidPassword(user.PasswordHash, ModelState))
            {
                ModelState.AddModelError("PasswordHash", "Mật khẩu không đủ mạnh. Cần chứa chữ hoa, chữ thường, số và ký tự đặc biệt.");
                isValid = false;
            }

            return isValid;
        }

        private bool IsEmailExist(string email)
        {
            return _connectStr.KhachHang.Any(k => k.Email == email);
        }

        private bool IsValidName(string name, ModelStateDictionary modelState)
        {
            bool isValid = true;

            // Kiểm tra nếu chuỗi rỗng hoặc chỉ chứa khoảng trắng
            if (string.IsNullOrWhiteSpace(name))
            {
                modelState.AddModelError("Name", "Tên không được để trống.");
                isValid = false;
            }
            // Kiểm tra độ dài tên
            else if (name.Length < 2 || name.Length > 50)
            {
                modelState.AddModelError("Name", "Tên phải có độ dài từ 2 đến 50 ký tự.");
                isValid = false;
            }

            return isValid;
        }


        private bool IsPhoneExist(string phone, ModelStateDictionary modelState)
        {
            bool isValid = true;

            // Kiểm tra nếu chuỗi rỗng hoặc chỉ chứa khoảng trắng
            if (string.IsNullOrWhiteSpace(phone))
            {
                modelState.AddModelError("PhoneNumber", "Số điện thoại không được để trống.");
                isValid = false;
            }

            // Kiểm tra độ dài số điện thoại
            else if (phone.Length != 10)
            {
                modelState.AddModelError("PhoneNumber", "Số điện thoại phải có 10 ký tự.");
                isValid = false;
            }
            // Kiểm tra nếu số điện thoại không bắt đầu bằng '0'
            else if (!phone.StartsWith("0"))
            {
                modelState.AddModelError("PhoneNumber", "Số điện thoại phải bắt đầu bằng số '0'.");
                isValid = false;
            }
            // Kiểm tra nếu số điện thoại không chứa toàn số
            else if (!phone.All(char.IsDigit))
            {
                modelState.AddModelError("PhoneNumber", "Số điện thoại chỉ được chứa các chữ số.");
                isValid = false;
            }
            // Kiểm tra xem số điện thoại có tồn tại trong cơ sở dữ liệu không
            else if (_connectStr.KhachHang.Any(k => k.PhoneNumber == phone))
            {
                modelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                isValid = false;
            }

            return isValid;
        }

        private bool IsValidEmail(string email) => new EmailAddressAttribute().IsValid(email);

        private bool IsValidPassword(string password, ModelStateDictionary modelState)
        {
            bool isValid = true;
            if (password == null)
            {
                modelState.AddModelError("", "Vui lòng nhập đủ dữ liệu.");
                isValid = false;
            }
            else
            {
                // Kiểm tra mật khẩu có đủ độ mạnh (chứa ký tự đặc biệt, chữ hoa, chữ thường và số)
                var hasUpperCase = password.Any(char.IsUpper);
                var hasLowerCase = password.Any(char.IsLower);
                var hasDigit = password.Any(char.IsDigit);
                var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

                return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
            }
            return isValid;
        }

        #endregion

        #region Sửa thông tin người dùng
        [HttpGet]
        public IActionResult Edit(string email)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null) 
                return RedirectToAction("Login", "Login");

            var khachHang = _connectStr.KhachHang.FirstOrDefault(k => k.Email == email);
            if (khachHang == null) 
                return NotFound();

            GetCrad(userEmail);
            SetViewBag(userEmail);
            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KhachHang khachHang, IFormFile imageFile)
        {
            var currentUser = _connectStr.KhachHang.FirstOrDefault(k => k.Id == khachHang.Id);

            // Lưu lại session hiện tại trước khi bắt đầu kiểm tra
            var currentEmail = HttpContext.Session.GetString("UserEmail");
            var currentImage = HttpContext.Session.GetString("UserImage");

            if (currentUser == null) return NotFound();

            bool isValid = true;

            // Kiểm tra email chỉ khi email mới khác với email hiện tại
            if (khachHang.Email != currentUser.Email)
            {
                if (!IsEmailExist(currentUser.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã tồn tại.");
                    isValid = false;
                }
            }

            if(khachHang.HoTen != currentUser.HoTen)
            {
                if (!IsValidName(khachHang.HoTen, ModelState))
                {
                    ModelState.AddModelError("HoTen", "Tên quá ngắn");
                    isValid = false;
                }
            }

            // Kiểm tra số điện thoại chỉ khi số điện thoại mới khác với số điện thoại hiện tại
            if (khachHang.PhoneNumber != currentUser.PhoneNumber)
            {
                if (!IsPhoneExist(khachHang.PhoneNumber, ModelState))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                    isValid = false;
                }
            }

            // Kiểm tra mật khẩu chỉ khi mật khẩu mới khác với mật khẩu hiện tại
            if (khachHang.PasswordHash != currentUser.PasswordHash)
            {
                if (!IsValidPassword(khachHang.PasswordHash, ModelState))
                {
                    ModelState.AddModelError("PasswordHash", "Mật khẩu không đủ mạnh. Cần chứa chữ hoa, chữ thường, số và ký tự đặc biệt.");
                    isValid = false;
                }
            }

            // Kiểm tra và lưu hình ảnh nếu có thay đổi
            if (imageFile != null && !IsValidImage(imageFile))
            {
                ViewBag.UserEmail = currentEmail;
                ViewBag.currentImage = currentImage;
                return View(khachHang);
            }

            if (imageFile != null)
            {
                // Xóa hình ảnh cũ nếu có
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, currentUser.Hinh.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);

                currentUser.Hinh = await SaveUserImage(imageFile);
            }

            // Cập nhật các trường dữ liệu khác nếu có thay đổi
            UpdateCustomerInfo(khachHang, currentUser);

            if (!isValid)
            {
                SetViewBag(currentEmail!);
                return View(khachHang);
            }

            // Cập nhật thông tin người dùng vào cơ sở dữ liệu
            _connectStr.KhachHang.Update(currentUser);
            await _connectStr.SaveChangesAsync();

            // Cập nhật thông tin session với giá trị mới sau khi thành công
            UpdateSessionUser(currentUser);

            return RedirectToAction("Index", "Home");
        }

        private async Task<string> SaveUserImage(IFormFile imageFile)
        {
            try
            {
                // Đường dẫn đến thư mục lưu trữ hình ảnh
                // Kiểm tra và tạo thư mục nếu chưa tồn tại
                var directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "UserImages");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // Tạo thư mục nếu chưa tồn tại
                }

                // Tạo tên file duy nhất để tránh trùng lặp
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var imagePath = Path.Combine(directoryPath, uniqueFileName);

                // Lưu hình ảnh vào thư mục 'ImagesUser'
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Trả về đường dẫn hình ảnh lưu trữ
                return "/images/UserImages/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void UpdateCustomerInfo(KhachHang khachHang, KhachHang currentUser)
        {
            if (!string.IsNullOrWhiteSpace(khachHang.PasswordHash)) currentUser.PasswordHash = khachHang.PasswordHash;
            if (!string.IsNullOrWhiteSpace(khachHang.DiaChi)) currentUser.DiaChi = khachHang.DiaChi;
            if (!string.IsNullOrWhiteSpace(khachHang.PhoneNumber)) currentUser.PhoneNumber = khachHang.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(khachHang.HoTen)) currentUser.HoTen = khachHang.HoTen;
            if (!string.IsNullOrWhiteSpace(khachHang.Email) && khachHang.Email != currentUser.Email)
            {
                if (IsEmailExist(khachHang.Email)) throw new InvalidOperationException("Email đã tồn tại.");
                currentUser.Email = khachHang.Email;
            }
        }
        #endregion

        #region Xóa Tài Khoản

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string MaKhachHang)
        {
            // Tìm người dùng trong cơ sở dữ liệu
            var KhachHang = _connectStr.KhachHang.FirstOrDefault(kh => kh.Id == MaKhachHang);
            if (KhachHang == null)
            {
                return NotFound();
            }

            // Xóa người dùng từ cơ sở dữ liệu
            KhachHang.TinhTrang = "Ngừng hoạt động";
            _connectStr.KhachHang.Update(KhachHang);
            _connectStr.SaveChanges();

            // Xóa thông tin session khi người dùng bị xóa
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserImage");

            TempData["Message"] = "Tài khoản:"+ KhachHang.Email +" đã xóa tài khoản.";
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Khôi phục tài khoản đã xóa
        [HttpGet]
        public IActionResult KhoiPhuc() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KhoiPhuc(string email, string matKhau,string? xacNhanCode = null)
        {
            // Kiểm tra thông tin tài khoản
            var khachHang = _connectStr.KhachHang.FirstOrDefault(ad => ad.Email == email && ad.PasswordHash == matKhau);
            if (string.IsNullOrEmpty(xacNhanCode))
            {
                // Nếu không tìm thấy khách hàng và không có mã xác nhận
                if (khachHang == null && string.IsNullOrEmpty(xacNhanCode))
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                    return View();
                }

                if (khachHang!.TinhTrang != "Ngừng hoạt động")
                {
                    ModelState.AddModelError("", $"Tài khoản '{email}' vẫn còn hoạt động!");
                    return View();
                }
            }

            // Xử lý mã xác nhận
            else if (!string.IsNullOrEmpty(xacNhanCode))
            {
                khachHang = _connectStr.KhachHang.FirstOrDefault(ad => ad.Email == email);
                var storedCode = HttpContext.Session.GetString("XacNhanCode");

                if (storedCode == null || storedCode != xacNhanCode)
                {
                    ViewData["RequireCode"] = true;
                    ModelState.AddModelError("maXacNhan", "Mã xác nhận không chính xác.");
                    return View();
                }

                // Khôi phục tài khoản
                khachHang!.TinhTrang = "Hoạt động";
                _connectStr.KhachHang.Update(khachHang);
                _connectStr.SaveChanges();

                HttpContext.Session.Remove("XacNhanCode");
                HttpContext.Session.Remove("Email");

                string subject = "Tài khoản của bạn đã được khôi phục";
                string body = "Chào mừng bạn đã trở lại với WebFood Quốc Thịnh! Hãy cùng trải nghiệm những món ăn siêu ngon.";
                _sendMail.SendEmail(khachHang.Email, subject, body);

                return RedirectToAction("Login", "Login");
            }

            // Tạo mã xác nhận nếu chưa có
            var randomCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("XacNhanCode", randomCode);
            HttpContext.Session.SetString("Email", email);
            ViewBag.EmailKp = HttpContext.Session.GetString("Email");

            string subjectEmail = "Mã xác nhận khôi phục tài khoản của bạn";
            string bodyEmail = $"Mã xác nhận của bạn là: {randomCode}";
            _sendMail.SendEmail(khachHang!.Email, subjectEmail, bodyEmail);

            ViewData["RequireCode"] = true;
            return View();
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
            var khachHang = _connectStr.KhachHang.FirstOrDefault(ad => ad.Email == email);
            if (khachHang == null)
            {
                ViewData["ErrorMessage"] = "Email không tồn tại.";
                return View("QuenPasswordHash");
            }
            if (khachHang.TinhTrang != "Hoạt động")
            {
                ViewData["ErrorMessage"] = $"{email} đã ngừng hoạt động!";
                return View("QuenMatKhau");
            }
            // Tạo mật khẩu mới
            var newPassword = "User123@";

            // **Mã hóa mật khẩu mới**
            khachHang.PasswordHash = _passwordHasher.HashPassword(khachHang, newPassword);

            _connectStr.KhachHang.Update(khachHang);
            _connectStr.SaveChanges();

            // Gửi email với mật khẩu mới
            string subject = "Mật khẩu mới của bạn";
            string body = $"Mật khẩu mới của bạn là: {newPassword}";

            _sendMail.SendEmail(khachHang.Email, subject, body);

            TempData["SuccessMessage"] = "Mật khẩu mới đã được gửi đến email của bạn.";

            return RedirectToAction("Login","Login");
        }
        #endregion

        #region Đổi mật khẩu
        public IActionResult ChangePassword(string email)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Login");

            var khachHang = _connectStr.KhachHang.FirstOrDefault(k => k.Email == email);
            if (khachHang == null)
                return NotFound();

            GetCrad(userEmail);
            SetViewBag(userEmail);
            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login", "Login");

            var user = _connectStr.KhachHang.FirstOrDefault(u => u.Email == userEmail);
            if (!_passwordService.ValidatePassword(OldPassword, NewPassword, ConfirmPassword, user, ModelState))
            {
                SetViewBag(userEmail);
                return View();
            }

            _passwordService.UpdatePassword(user!, NewPassword);
            return RedirectToAction("Login", "Login");
        }

        private void SetViewBag(string userEmail)
        {
            ViewBag.UserEmail = userEmail;
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
        }
        #endregion
    }
}
