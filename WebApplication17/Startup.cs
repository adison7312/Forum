using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebApplication17.Startup))]
namespace WebApplication17
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
