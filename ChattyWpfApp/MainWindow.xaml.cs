using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ChattyWpfApp.ViewModels;

namespace ChattyWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;

        public MainWindow()
        {
            DataContext = _vm ??= new MainWindowViewModel(ScrollMessagesToBottom);
            InitializeComponent();
        }

        private void MessageTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var text = ((TextBox)sender).Text;
                SendMessageButton.Focus();
                _vm.SendCommand.Execute(text);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            _ = Task.Run(() => _vm.InitializeAsync());
            base.OnInitialized(e);
        }

        private void ScrollMessagesToBottom()
        {
            MessagesListView.SelectedIndex = MessagesListView.Items.Count - 1;
            MessagesListView.ScrollIntoView(MessagesListView.SelectedItem);
        }
    }
}
