using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HyperLisUI.Views
{
    public partial class GameConfigsView : UserControl
    {
        public GameConfigsView()
        {
            InitializeComponent();
        }

        private void BrowseGameLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Game Location",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                GameLocationTextBox.Text = dialog.FolderName;
            }
        }
    }
}
