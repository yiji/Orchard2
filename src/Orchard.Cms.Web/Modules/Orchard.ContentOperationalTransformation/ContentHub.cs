using Microsoft.AspNetCore.SignalR;
using Orchard.ContentManagement;

namespace Orchard.ContentOperationalTransformation
{
    public class ContentHub : Hub
    {
        private readonly IContentManager _contentManager;

        public ContentHub(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public void Ok()
        {
            Clients.All.message("foo");
        }
    }
}
