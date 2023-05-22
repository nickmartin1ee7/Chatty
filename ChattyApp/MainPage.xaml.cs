using ChattyApp.ViewModels;

namespace ChattyApp;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _vm;

    public MainPage(MainPageViewModel vm)
    {
        BindingContext = _vm = vm;
        InitializeComponent();
    }

    private void Message_Entry_OnCompleted(object sender, EventArgs e)
    {
        _vm.SendCommand.Execute(this);
    }

    private void Username_Entry_OnCompleted(object sender, EventArgs e)
    {
        _vm.RegisterCommand.Execute(this);
    }
}
