using System;
using System.Security.Cryptography;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Helpers.Security
{
    public class PasswordHashResult
    {
        public string Hash { get; set; }
        public string Salt { get; set; }
    }

    public class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        public static PasswordHashResult HashPassword(string password)
        {
            try
            {
                PasswordHashResult result = new PasswordHashResult();
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, SaltSize, Iterations))
                {
                    byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                    byte[] saltBytes = pbkdf2.Salt;
                    result.Hash = Convert.ToBase64String(hashBytes);
                    result.Salt = Convert.ToBase64String(saltBytes);
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "PasswordHasher", "HashPassword");
                throw;
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, saltBytes, Iterations))
                {
                    byte[] hashBytes = pbkdf2.GetBytes(HashSize);
                    string enteredHash = Convert.ToBase64String(hashBytes);
                    return enteredHash == storedHash;
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "PasswordHasher", "VerifyPassword");
                throw;
            }
        }
    }
}