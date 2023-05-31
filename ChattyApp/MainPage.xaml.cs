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

    protected override void OnDisappearing()
    {
        _vm.IsUserObserving = false;

        base.OnDisappearing();
    }

    protected override async void OnAppearing()
    {
        _vm.IsUserObserving = true;

        await _vm.TryRequestNotificationsEnabled();
        _vm.StartConnectionTestJob();
        await _vm.LoadLastUsername();
        base.OnAppearing();
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
