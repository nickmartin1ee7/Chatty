using System.Collections.ObjectModel;
using System.Windows.Input;

using ChatHubClient;

using Microsoft.Extensions.Logging;

using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace ChattyApp.ViewModels;

public class MainPageViewModel : BaseViewModel
{
    private readonly ILogger<MainPageViewModel> _logger;
    private readonly ChatHubService _chatHub;
    private readonly Queue<Func<Task>> _messages = new();
    private readonly Task _messageProcessor;
    private string _usernameText;
    private string _chatText;
    private string _messageText;
    private string _statusLabelText;
    private bool _isRegistered;
    private bool _isLoading;
    private Color _statusLabelColor;
    private bool _statusVisibility;
    private Task _connectivityCheckerJob;

    public MainPageViewModel(
        ILogger<MainPageViewModel> logger,
        ChatHubService chatHub)
    {
        _logger = logger;
        _chatHub = chatHub;

        _chatHub.OnClosed += ChatHubOnClosed;
        _chatHub.OnReconnecting += ChatHubOnReconnecting;
        _chatHub.OnReconnected += ChatHubOnReconnected;

        _chatHub.OnUserDisconnected += ChatHubOnOnUserDisconnected;
        _chatHub.OnMessageReceived += ChatHubOnOnMessageReceived;
        _chatHub.OnUsernameRegistered += ChatHubOnUsernameRegistered;

        SendCommand = new Command(
            execute: SendMessage,
            canExecute: () =>
                !string.IsNullOrWhiteSpace(MessageText));

        RegisterCommand = new Command(
            execute: async () => await RegisterAsync(),
            canExecute: () =>
                !string.IsNullOrWhiteSpace(UsernameText));

        _messageProcessor = Task.Run(MessageProcessorJobAsync);
    }

    private async void ChatHubOnReconnected(object sender, string e)
    {
        await ShowTemporaryStatusAsync("Back online", System.Drawing.Color.Green);
    }

    private async void ChatHubOnReconnecting(object sender, Exception e)
    {
        await ShowConstantStatusAsync("Reconnecting...", System.Drawing.Color.Gray);
    }

    private async void ChatHubOnClosed(object sender, Exception e)
    {
        await ShowConstantStatusAsync("Offline", System.Drawing.Color.Red);
    }

    private void ChatHubOnOnUserDisconnected(object sender, string username)
    {
        Messages.Add(new Message(new User("System"), $"{username} left")
            .WithTimestamp(DateTimeOffset.Now));
    }

    private async Task ShowTemporaryStatusAsync(string text, System.Drawing.Color color)
    {
        StatusLabelText = text;
        StatusLabelColor = color.ConvertToMauiColor();
        StatusVisibility = true;

        await Task.Delay(TimeSpan.FromSeconds(5));

        StatusVisibility = false;
    }

    private Task ShowConstantStatusAsync(string text, System.Drawing.Color color)
    {
        StatusLabelText = text;
        StatusLabelColor = color.ConvertToMauiColor();
        StatusVisibility = true;

        return Task.CompletedTask;
    }

    private void ChatHubOnUsernameRegistered(object sender, string username)
    {
        if (username == _chatHub.ActiveUsername)
        {
            IsRegistered = true;
            ToggleLoading();
            _ = ShowTemporaryStatusAsync("Connected", System.Drawing.Color.Green);
            _logger.LogInformation("User {username} successfully registered", _chatHub.ActiveUsername);
        }

        Messages.Add(new Message(new User("System"), $"{username} joined")
            .WithTimestamp(DateTimeOffset.Now));
    }

