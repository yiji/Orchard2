using System;
using Microsoft.AspNetCore.Modules;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Configuration;
using Nancy.Bootstrapper;

namespace Microsoft.AspNetCore.Nancy.Modules
{
    public static class ModularServiceCollectionExtensions
    {
        public static ModularServiceCollection AddNancyModules(this ModularServiceCollection moduleServices, 
            IServiceProvider applicationServices)
        {
            moduleServices.Configure(services =>
            {
                services.AddNancyModules(applicationServices);
            });

            return moduleServices;
        }

        public static IServiceCollection AddNancyModules(this IServiceCollection services,
            IServiceProvider applicationServices)
        {
            services.AddRouting();


            // TODO: Either register everythign individually, or take the assembly and scan it.
            // i.e. typeof(NancyEngine).GetTypeInfo().Assembly.ExportedTypes......
            // Not Sure

            services.AddSingleton<INancyBootstrapper>(x =>
            {
                var bootstrapper = new DefaultNancyBootstrapper();
                bootstrapper.Initialise();
                return bootstrapper;
            });
            services.AddSingleton<INancyEngine>(x => x.GetRequiredService<INancyBootstrapper>().GetEngine());
            services.AddSingleton<INancyEnvironment>(x => x.GetRequiredService<INancyBootstrapper>().GetEnvironment());

            return services;
        }
    }
}