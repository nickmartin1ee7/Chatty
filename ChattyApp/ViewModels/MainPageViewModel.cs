using System.Windows.Input;

using ChatHubClient;

namespace ChattyApp.ViewModels;

public class MainPageViewModel : BaseViewModel
{
    private readonly ChatHubService _chatHub;
    private readonly Task _messageProcessor;
    private string _chatText;
    private string _messageText;
    private readonly Queue<Func<Task>> _messages = new();

    public MainPageViewModel(ChatHubService chatHub)
    {
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
                await _chatHub.StartAsync("Testificate");

            await _chatHub.SendMessageAsync(MessageText);

            _messages.Dequeue();
        });
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
            await _chatHub.StartAsync("Testificate");
    }
}