using System;
using Microsoft.AspNetCore.Modules;
using Microsoft.Extensions.DependencyInjection;
using Nancy;

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
            // TODO: Either register everythign individually, or take the assembly and scan it.
            // i.e. typeof(NancyEngine).GetTypeInfo().Assembly.ExportedTypes......
            // Not Sure

            services.AddScoped<INancyEngine, NancyEngine>();

            return services;
        }
    }
}