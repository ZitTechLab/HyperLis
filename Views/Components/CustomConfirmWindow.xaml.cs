using System.Windows;

namespace HyperLisUI.Views.Components
{
    public partial class CustomConfirmWindow : Window
    {
        public CustomConfirmWindow(string title, string message, string yesText, string noText)
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
            btnYes.Content = yesText;
            btnNo.Content = noText;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static bool ShowConfirm(
            string message,
            string title = "Confirmation",
            string yesText = "Yes",
            string noText = "No",
            Window? owner = null)
        {
            var dialog = new CustomConfirmWindow(title, message, yesText, noText);
            if (owner != null)
            {
                dialog.Owner = owner;
            }

            return dialog.ShowDialog() == true;
        }
    }
}
