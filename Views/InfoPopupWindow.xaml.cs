using System.Windows;
using System.Windows.Input;

namespace HyperLisUI.Views
{
    public partial class InfoPopupWindow : Window
    {
        public InfoPopupWindow(string message1, string title = "Info", string? message2 = null)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage1.Text = message1;
            
            if (!string.IsNullOrEmpty(message2))
            {
                MsgSeparator.Visibility = Visibility.Visible;
                TxtMessage2.Visibility = Visibility.Visible;
                TxtMessage2.Text = message2;
            }
        }

        private void GotIt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
