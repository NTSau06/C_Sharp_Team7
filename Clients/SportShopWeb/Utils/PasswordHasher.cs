using System.Security.Cryptography;
using System.Text;

namespace SportShopWeb.Utils
{
    /// <summary>
    /// Băm mật khẩu "SALT$HASH" với HASH = SHA256(SALT + matKhau) dạng HEX in hoa.
    /// Giống hệt PasswordHasher của phân hệ Admin và dữ liệu mẫu trong SportShopDB.sql.
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            string salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
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

        private static string Sha256Hex(string text)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }
    }
}
