using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using ChatHubClient;

namespace ChattyWpfApp.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private readonly Action _scrollMessagesToBottom;
    private readonly ChatHubService _chatClient;
    private string _messageText = string.Empty;
    private User _me;

    public MainWindowViewModel(Action scrollMessagesToBottom)
    {
        _scrollMessagesToBottom = scrollMessagesToBottom;
        SendCommand = new Command(execute: async (o) => await SendAsync(o));

        _chatClient = new ChatHubService(new HttpClient(), "REPLACEME"); // TODO: App Configuration

        _chatClient.OnUsernameRegistered += (o, e) =>
            Application.Current.Dispatcher.Invoke(() => OnChatClientOnOnUsernameRegistered(this, e));

        _chatClient.OnMessageReceived += (o, e) =>
            Application.Current.Dispatcher.Invoke(() => OnChatClientOnOnMessageReceived(this, e));
    }

    private void OnChatClientOnOnMessageReceived(object o, Message e)
    {
        if (!Users.Contains(e.Sender))
            Users.Add(e.Sender);

        Messages.Add(e);

        _scrollMessagesToBottom();
    }

    private void OnChatClientOnOnUsernameRegistered(object o, string e)
    {
        var newUser = new User(e);

        if (!Users.Contains(newUser))
            Users.Add(newUser);
    }

    private async Task SendAsync(object? obj)
    {
        await _chatClient.SendMessageAsync(obj?.ToString() ?? MessageText);
        MessageText = string.Empty;
    }

    public ObservableCollection<User> Users { get; } = new();

    public ObservableCollection<Message> Messages { get; } = new();

    public string MessageText
    {
        get => _messageText;
        set => SetField(ref _messageText, value);
    }

    public ICommand SendCommand { get; }

    public Task InitializeAsync()
    {
        return _chatClient.StartAsync("Test"); // TODO: Ask user for name
    }
}