using Microsoft.AspNetCore.Identity;

namespace WebFood.Data
{
    public static class IdentityPasswordHelper
    {
        private static readonly PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();

        // Hàm băm mật khẩu khi đăng ký
        public static string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null, password);
        }

        // Hàm kiểm tra mật khẩu khi đăng nhập
        public static bool VerifyPassword(string hashedPassword, string enteredPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, enteredPassword);
            return result == PasswordVerificationResult.Success;
        }
    }

}
