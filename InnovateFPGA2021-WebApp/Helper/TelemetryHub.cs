using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace InnovateFPGA2021_WebApp.Helper
{
    public class TelemetryHub : Hub
    {
        private readonly ILogger<TelemetryHub> _logger;

        public TelemetryHub(ILogger<TelemetryHub> logger)
        {
            _logger = logger;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public void BroadcastMessage(string name, string message)
        {
            Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public void Echo(string name, string message)
        {
            Clients.Client(Context.ConnectionId).SendAsync("echo", name, message + " (echo from server)");
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected. Connection: {ConnectionId}; User: {UserIdentifier}", Context.ConnectionId, Context.UserIdentifier);
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            //await chatService.ChangeUserStatus(targetUserUid: Context.UserIdentifier, isOnline: false);
            //ConnectedUsers.TryRemove(Context.UserIdentifier, out string value);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
