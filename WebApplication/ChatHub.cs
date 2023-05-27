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
        message.Timestamp = DateTime.UtcNow;

        if (!message.IsValid())
            return;

        Messages.Enqueue(message);

        if (Context is null)
            return;

        string senderId = Context.ConnectionId;

        _logger.LogInformation("New message: {message}; Correlation Id: {cid}", message, message.Id);

        if (message.Recipient?.Username == "all")
        {
            await Clients.All.SendAsync(Notification.Subscription.ReceiveMessage, message);
            _logger.LogInformation("Broadcasted message; Correlation Id: {cid}", message.Id);
        }
        else
        {
            var recipientId = ConnectedUsers.FirstOrDefault(u => u.Value.Username == message.Recipient?.Username).Key;

            if (!string.IsNullOrEmpty(recipientId))
            {
                await Clients.Client(recipientId).SendAsync(Notification.Subscription.ReceiveMessage, message);
                _logger.LogInformation("DMed message to connection: {recipientId}; Correlation Id: {cid}", recipientId, message.Id);
            }
            else
            {
                // Handle recipient not found
                await Clients.Client(senderId).SendAsync(Notification.Subscription.ErrorMessage, "Recipient not found");
                _logger.LogInformation("Failed to DM message; Correlation Id: {cid}", message.Id);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        if (Context is null)
            return;

        string userId = Context.ConnectionId;

        _logger.LogInformation("New connection: {userId}", userId);

        ConnectedUsers.TryAdd(userId, new User(string.Empty));
        await Clients.All.SendAsync(Notification.Subscription.UserConnected, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context is null)
            return;

        string userId = Context.ConnectionId;

        if (!ConnectedUsers.TryRemove(userId, out var user))
            return;

        _logger.LogInformation("Connection {userId} ended with user {username}",
            userId,
            user.Username);

        await Clients.All.SendAsync(Notification.Subscription.UserDisconnected, user.Username);

        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            await SendMessage(new Message(MessageType.System, new User("System"), $"{user.Username} has left"));
        }

        if (exception is not null)
            _logger.LogWarning(exception,
                "Connection {userId} terminated due to exception",
                userId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterUsername(string username)
    {
        if (Context is null)
            return;

        string userId = Context.ConnectionId;

        if (ConnectedUsers.Any(c => c.Value.Username == username))
            return;

        _logger.LogInformation("Connection {userId} registered username: {username}",
            userId,
            username);

        ConnectedUsers[userId] = new User(username);
        await Clients.Client(userId).SendAsync(Notification.Subscription.UsernameRegistered, username);

        await SendMessage(new Message(MessageType.System, new User("System"), $"{username} has joined"));

        if (Messages.IsEmpty)
            return;

        _logger.LogInformation("Forwarding {messageCount} messages to connection {userId} with username: {username}",
            Messages.Count,
            userId,
            username);

        foreach (var message in Messages)
        {
            await Clients.Client(userId).SendAsync(Notification.Subscription.ReceiveMessage, message);
        }
    }
}