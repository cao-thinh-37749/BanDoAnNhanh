using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using WebFood.Models;

namespace WebFood.Data
{

    public class PasswordService
    {
        private readonly ConnectStr _context;
        private readonly IPasswordHasher<KhachHang> _passwordHasher;
        private readonly SendMail _sendMail;

        public PasswordService(ConnectStr context, IPasswordHasher<KhachHang> passwordHasher, SendMail sendMail)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _sendMail = sendMail;
        }

        public bool ValidatePassword(string oldPassword, string newPassword, string confirmPassword, KhachHang? user, ModelStateDictionary modelState)
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                modelState.AddModelError("", "Vui lòng nhập đủ dữ liệu.");
                isValid = false;
            }
            else
            {
                if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword) != PasswordVerificationResult.Success)
                {
                    modelState.AddModelError("PasswordHash", "Mật khẩu hiện tại không đúng.");
                    isValid = false;
                }

                if (!IsValidPassword(newPassword))
                {
                    modelState.AddModelError("PasswordHashMoi", "Mật khẩu không đủ mạnh. Cần chứa chữ hoa, chữ thường, số và ký tự đặc biệt.");
                    isValid = false;
                }

                if (newPassword != confirmPassword)
                {
                    modelState.AddModelError("XacNhan", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                    isValid = false;
                }
            }

            return isValid;
        }

        public void UpdatePassword(KhachHang user, string newPassword)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _context.SaveChanges();

            string subject = "Bạn đã sử dụng chức năng đổi mật khẩu của WebFood Quốc Thịnh";
            string body = "Mật khẩu của bạn đã được thay đổi thành công.";
            _sendMail.SendEmail(user.Email, subject, body);
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}
