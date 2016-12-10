using Microsoft.AspNetCore.SignalR;
using System;
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


        public void Send(string message)
        {
            Console.WriteLine(message);
        }
    }
}
