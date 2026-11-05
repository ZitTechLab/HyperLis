using System.Windows.Controls;
using HyperLisUI.Helpers;

namespace HyperLisUI.Views
{
    public partial class SettingsDescriptionView : UserControl
    {
        public SettingsDescriptionView()
        {
            InitializeComponent();
            this.Loaded += SettingsDescriptionView_Loaded;
            this.Unloaded += SettingsDescriptionView_Unloaded;
        }

        private void SettingsDescriptionView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplyTranslations();
            LocalizationService.LanguageChanged += ApplyTranslations;
        }

        private void SettingsDescriptionView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LocalizationService.LanguageChanged -= ApplyTranslations;
        }

        private void ApplyTranslations()
        {
            if (lblDonationTitle != null) lblDonationTitle.Text = LocalizationService.Get("lblDonationTitle");
            if (lblDonationDesc != null) lblDonationDesc.Text = LocalizationService.Get("lblDonationDesc");
            if (lblDonationThanks != null) lblDonationThanks.Text = LocalizationService.Get("lblDonationThanks");
        }
    }
}
