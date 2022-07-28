using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ThingsAPI
{
    public class NotifyHub : Hub
    {

        public async Task SendPing(string user, string message)
        {
            await Clients.All.SendAsync("BroadcastPing", user, message);
        }

        public async Task NotifyThingUpdate(string message)
        {
            await Clients.All.SendAsync("BroadcastThingUpdate", message);
        }

        public async Task NotifyThingDelete(string message)
        {
            await Clients.All.SendAsync("BroadcastThingDelete", message);
        }

        public async Task NotifyThingDeleteAll()
        {
            await Clients.All.SendAsync("BroadcastThingDelete");
        }
    }
}



