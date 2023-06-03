using System.Collections.ObjectModel;
using System.Windows.Input;

using ChatHubClient;

namespace ChattyWpfApp.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private string _messageText = string.Empty;
    private User _me;

    public MainWindowViewModel()
    {
        SendCommand = new Command(execute: Send);
    }

    private void Send(object? obj)
    {
        _me = new User("Test");

        if (!Users.Contains(_me))
            Users.Add(_me);

        Messages.Add(new Message(MessageType.Chat, _me, obj?.ToString() ?? MessageText));
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
}