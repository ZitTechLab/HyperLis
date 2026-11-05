using System.Windows;
using System.Windows.Input;

namespace HyperLisUI.Views.Components
{
    public partial class CustomMessageBoxWindow : Window
    {
        public CustomMessageBoxWindow(string message, string title)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;

            string gotIt = Helpers.LocalizationService.Get("btnGotIt");
            if (!string.IsNullOrEmpty(gotIt) && gotIt != "btnGotIt")
            {
                OkButton.Content = gotIt;
            }

            if (!string.IsNullOrEmpty(message) && (message.Contains("\n") || message.Length > 80))
            {
                MessageText.TextAlignment = TextAlignment.Left;
            }
            else
            {
                MessageText.TextAlignment = TextAlignment.Center;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try { this.DragMove(); } catch { }
        }

        public static void Show(string message, string title = "Info")
        {
            var msgBox = new CustomMessageBoxWindow(message, title);
            try
            {
                if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    msgBox.Owner = Application.Current.MainWindow;
                    msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
            }
            catch { }
            msgBox.ShowDialog();
        }
    }
}
