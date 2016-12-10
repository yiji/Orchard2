using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Display.ContentDisplay;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentOperationalTransformation.Drivers;
using Orchard.ContentOperationalTransformation.Handlers;
using Orchard.ContentOperationalTransformation.Hubs;
using Orchard.ContentOperationalTransformation.Model;
using Orchard.Data.Migration;

namespace Orchard.ContentOperationalTransformation
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Body Part
            services.AddScoped<IContentPartDisplayDriver, ContentOTPartDisplay>();
            services.AddSingleton<ContentPart, ContentOTPart>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentPartHandler, ContentOTPartHandler>();
        }

        public override void Configure(IApplicationBuilder app, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            app.UseSignalR(hrb => {
                hrb.MapHub<ContentHub>("/contenthub");
            });
        }
    }
}
