using System.Windows;
using System.Windows.Input;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class CleaningCompletedWindow : Window
    {
        private string? _customTitleKey;

        public CleaningCompletedWindow(string? message = null, string? titleKey = null)
        {
            _customTitleKey = titleKey;
            InitializeComponent();
            MessageText.Text = !string.IsNullOrEmpty(message) ? message : Helpers.LocalizationService.Get("msgCleaningCompletedDefault");
            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            Title = Helpers.LocalizationService.Get("wndCleaningCompletedTitle");
            if (LblInfo != null) LblInfo.Text = !string.IsNullOrEmpty(_customTitleKey) ? Helpers.LocalizationService.Get(_customTitleKey) : Helpers.LocalizationService.Get("msgInfo");
            if (BtnGotIt != null) BtnGotIt.Content = Helpers.LocalizationService.Get("btnGotIt");
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
