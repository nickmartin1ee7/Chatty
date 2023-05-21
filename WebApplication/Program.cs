using System.Collections.Concurrent;

using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#if !DEBUG
app.UseHttpsRedirection();
#endif

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHub>("/chathub");
});

app.MapGet("/healthcheck", () => "Healthy")
    .WithName("GetHealthCheck");

app.Run();

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private static readonly ConcurrentDictionary<string, string> s_connectedUsers = new();

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string username, string recipientUsername, string message)
    {
        string senderId = Context.ConnectionId;

        _logger.LogInformation("New message from {username} to {recipientUsername}: {message}",
            username,
            recipientUsername,
            message);

        if (recipientUsername == "all")
        {
            await Clients.All.SendAsync("ReceiveMessage", senderId, $"{username}: {message}");
        }
        else
        {
            if (s_connectedUsers.TryGetValue(recipientUsername, out string? recipientId))
            {
                await Clients.Client(recipientId).SendAsync("ReceiveMessage", senderId, $"{username}: {message}");
            }
            else
            {
                // Handle recipient not found
                await Clients.Client(senderId).SendAsync("ErrorMessage", "Recipient not found");
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        string userId = Context.ConnectionId;

        _logger.LogInformation("New connection: {userId}", userId);

        s_connectedUsers.TryAdd(userId, string.Empty);
        await Clients.All.SendAsync("UserConnected", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.ConnectionId;

        _logger.LogInformation("Connection ended: {userId}", userId);

        s_connectedUsers.TryRemove(userId, out _);
        await Clients.All.SendAsync("UserDisconnected", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RegisterUsername(string username)
    {
        string userId = Context.ConnectionId;

        _logger.LogInformation("Connection {userId} registered username: {username}",
            userId,
            username);

        s_connectedUsers[userId] = username;
        await Clients.Client(userId).SendAsync("UsernameRegistered", username);
    }
}
