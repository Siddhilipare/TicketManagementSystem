using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Ticket_Management_System
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier = "sub";
            // ADD THIS — suppresses the user-identity check on the token
            // so switching users doesn't cause token mismatch errors
            System.Web.Helpers.AntiForgeryConfig.SuppressIdentityHeuristicChecks = true;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex != null)
            {
                string controllerName = null;
                string actionName = null;

                try
                {
                    var routeData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current));
                    if (routeData != null)
                    {
                        controllerName = routeData.Values["controller"]?.ToString();
                        actionName = routeData.Values["action"]?.ToString();
                    }
                }
                catch { }

                // Layer 1: txt file — best-effort
                try
                {
                    Ticket_Management_System.Helpers.Logger.LogToFile(
                        ex,
                        controllerName ?? "Global",
                        actionName ?? "Application_Error");
                }
                catch { }

                // Layer 2: SQL Server ErrorLog table — best-effort
                try
                {
                    new TicketDAL.Dal.ErrorLogDataAccess().LogError(
                        controllerName: controllerName ?? "Global",
                        actionName: actionName ?? "Application_Error",
                        exceptionMessage: ex.Message,
                        stackTrace: ex.StackTrace,
                        userEmail: (Context != null && Context.User != null
                                          && Context.User.Identity != null
                                          && Context.User.Identity.IsAuthenticated)
                                          ? Context.User.Identity.Name : "Anonymous",
                        requestUrl: Request?.Url?.ToString());
                }
                catch { }
            }
        }
    }
}