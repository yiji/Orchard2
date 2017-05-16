using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Orchard.BackgroundTasks;
using Orchard.ContentManagement.Display.ContentDisplay;
using Orchard.Data.Migration;
using Orchard.MPUnovo.Commands;
using Orchard.DisplayManagement.Descriptors;
using Orchard.Environment.Commands;
using Orchard.Environment.Navigation;
using Orchard.Security.Permissions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Constraints;
using Orchard.MPUnovo.Components;
using Microsoft.AspNetCore.Session;
using YesSql.Indexes;
using Orchard.MPUnovo.Indexes;
using OdooRpc.CoreCLR.Client.Models;
using Orchard.MPUnovo.Services;

namespace Orchard.MPUnovo
{
    public class Startup : StartupBase
    {
        public override void Configure(IApplicationBuilder builder, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            builder.UseSession();

            routes.MapAreaRoute(
                name: "WeiXin",
                areaName: "Orchard.MPUnovo",
                template: "MPUnovo/Index",
                defaults: new { controller = "Home", action = "Index" }
            );

            //将这路由插入到路由集合第一位，替换掉User模块中的登录
            var inlineConstraintResolver = routes
                .ServiceProvider
                .GetRequiredService<IInlineConstraintResolver>();

            var defaultsDictionary = new RouteValueDictionary(new { controller = "Account", action = "Login" });
            defaultsDictionary["area"] = "Orchard.MPUnovo";

            var constraintsDictionary = new RouteValueDictionary();
            constraintsDictionary["area"] = new StringRouteConstraint("Orchard.MPUnovo");

            var dataToken = new RouteValueDictionary();
            dataToken["area"] = new StringRouteConstraint("Orchard.MPUnovo");

            //这个不会影响RouteCollection在context(RouteContext).RouteData.Routers中第一的位置
            routes.Routes.Insert(0, new Route(
                routes.DefaultHandler,
                "UnovoLogin",
                "Login",
                defaultsDictionary,
                constraintsDictionary,
                new RouteValueDictionary(null),
                inlineConstraintResolver));

            routes.Routes.Insert(0, new Route(
                routes.DefaultHandler,
                "UnovoHome",
                "",
                new RouteValueDictionary(new { area = "Orchard.MPUnovo", controller = "Home", action = "Index" }),
                constraintsDictionary,
                new RouteValueDictionary(null),
                inlineConstraintResolver));

            routes.Routes.Insert(0, new Route(
                routes.DefaultHandler,
                "ERP",
                "{controller}/{action}",
                new RouteValueDictionary(new { area = "Orchard.MPUnovo" }),
                constraintsDictionary,
                new RouteValueDictionary(null),
                inlineConstraintResolver));

            #region 测试自定义UnovoRoute，该路由可用
            ////将这路由插入到路由集合第一位，替换掉User模块中的登录
            //var inlineConstraintResolver = routes
            //    .ServiceProvider
            //    .GetRequiredService<IInlineConstraintResolver>();

            //var defaultsDictionary = new RouteValueDictionary(new { controller = "Account", action = "Login" });
            //defaultsDictionary["area"] = "Orchard.MPUnovo";

            //var constraintsDictionary = new RouteValueDictionary();
            //constraintsDictionary["area"] = new StringRouteConstraint("Orchard.MPUnovo");

            //var dataToken = new RouteValueDictionary();
            //dataToken["area"] = new StringRouteConstraint("Orchard.MPUnovo");

            ////这个不会影响RouteCollection在context(RouteContext).RouteData.Routers中第一的位置
            //routes.Routes.Insert(0, new UnovoRoute(
            //    routes.DefaultHandler,
            //    "UnovoLogin",
            //    "Login",
            //    defaultsDictionary,
            //    constraintsDictionary,
            //    new RouteValueDictionary(null),
            //    inlineConstraintResolver)); 
            #endregion
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            //services.AddScoped<ITestDependency, ClassFoo>();
            //services.AddScoped<ICommandHandler, DemoCommands>();
            //services.AddSingleton<IBackgroundTask, TestBackgroundTask>();
            //services.AddScoped<IShapeTableProvider, DemoShapeProvider>();
            //services.AddShapeAttributes<DemoShapeProvider>();
            //services.AddScoped<INavigationProvider, AdminMenu>();
            //services.AddScoped<IContentDisplayDriver, TestContentElementDisplay>();
            //services.AddScoped<IDataMigration, Migrations>();
            //services.AddScoped<IPermissionProvider, Permissions>();

            services.AddSingleton<IBackgroundTask, UnovoBackgroundTask>();

            OdooConnectionInfo odooConnection = new OdooConnectionInfo();
            odooConnection.Database = "zhc_0228";
            odooConnection.Host = "192.168.3.49";
            odooConnection.Port = 8066;
            odooConnection.IsSSL = false;
            odooConnection.Username = "support@unovo.com.cn";
            odooConnection.Password = "unovo883&";
            services.AddSingleton<OdooConnectionInfo>(odooConnection);

            //要添加这个才可以在添加数据时向PartnerInfoIndex表中加入数据
            services.AddScoped<IIndexProvider, PartnerInfoIndexProvider>();
            //要添加这个才可以更新数据库
            services.AddScoped<IDataMigration, Migrations>();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });


            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.CookieHttpOnly = true;
            });
        }

        public override int Order => 1;
    }
}
