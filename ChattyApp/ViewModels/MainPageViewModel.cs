using System.Windows.Input;

using ChatHubClient;

using Microsoft.Extensions.Logging;

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
    private bool _isRegistered;

    public MainPageViewModel(
        ILogger<MainPageViewModel> logger,
        ChatHubService chatHub)
    {
        _logger = logger;
        _chatHub = chatHub;

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

    private async Task RegisterAsync()
    {
        try
        {
            await _chatHub.StartAsync(UsernameText);
            IsRegistered = _chatHub.IsStarted;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to register with username: {username}",
                UsernameText);

            IsRegistered = false;
        }
    }

    private async Task MessageProcessorJobAsync()
    {
        var failureCount = 0;

        while (true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));

            try
            {
                if (!_messages.TryPeek(out var messageAction))
                {
                    continue;
                }

                await messageAction();
                _messages.Dequeue();
                failureCount = 0;
            }
            catch (Exception ex)
            {
                failureCount++;

                if (failureCount % 300 == 0)
                    _logger.LogError(ex, "Failed to dequeue message");
            }
        }
    }

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        _messages.Enqueue(async () =>
        {
            if (!_chatHub.IsStarted)
            {
                _logger.LogInformation("Sending message is re-initializing chat hub connection with username: {username}",
                    UsernameText);

                await _chatHub.StartAsync(UsernameText);
            }

            _logger.LogInformation("Sending Message: {messageText}",
                MessageText);

            await _chatHub.SendMessageAsync(MessageText);

            MessageText = string.Empty;
        });
    }

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

    public ICommand SendCommand { get; }

    public ICommand RegisterCommand { get; }
}