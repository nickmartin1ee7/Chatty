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

        await TryRequestNotificationsEnabled();
        _vm.StartConnectionTestJob();

        await Task.Delay(100);
        TypingImageAnimation.IsAnimationPlaying = true;

        base.OnAppearing();
    }

    private static async Task TryRequestPhoneEnabled()
    {
        if (!Permissions.IsDeclaredInManifest("android.permission.READ_PHONE_STATE"))
            return;

        var status = await Permissions.CheckStatusAsync<Permissions.Phone>();

        if (status == PermissionStatus.Granted)
            return;

        while (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Phone>();
        }

        Environment.Exit(0);
    }

    private static async Task TryRequestNotificationsEnabled()
    {
        var shouldPrompt = true;

        try
        {
            if (!bool.TryParse(await SecureStorage.GetAsync("prompt_notifications"), out shouldPrompt))
            {
                await SetPromptNotificationsAsync(shouldPrompt = true); // Default to prompt user
            }
        }
        catch (Exception e)
        {
            // No logger available here
        }

        if (shouldPrompt && !AreDeviceNotificationsEnabled())
        {
            var userPermissionResult = await Application.Current!.MainPage!.DisplayAlert(
                "Enable Notifications",
                "Your notifications are currently turned off for this app. To receive audio notifications, you need to enable them.",
                "Go to Settings",
                "Cancel");

            if (userPermissionResult)
            {
                AppInfo.ShowSettingsUI();
            }

            try
            {
                await SetPromptNotificationsAsync(userPermissionResult); // Don't prompt again
            }
            catch (Exception e)
            {
                // No logger available here
            }
        }
    }

    private static async Task SetPromptNotificationsAsync(bool shouldPrompt)
    {
        await SecureStorage.SetAsync("prompt_notifications", shouldPrompt.ToString());
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
