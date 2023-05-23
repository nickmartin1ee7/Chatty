using System.Collections.Concurrent;

using ChatHubClient;

using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public ConcurrentQueue<Message> Messages { get; } = new();

    public ConcurrentDictionary<string, User> ConnectedUsers { get; } = new();

    public async Task SendMessage(Message message)
    {
        Messages.Enqueue(message);

        string senderId = Context.ConnectionId;

        _logger.LogInformation("New message: {message}; Correlation Id: {cid}", message, message.Id);

        if (message.Recipient?.Username == "all")
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
            _logger.LogInformation("Broadcasted message; Correlation Id: {cid}", message.Id);
        }
        else
        {
            var recipientId = ConnectedUsers.FirstOrDefault(u => u.Value.Username == message.Recipient?.Username).Key;

            if (!string.IsNullOrEmpty(recipientId))
            {
                await Clients.Client(recipientId).SendAsync("ReceiveMessage", message);
                _logger.LogInformation("DMed message to connection: {recipientId}; Correlation Id: {cid}", recipientId, message.Id);
            }
            else
            {
                // Handle recipient not found
                await Clients.Client(senderId).SendAsync("ErrorMessage", "Recipient not found");
                _logger.LogInformation("Failed to DM message; Correlation Id: {cid}", message.Id);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        string userId = Context.ConnectionId;

        _logger.LogInformation("New connection: {userId}", userId);

        ConnectedUsers.TryAdd(userId, new User(string.Empty));
        await Clients.All.SendAsync("UserConnected", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.ConnectionId;

        if (!ConnectedUsers.TryRemove(userId, out var username))
            return;

        _logger.LogInformation("Connection {userId} ended with username {username}",
            userId,
            username);

        await Clients.All.SendAsync("UserDisconnected", username);

        if (exception is not null)
            _logger.LogWarning(exception,
                "Connection {userId} terminated due to exception",
                userId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterUsername(string username)
    {
        string userId = Context.ConnectionId;

        if (ConnectedUsers.Any(c => c.Value.Username == username))
            return;

        _logger.LogInformation("Connection {userId} registered username: {username}",
            userId,
            username);

        ConnectedUsers[userId] = new User(username);
        await Clients.Client(userId).SendAsync("UsernameRegistered", username);

        if (Messages.IsEmpty)
            return;

        _logger.LogInformation("Forwarding {messageCount} messages to connection {userId} with username: {username}",
            Messages.Count,
            userId,
            username);

        foreach (var message in Messages)
        {
            await Clients.Client(userId).SendAsync("ReceiveMessage", message);
        }
    }
}