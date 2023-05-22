using System.Collections.Concurrent;

using ChatHubClient;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSingleton<ChatHub>();
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

app.MapGet("/healthcheck",
        () => "Healthy")
.WithName("GetHealthCheck");

app.MapGet("/chathub/clients",
        ([FromServices] ChatHub chatHub) =>
            chatHub.ConnectedUsers)
    .WithName("GetChatHubClients");

app.Run();

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public ConcurrentDictionary<string, User> ConnectedUsers { get; } = new();

    public async Task SendMessage(Message message)
    {
        string senderId = Context.ConnectionId;

        _logger.LogInformation("New message: {message}", message);

        if (message.Recipient?.Username == "all")
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
        else
        {
            var recipientId = ConnectedUsers.FirstOrDefault(u => u.Value.Username == message.Recipient?.Username).Key;

            if (!string.IsNullOrEmpty(recipientId))
            {
                await Clients.Client(recipientId).SendAsync("ReceiveMessage", message);
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

        ConnectedUsers.TryAdd(userId, new User(string.Empty));
        await Clients.All.SendAsync("UserConnected", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string userId = Context.ConnectionId;

        _logger.LogInformation("Connection ended: {userId}", userId);

        ConnectedUsers.TryRemove(userId, out _);
        await Clients.All.SendAsync("UserDisconnected", userId);
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
    }
}
