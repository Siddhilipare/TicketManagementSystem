using System.Web;
using System.Web.Mvc;

namespace Ticket_Management_System
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new Ticket_Management_System.Filters.JwtAuthenticationFilter());
            filters.Add(new Ticket_Management_System.Filters.NoCacheAttribute());

        }
    }
}
