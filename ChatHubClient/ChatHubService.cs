﻿using Microsoft.AspNetCore.SignalR.Client;

namespace ChatHubClient;

public class ChatHubService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HubConnection _connection;
    private readonly UriBuilder _chatHubUrl;
    private readonly string _healthcheckUri;

    public ChatHubService(HttpClient httpClient, string hubUrl)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);

        _chatHubUrl = new(hubUrl);
        _chatHubUrl.Path = "chathub";

        _healthcheckUri = _chatHubUrl.Uri.AbsoluteUri
            .Replace(_chatHubUrl.Uri.PathAndQuery, "/healthcheck");

        _connection = new HubConnectionBuilder()
            .WithAutomaticReconnect()
            .WithUrl(_chatHubUrl.ToString())
            .Build();

        _connection.Closed += async exception =>
            OnClosed?.Invoke(this, exception);

        _connection.Reconnecting += async exception =>
            OnReconnecting?.Invoke(this, exception);

        _connection.Reconnected += async newConnectionId =>
            OnReconnected?.Invoke(this, newConnectionId);

        _connection.On<Message>(Notification.Subscription.ReceiveMessage, (message) =>
            OnMessageReceived?.Invoke(this, message));

        _connection.On<string>(Notification.Subscription.UserConnected, (userId) =>
            OnUserConnected?.Invoke(this, userId));

        _connection.On<string>(Notification.Subscription.UserDisconnected, (userId) =>
            OnUserDisconnected?.Invoke(this, userId));

        _connection.On<string>(Notification.Subscription.UsernameRegistered, (userName) =>
            OnUsernameRegistered?.Invoke(this, userName));
    }

    public bool IsStarted { get; private set; }
    public string? ActiveUsername { get; private set; }

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

        try
        {
            ActiveUsername = username;
            await _connection.StartAsync();
            await RegisterUsernameAsync(username ?? throw new ArgumentNullException(nameof(username)));
        }
        catch (Exception ex)
        {
            await _connection.StopAsync();
            IsStarted = false;
            ActiveUsername = null;
            throw;
        }
    }

    public Task StopAsync()
    {
        IsStarted = false;
        return _connection.StopAsync();
    }

    public Task SendMessageAsync(string content, string recipientUsername = "all")
    {
        var message = new Message(
            MessageType.Chat,
            new User(ActiveUsername!),
            content,
            new User(recipientUsername));

        return message.IsValid()
            ? _connection.InvokeAsync(Notification.Action.SendMessage, message)
            : Task.CompletedTask;
    }

    private Task RegisterUsernameAsync(string username)
    {
        return _connection.InvokeAsync(Notification.Action.RegisterUsername, username);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public async Task<(bool Online, string? ErrorMessage)> TestConnectivityAsync()
    {
        try
        {
            var result = await _httpClient.GetAsync(_healthcheckUri);
            return (result.IsSuccessStatusCode, result.ReasonPhrase);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }
}