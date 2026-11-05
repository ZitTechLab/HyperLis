using System.Windows;

namespace HyperLisUI.Views
{
    public partial class USBBootProgressWindow : Window
    {
        public USBBootProgressWindow()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = status;
            });
        }
    }
}
