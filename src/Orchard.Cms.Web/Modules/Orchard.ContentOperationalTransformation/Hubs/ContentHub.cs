using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Orchard.ContentOperationalTransformation.Hubs
{
    public class ContentHub : Hub
    {
        //private readonly IContentManager _contentManager;

        //public ContentHub(IContentManager contentManager)
        //{
        //    _contentManager = contentManager;
        //}

        public override Task OnConnectedAsync()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                Context.Connection.Channel.Dispose();
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync()
        {
            return Task.CompletedTask;
        }

        public async Task Send(string message)
        {
            await Clients.All.InvokeAsync("Send", $"{Context.User.Identity.Name}: {message}");
        }
    }
}
