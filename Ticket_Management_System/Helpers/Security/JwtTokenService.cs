using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Helpers.Security
{
    public class JwtTokenService
    {
        static JwtTokenService()
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        }

        private readonly string _signingKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;

        public JwtTokenService()
        {
            try
            {
                _signingKey = ConfigurationManager.AppSettings["JwtSecretKey"] ?? ConfigurationManager.AppSettings["Jwt:SigningKey"];
                _issuer = ConfigurationManager.AppSettings["JwtIssuer"] ?? ConfigurationManager.AppSettings["Jwt:Issuer"];
                _audience = ConfigurationManager.AppSettings["JwtAudience"] ?? ConfigurationManager.AppSettings["Jwt:Audience"];

                string minutesStr = ConfigurationManager.AppSettings["AccessTokenExpiryMinutes"] ?? ConfigurationManager.AppSettings["Jwt:AccessTokenExpiryMinutes"];
                _accessTokenExpiryMinutes = !string.IsNullOrEmpty(minutesStr) ? Convert.ToInt32(minutesStr) : 15;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "JwtTokenService", "Constructor");
                _accessTokenExpiryMinutes = 15;
            }
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        }

        public string GenerateAccessToken(int userId, string email, string roleName)
        {
            try
            {
                var now = DateTime.UtcNow;
                long unixTime = (long)(now - new DateTime(1970, 1, 1)).TotalSeconds;

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim(JwtRegisteredClaimNames.Email, email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, unixTime.ToString(), ClaimValueTypes.Integer64)
                };

                var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _issuer,
                    audience: _audience,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddMinutes(_accessTokenExpiryMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "JwtTokenService", "GenerateAccessToken");
                throw;
            }
        }

        public ClaimsPrincipal ValidateAccessToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return null;

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetSigningKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                SecurityToken validatedToken;
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out validatedToken);
                JwtSecurityToken jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null)
                    return null;

                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "JwtTokenService", "ValidateAccessToken");
                return null;
            }
        }
    }
}