using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Orchard.SignalR
{
    public class HubDescriptorProvider : IHubDescriptorProvider
    {
        private readonly Lazy<IDictionary<string, HubDescriptor>> _hubs;
        private readonly ILogger _logger;

        public HubDescriptorProvider(IEnumerable<IHub> hubs, ILoggerFactory loggerFactory)
        {
            _hubs = new Lazy<IDictionary<string, HubDescriptor>>(() => BuildHubsCache(hubs));
            _logger = loggerFactory.CreateLogger<ReflectedHubDescriptorProvider>();
        }

        public IList<HubDescriptor> GetHubs()
        {
            return _hubs.Value
                .Select(kv => kv.Value)
                .Distinct()
                .ToList();
        }

        public bool TryGetHub(string hubName, out HubDescriptor descriptor)
        {
            return _hubs.Value.TryGetValue(hubName, out descriptor);
        }

        protected IDictionary<string, HubDescriptor> BuildHubsCache(IEnumerable<IHub> hubs)
        {
            // Getting all IHub-implementing types that apply
            var types = hubs.Select(x => x.GetType());

            // Building cache entries for each descriptor
            // Each descriptor is stored in dictionary under a key
            // that is it's name or the name provided by an attribute
            var hubDescriptors = types
                .Select(type => new HubDescriptor
                {
                    NameSpecified = (type.GetHubAttributeName() != null),
                    Name = type.GetHubName(),
                    HubType = type
                });

            var cacheEntries = new Dictionary<string, HubDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var descriptor in hubDescriptors)
            {
                HubDescriptor oldDescriptor = null;
                if (!cacheEntries.TryGetValue(descriptor.Name, out oldDescriptor))
                {
                    cacheEntries[descriptor.Name] = descriptor;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return cacheEntries;
        }
    }

    internal static class HubTypeExtensions
    {
        internal static string GetHubName(this Type type)
        {
            if (!typeof(IHub).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return null;
            }

            return GetHubAttributeName(type) ?? GetHubTypeName(type);
        }

        internal static string GetHubAttributeName(this Type type)
        {
            if (!typeof(IHub).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return null;
            }

            // We can still return null if there is no attribute name
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type.GetTypeInfo(), attr => attr.HubName);
        }

        private static string GetHubTypeName(Type type)
        {
            var lastIndexOfBacktick = type.Name.LastIndexOf('`');
            if (lastIndexOfBacktick == -1)
            {
                return type.Name;
            }
            else
            {
                return type.Name.Substring(0, lastIndexOfBacktick);
            }
        }
    }
}
