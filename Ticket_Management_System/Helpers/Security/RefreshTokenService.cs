using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using TicketDAL.Dal;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Helpers.Security
{
    public class RefreshTokenService
    {
        private readonly TokenDAL _tokenDal;
        private readonly int _refreshTokenExpiryDays;

        public RefreshTokenService()
        {
            try
            {
                _tokenDal = new TokenDAL();
                string daysStr = ConfigurationManager.AppSettings["RefreshTokenExpiryDays"] ?? ConfigurationManager.AppSettings["Jwt:RefreshTokenExpiryDays"];
                _refreshTokenExpiryDays = !string.IsNullOrEmpty(daysStr) ? Convert.ToInt32(daysStr) : 7;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "Constructor");
                _refreshTokenExpiryDays = 7;
            }
        }

        public string GenerateAndStore(int userId)
        {
            try
            {
                string rawToken = GenerateRawToken();
                string hash = Hash(rawToken);
                _tokenDal.InsertRefreshToken(userId, hash, DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));
                return rawToken;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "GenerateAndStore");
                throw;
            }
        }

        public RefreshRotationResult Rotate(string incomingRawToken)
        {
            try
            {
                string incomingHash = Hash(incomingRawToken);
                var record = _tokenDal.GetByTokenHash(incomingHash);

                if (record == null)
                    throw new InvalidOperationException("Unknown refresh token.");

                if (record.RevokedAt != null)
                {
                    _tokenDal.RevokeAllForUser(record.UserId);
                    throw new SecurityTokenReuseException();
                }

                if (record.ExpiresAt <= DateTime.UtcNow)
                    throw new InvalidOperationException("Refresh token expired.");

                string newRawToken = GenerateRawToken();
                string newHash = Hash(newRawToken);
                _tokenDal.InsertRefreshToken(record.UserId, newHash, DateTime.UtcNow.AddDays(_refreshTokenExpiryDays));
                _tokenDal.RevokeToken(incomingHash, newHash);

                return new RefreshRotationResult { NewRawToken = newRawToken, UserId = record.UserId };
            }
            catch (SecurityTokenReuseException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "Rotate");
                throw;
            }
        }

        public void Revoke(string rawToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawToken)) return;
                _tokenDal.RevokeToken(Hash(rawToken));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "Revoke");
                throw;
            }
        }

        private string GenerateRawToken()
        {
            try
            {
                var bytes = new byte[64];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "GenerateRawToken");
                throw;
            }
        }

        private string Hash(string rawToken)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
                    return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "RefreshTokenService", "Hash");
                throw;
            }
        }

        public class RefreshRotationResult
        {
            public string NewRawToken { get; set; }
            public int UserId { get; set; }
        }

        public class SecurityTokenReuseException : Exception
        {
            public SecurityTokenReuseException() : base("Refresh token reuse detected — token family revoked.") { }
        }
    }
}