    private void ChatHubOnOnMessageReceived(object sender, Message userMessage)
    {
        Messages.Add(userMessage);

        if (userMessage.Sender.Username != _chatHub.ActiveUsername // Not current user
            && userMessage.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-2)) // Received less than two minutes ago
            ShowNewMessageNotification(userMessage);
    }

    private void ShowNewMessageNotification(Message userMessage)
    {
        const int NEW_MESSAGE_ID = 100;

        var request = new NotificationRequest
        {
            NotificationId = NEW_MESSAGE_ID,
            Title = "New Message",
            Subtitle = userMessage.Sender.Username,
            Description = userMessage.Content,
            Android = new AndroidOptions
            {
                LaunchAppWhenTapped = true,
            },

        };
        LocalNotificationCenter.Current.Show(request);
    }

    private async Task RegisterAsync()
    {
        var username = UsernameText;

        try
        {
            ToggleLoading();
            _logger.LogInformation("Attempting to register user under the username: {username}", username);
            await _chatHub.StartAsync(username);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to register user with username: {username}",
                username);

            ToggleLoading();
        }
    }

    private void ToggleLoading()
    {
        IsLoading = !IsLoading;
    }

    private async Task MessageProcessorJobAsync()
    {
        var delayOfFailureBeforeLog = TimeSpan.FromMinutes(5);
        var delay = TimeSpan.FromMilliseconds(10);
        var failureCount = 0;

        while (true)
        {
            await Task.Delay(delay);

            try
            {
                if (!_messages.TryPeek(out var messageAction))
                {
                    continue;
                }

                await messageAction();
                _messages.Dequeue();
                _logger.LogInformation("Message sent for username: {username}", _chatHub.ActiveUsername);
                failureCount = 0;
            }
            catch (Exception ex)
            {
                failureCount++;

                var failureDuration = (delay * failureCount);
                if (failureDuration.TotalMilliseconds % delayOfFailureBeforeLog.TotalMilliseconds == 0)
                    _logger.LogError(ex, "Failed to dequeue message for user {username}; Failure duration: {failureDuration}",
                        _chatHub.ActiveUsername,
                        failureDuration);
            }
        }
    }

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        var message = MessageText;

        MessageText = string.Empty;
        (SendCommand as Command).ChangeCanExecute();


        _messages.Enqueue(async () =>
        {
            if (!_chatHub.IsStarted)
            {
                _logger.LogInformation("Sending message is re-initializing chat hub connection with username: {username}",
                    _chatHub.ActiveUsername);

                await _chatHub.StartAsync(_chatHub.ActiveUsername);
            }

            _logger.LogInformation("User {username} is sending message: {messageText}",
                _chatHub.ActiveUsername,
                message);

            await _chatHub.SendMessageAsync(message);
        });
    }

    public void StartConnectionTestJob()
    {
        _connectivityCheckerJob = Task.Run(async () =>
        {
            while (true)
            {
                if (!await _chatHub.TestConnectivityAsync())
                {
                    await ShowConstantStatusAsync("Offline", System.Drawing.Color.Red);
                }
                else if (StatusLabelText == "Offline" && StatusVisibility) // TODO: Make a state enum
                {
                    await ShowTemporaryStatusAsync("Back online", System.Drawing.Color.Green);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        });
    }

    public ObservableCollection<Message> Messages { get; } = new();

    public string UsernameText
    {
        get => _usernameText;
        set
        {
            SetField(ref _usernameText, value);
            (RegisterCommand as Command).ChangeCanExecute();
        }
    }

    public string ChatText
    {
        get => _chatText;
        set
        {
            SetField(ref _chatText, value);
        }
    }

    public string MessageText
    {
        get => _messageText;
        set
        {
            SetField(ref _messageText, value);
            (SendCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsRegistered
    {
        get => _isRegistered;
        set
        {
            SetField(ref _isRegistered, value);
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetField(ref _isLoading, value);
        }
    }

    public string StatusLabelText
    {
        get => _statusLabelText;
        set
        {
            SetField(ref _statusLabelText, value);
        }
    }

    public Color StatusLabelColor
    {
        get => _statusLabelColor;
        set
        {
            SetField(ref _statusLabelColor, value);
        }
    }

    public bool StatusVisibility
    {
        get => _statusVisibility;
        set
        {
            SetField(ref _statusVisibility, value);
        }
    }

    public ICommand SendCommand { get; }

    public ICommand RegisterCommand { get; }
}