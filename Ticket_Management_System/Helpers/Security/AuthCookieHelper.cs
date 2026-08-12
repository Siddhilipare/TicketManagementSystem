using System;
using System.Configuration;
using System.Web;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Helpers.Security
{
    public static class AuthCookieHelper
    {
        public const string AccessTokenCookieName = "access_token";
        public const string RefreshTokenCookieName = "refresh_token";

        private static bool IsProductionEnvironment()
        {
            string isProdSetting = ConfigurationManager.AppSettings["IsProduction"];
            return string.Equals(isProdSetting, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetAuthCookies(HttpContextBase httpContext, string accessToken, string refreshToken)
        {
            try
            {
                string accessMinutesSetting = ConfigurationManager.AppSettings["AccessTokenExpiryMinutes"] ?? ConfigurationManager.AppSettings["Jwt:AccessTokenExpiryMinutes"];
                string refreshDaysSetting = ConfigurationManager.AppSettings["RefreshTokenExpiryDays"] ?? ConfigurationManager.AppSettings["Jwt:RefreshTokenExpiryDays"];
                int accessMinutes = !string.IsNullOrEmpty(accessMinutesSetting) ? Convert.ToInt32(accessMinutesSetting) : 15;
                int refreshDays = !string.IsNullOrEmpty(refreshDaysSetting) ? Convert.ToInt32(refreshDaysSetting) : 7;

                HttpCookie accessCookie = new HttpCookie(AccessTokenCookieName, accessToken)
                {
                    HttpOnly = true,
                    Secure = IsProductionEnvironment(),
                    Expires = DateTime.UtcNow.AddMinutes(accessMinutes)
                };
                HttpCookie refreshCookie = new HttpCookie(RefreshTokenCookieName, refreshToken)
                {
                    HttpOnly = true,
                    Secure = IsProductionEnvironment(),
                    Expires = DateTime.UtcNow.AddDays(refreshDays)
                };
                httpContext.Response.Cookies.Set(accessCookie);
                httpContext.Response.Cookies.Set(refreshCookie);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AuthCookieHelper", "SetAuthCookies");
                throw;
            }
        }

        public static void ClearAuthCookies(HttpContextBase httpContext)
        {
            try
            {
                HttpCookie accessCookie = new HttpCookie(AccessTokenCookieName, "")
                {
                    Expires = DateTime.UtcNow.AddDays(-1)
                };
                HttpCookie refreshCookie = new HttpCookie(RefreshTokenCookieName, "")
                {
                    Expires = DateTime.UtcNow.AddDays(-1)
                };
                HttpCookie antiForgeryTokenCookie = new HttpCookie("__RequestVerificationToken", "")
                {
                    Expires = DateTime.UtcNow.AddDays(-1)
                };
                httpContext.Response.Cookies.Set(accessCookie);
                httpContext.Response.Cookies.Set(refreshCookie);
                httpContext.Response.Cookies.Set(antiForgeryTokenCookie);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "AuthCookieHelper", "ClearAuthCookies");
                throw;
            }
        }
    }
}