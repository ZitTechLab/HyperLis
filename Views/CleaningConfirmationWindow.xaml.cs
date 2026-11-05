using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class CleaningConfirmationWindow : Window
    {
        public CleaningConfirmationWindow(List<string> files)
        {
            InitializeComponent();
            FileList.ItemsSource = files;
            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            Title = Helpers.LocalizationService.Get("wndConfirmCleaningTitle");
            if (BtnConfirm != null) BtnConfirm.Content = Helpers.LocalizationService.Get("btnConfirmClean");
            if (BtnCancel != null) BtnCancel.Content = Helpers.LocalizationService.Get("btnWaitAMinute");
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Allow dragging the window
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
