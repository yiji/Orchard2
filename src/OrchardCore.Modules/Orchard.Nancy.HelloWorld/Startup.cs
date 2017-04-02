using Microsoft.AspNetCore.Modules;
using Microsoft.Extensions.DependencyInjection;
using Nancy;

namespace Orchard.Nancy.HelloWorld
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<INancyModule, HomeModule>();
        }
    }
}
