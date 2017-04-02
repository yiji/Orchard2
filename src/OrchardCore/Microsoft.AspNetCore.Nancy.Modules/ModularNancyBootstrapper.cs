using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;

namespace Microsoft.AspNetCore.Nancy.Modules
{
    public class ModularNancyBootsrapper : INancyBootstrapper, INancyModuleCatalog
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ModularNancyBootsrapper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Dispose()
        {
            
        }

        public IEnumerable<INancyModule> GetAllModules(NancyContext context)
        {
            return _httpContextAccessor.HttpContext.RequestServices.GetServices<INancyModule>();
        }

        public INancyEngine GetEngine()
        {
            return _httpContextAccessor.HttpContext.RequestServices.GetService<INancyEngine>();
        }

        public INancyEnvironment GetEnvironment()
        {
            return new DefaultNancyEnvironment();
        }

        public INancyModule GetModule(Type moduleType, NancyContext context)
        {
            return (INancyModule)_httpContextAccessor.HttpContext.RequestServices.GetService(moduleType);
        }

        public void Initialise()
        {
            
        }
    }
}
