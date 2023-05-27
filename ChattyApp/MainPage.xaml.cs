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

    protected override async void OnAppearing()
    {
        await TryRequestNotificationsEnabled();

        _vm.StartConnectionTestJob();

        base.OnAppearing();
    }

    private static async Task TryRequestNotificationsEnabled()
    {
        var enabled = AreDeviceNotificationsEnabled();

        if (!enabled)
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Enable Notifications",
                "Your notifications are currently turned off for this app. To receive audio notifications, you need to enable them.",
                "Go to Settings",
                "Cancel");

            if (result)
            {
                AppInfo.ShowSettingsUI();
            }
        }
    }

    public static bool AreDeviceNotificationsEnabled() =>
        AndroidX.Core.App.NotificationManagerCompat.From(Platform.CurrentActivity!).AreNotificationsEnabled();

    private void Message_Entry_OnCompleted(object sender, EventArgs e)
    {
        _vm.SendCommand.Execute(this);
    }

    private void Username_Entry_OnCompleted(object sender, EventArgs e)
    {
        _vm.RegisterCommand.Execute(this);
    }
}
