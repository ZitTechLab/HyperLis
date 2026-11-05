using System.Windows;
using System.Windows.Input;

namespace HyperLisUI.Views
{
    public partial class ModernConfirmationWindow : Window
    {
        public ModernConfirmationWindow(string message, string title = "Confirmation", string confirmText = "Yes", string cancelText = "No")
        {
            InitializeComponent();
            TxtMessage.Text = message;
            TxtTitle.Text = title;
            BtnConfirm.Content = confirmText;

            if (!string.IsNullOrEmpty(message) && (message.Contains("\n") || message.Length > 80))
            {
                TxtMessage.TextAlignment = TextAlignment.Left;
            }
            else
            {
                TxtMessage.TextAlignment = TextAlignment.Center;
            }
            
            if (string.IsNullOrEmpty(cancelText))
            {
                BtnCancel.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnCancel.Content = cancelText;
                BtnCancel.Visibility = Visibility.Visible;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try { DialogResult = true; } catch { }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try { DialogResult = false; } catch { }
            Close();
        }

        // Allow dragging the window
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try { this.DragMove(); } catch { }
        }
    }
}
