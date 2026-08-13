using System;
using System.Security.Cryptography;
using System.Text;

namespace Ticket_Management_System.Helpers
{
   
    public static class OtpGenerator
    {
        public static string Generate(int length = 6)
        {
            const string digits = "0123456789";
            var randomBytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(digits[randomBytes[i] % digits.Length]);
            }
            return sb.ToString();
        }

        public static string Hash(string otp)
        {
            using (var sha = new SHA256Managed())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(otp ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
