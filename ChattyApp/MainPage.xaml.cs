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
}
