using System;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Utils
{
    /// <summary>
    /// Helper để hash password (SHA256) và verify.
    /// Lưu ý: Trong production nên dùng bcrypt hoặc Argon2, nhưng để đơn giản dùng SHA256.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash password bằng SHA256.
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            // Chuyển byte array sang hex string (tương thích .NET 6)
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Verify password với hash đã lưu.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tạo OTP ngẫu nhiên 6 chữ số.
        /// </summary>
        public static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}

