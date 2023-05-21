using Microsoft.AspNetCore.SignalR.Client;

namespace ChatHubClient;

public class ChatHubService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private string _username;

    public ChatHubService(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        _connection.On<string, string>("ReceiveMessage", (senderId, message) =>
            OnMessageReceived?.Invoke(this, (senderId, message)));

        _connection.On<string>("UserConnected", (userId) =>
            OnUserConnected?.Invoke(this, userId));

        _connection.On<string>("UserDisconnected", (userId) =>
            OnUserDisconnected?.Invoke(this, userId));
    }

    public bool IsStarted { get; private set; }

    public event EventHandler<(string userId, string message)> OnMessageReceived;
    public event EventHandler<string> OnUserConnected;
    public event EventHandler<string> OnUserDisconnected;

    public async Task StartAsync(string username)
    {
        if (IsStarted)
            return;

        IsStarted = true;

        _username = username;

        try
        {
            await _connection.StartAsync();
            await RegisterUsernameAsync(username);
        }
        catch (Exception ex)
        {
            IsStarted = false;
            throw;
        }
    }

    public Task StopAsync()
    {
        IsStarted = false;
        return _connection.StopAsync();
    }

    public Task SendMessageAsync(string message, string recipientUsername = "all")
    {
        return _connection.InvokeAsync("SendMessage", _username, recipientUsername, message);
    }

    private Task RegisterUsernameAsync(string username)
    {
        return _connection.InvokeAsync("RegisterUsername", username);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}