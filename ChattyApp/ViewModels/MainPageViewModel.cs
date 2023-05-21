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
    private string _username;
    private string _chatText;
    private string _messageText;

    public MainPageViewModel(
        ILogger<MainPageViewModel> logger,
        ChatHubService chatHub)
    {
        _logger = logger;
        _chatHub = chatHub;

        SendCommand = new Command(
            execute: async () => await SendMessageAsync(),
            canExecute: () =>
                !string.IsNullOrWhiteSpace(MessageText));

        _messageProcessor = Task.Run(MessageProcessorJobAsync);
    }

    private async Task MessageProcessorJobAsync()
    {
        while (true)
        {
            await Task.Delay(10);

            try
            {
                if (!_messages.TryPeek(out var messageAction))
                {
                    continue;
                }

                await messageAction();
                _messages.Dequeue();
            }
            catch (Exception ex)
            {
            }
        }
    }

    private async Task SendMessageAsync()
    {
        _messages.Enqueue(async () =>
        {
            if (!_chatHub.IsStarted)
            {
                _logger.LogInformation("Sending message is re-initializing chat hub connection with username: {username}",
                    Username);

                await _chatHub.StartAsync(Username);
            }

            _logger.LogInformation("Sending Message: {messageText}",
                MessageText);

            await _chatHub.SendMessageAsync(MessageText);

            MessageText = string.Empty;
        });
    }

    public string Username
    {
        get => _username;
        set
        {
            SetField(ref _username, value);
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

    public ICommand SendCommand { get; }

    public async Task InitializeAsync()
    {
        if (!_chatHub.IsStarted)
        {
            _logger.LogInformation("Initializing chat hub connection with username: {username}",
                Username);

            await _chatHub.StartAsync(Username);
        }
    }
}