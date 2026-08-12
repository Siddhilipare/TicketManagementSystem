using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Web.Mvc.Filters;
using Ticket_Management_System.Helpers.Security;
using TicketModel.ViewModel;
using TicketDAL.Dal;
namespace Ticket_Management_System.Filters
{
    public class JwtAuthenticationFilter : IAuthenticationFilter
    {
        public void OnAuthentication(AuthenticationContext filterContext)
        {
            HttpContextBase httpContext = filterContext.HttpContext;

            HttpCookie accessCookie = httpContext.Request.Cookies[AuthCookieHelper.AccessTokenCookieName];
            HttpCookie refreshCookie = httpContext.Request.Cookies[AuthCookieHelper.RefreshTokenCookieName];

            JwtTokenService jwtService = new JwtTokenService();

            if (accessCookie != null && !string.IsNullOrEmpty(accessCookie.Value))
            {
                ClaimsPrincipal claimsPrincipal = jwtService.ValidateAccessToken(accessCookie.Value);
                if (claimsPrincipal != null)
                {
                    SetPrincipal(httpContext, claimsPrincipal);
                    return;
                }
            }

            if (refreshCookie != null && !string.IsNullOrEmpty(refreshCookie.Value))
            {
                TryRefresh(httpContext, refreshCookie.Value, jwtService);
            }
        }

        public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
        {
            var user = filterContext.HttpContext.User;
            if (filterContext.Result is HttpUnauthorizedResult)
            {
                if (user != null && user.Identity.IsAuthenticated)
                {
                    filterContext.HttpContext.Response.StatusCode = 403;

                    if (filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = new { message = "Forbidden - You do not have the required role to access this resource." },
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }
                    else
                    {
                        filterContext.Result = new RedirectToRouteResult(
                            new System.Web.Routing.RouteValueDictionary(
                                new { controller = "Account", action = "AccessDenied" }
                            )
                        );
                    }
                }
                else
                {
                    filterContext.HttpContext.Response.StatusCode = 401;

                    if (filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = new { message = "Unauthorized - Please log in." },
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }
                    // Non-AJAX: framework redirects to Login via <authentication mode="Forms"> in Web.config
                }
            }
        }
       
        private void TryRefresh(HttpContextBase httpContext, string rawRefreshToken, JwtTokenService jwtService)
        {
            RefreshTokenService refreshService = new RefreshTokenService();
            RefreshTokenService.RefreshRotationResult rotationResult;

            try
            {
                rotationResult = refreshService.Rotate(rawRefreshToken);
            }
            catch (Exception)
            {
                AuthCookieHelper.ClearAuthCookies(httpContext);
                return;
            }

            UserDAL userDal = new UserDAL();
            var user = userDal.GetUserById(rotationResult.UserId);

            if (user == null || !user.IsActive)
            {
                AuthCookieHelper.ClearAuthCookies(httpContext);
                return;
            }

            string newAccessToken = jwtService.GenerateAccessToken(user.UserId, user.Email, user.RoleName);
            AuthCookieHelper.SetAuthCookies(httpContext, newAccessToken, rotationResult.NewRawToken);

            ClaimsPrincipal newPrincipal = jwtService.ValidateAccessToken(newAccessToken);
            if (newPrincipal != null)
            {
                SetPrincipal(httpContext, newPrincipal);
            }
        }

        private void SetPrincipal(HttpContextBase httpContext, ClaimsPrincipal claimsPrincipal)
        {
            httpContext.User = claimsPrincipal;
            System.Threading.Thread.CurrentPrincipal = claimsPrincipal;
        }

        private string GetClaimValue(ClaimsPrincipal principal, string primaryType, string fallbackType)
        {
            Claim claim = principal.FindFirst(primaryType);
            if (claim == null && fallbackType != null)
            {
                claim = principal.FindFirst(fallbackType);
            }
            return claim != null ? claim.Value : null;
        }

    }
}