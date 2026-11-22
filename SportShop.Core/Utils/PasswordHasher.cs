using System;
using System.Security.Cryptography;
using System.Text;

namespace SportShopAdmin.Utils
{
    /// <summary>
    /// Băm mật khẩu: lưu dạng "SALT$HASH" với HASH = SHA256(SALT + matKhau) dạng HEX in hoa.
    /// Định dạng này khớp với dữ liệu mẫu trong SportShopDB.sql và có thể dùng lại cho ứng dụng Web.
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            string salt = CreateSalt();
            return salt + "$" + Sha256Hex(salt + password);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored) || password == null) return false;
            int idx = stored.IndexOf('$');
            if (idx <= 0) return false;
            string salt = stored.Substring(0, idx);
            string hash = stored.Substring(idx + 1);
            return string.Equals(Sha256Hex(salt + password), hash, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateSalt()
        {
            var bytes = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private static string Sha256Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
