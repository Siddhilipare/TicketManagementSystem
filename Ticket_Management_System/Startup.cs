using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Ticket_Management_System.Startup))]
namespace Ticket_Management_System
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
