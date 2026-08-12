using System;
using System.Web.Mvc;
using Ticket_Management_System.Helpers;
using Ticket_Management_System.Helpers.Security;

namespace Ticket_Management_System.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                var tokenCookie = Request.Cookies[AuthCookieHelper.AccessTokenCookieName];
                if (tokenCookie != null && !string.IsNullOrEmpty(tokenCookie.Value))
                {
                    var jwtService = new JwtTokenService();
                    var principal = jwtService.ValidateAccessToken(tokenCookie.Value);
                    if (principal != null)
                    {
                        var roleClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Role);
                        string role = roleClaim != null ? roleClaim.Value : "";
                        switch (role)
                        {
                            case "Administrator":
                                return RedirectToAction("Dashboard", "Admin");
                            case "Support Executive":
                                return RedirectToAction("Index", "Support");
                            default:
                                return RedirectToAction("Index", "Ticket");
                        }
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "Home", "Index");
                return View();
            }
        }

        public ActionResult About()
        {
            try
            {
                ViewBag.Message = "Your application description page.";
                return View();
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "Home", "About");
                return View();
            }
        }

        public ActionResult Contact()
        {
            try
            {
                ViewBag.Message = "Your contact page.";
                return View();
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "Home", "Contact");
                return View();
            }
        }
    }
}