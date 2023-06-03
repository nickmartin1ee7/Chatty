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
        private readonly MainWindowViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm = new MainWindowViewModel();
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
    }
}
