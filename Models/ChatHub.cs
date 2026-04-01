using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static Dictionary<string, string> users = new();

    public Task Register(string user)
    {
        users[user] = Context.ConnectionId;
        return Task.CompletedTask;
    }

    public async Task SendPrivate(string fromUser, string toUser, string message)
    {
        if (users.TryGetValue(toUser, out var connectionId))
        {
            await Clients.Client(connectionId)
                .SendAsync("receiveMessage", fromUser, message);
        }
    }
}