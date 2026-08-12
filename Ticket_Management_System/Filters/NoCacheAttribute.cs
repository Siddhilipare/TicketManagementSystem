using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Ticket_Management_System.Filters
{
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var response = filterContext.HttpContext.Response;
            response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            response.Cache.SetNoStore();
            response.Cache.SetExpires(System.DateTime.UtcNow.AddDays(-1));
            response.Cache.SetRevalidation(System.Web.HttpCacheRevalidation.AllCaches);
            response.AppendHeader("Pragma", "no-cache");

            base.OnResultExecuting(filterContext);
        }
    }
   
}