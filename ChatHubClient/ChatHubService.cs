using Microsoft.AspNetCore.SignalR.Client;

namespace ChatHubClient;

public class ChatHubService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private string _username;

    public ChatHubService(string hubUrl)
    {
        var chatHubUrl = new UriBuilder(hubUrl);
        chatHubUrl.Path = "chathub";

        _connection = new HubConnectionBuilder()
            .WithUrl(chatHubUrl.ToString())
            .Build();

        _connection.Closed += async exception => OnClosed?.Invoke(this, exception);

        _connection.Reconnecting += async exception => OnReconnecting?.Invoke(this, exception);

        _connection.Reconnected += async newConnectionId => OnReconnected?.Invoke(this, newConnectionId);

        _connection.On<Message>("ReceiveMessage", (message) =>
            OnMessageReceived?.Invoke(this, message));

        _connection.On<string>("UserConnected", (userId) =>
            OnUserConnected?.Invoke(this, userId));

        _connection.On<string>("UserDisconnected", (userId) =>
            OnUserDisconnected?.Invoke(this, userId));

        _connection.On<string>("UsernameRegistered", (userName) =>
            OnUsernameRegistered?.Invoke(this, userName));
    }

    public bool IsStarted { get; private set; }

    public event EventHandler<Message> OnMessageReceived;
    public event EventHandler<string> OnUserConnected;
    public event EventHandler<string> OnUserDisconnected;
    public event EventHandler<string> OnUsernameRegistered;
    public event EventHandler<Exception> OnClosed;
    public event EventHandler<Exception> OnReconnecting;
    public event EventHandler<string> OnReconnected;

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
        return _connection.InvokeAsync("SendMessage", new Message(new User(_username), message, new User(recipientUsername)));
    }

    private Task RegisterUsernameAsync(string username)
    {
        return _connection.InvokeAsync("RegisterUsername", username);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}