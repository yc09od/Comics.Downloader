using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Comics.Downloader.Jwt;

namespace Comics.Downloader.Service.Utiliyes
{
    public static class AuthorizationExtensions 
    {
        public static string ToHashedPassword(this string t)
        {
            return HashPassword(t);
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                StringBuilder hashStringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashStringBuilder.Append(b.ToString("x2")); // 转换为16进制表示
                }

                return hashStringBuilder.ToString();
            }
        }
    }
}
