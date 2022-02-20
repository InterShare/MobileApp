using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public string AppVersion { get; set; }
        public string Website { get; set; } = "https://smts.julba.de";

        public SettingsPage()
        {
            try
            {
                AppVersion = VersionTracking.CurrentVersion;
            }
            catch (Exception exception)
            {
                AppVersion = "Unknown";
                Console.WriteLine(exception);
            }

            InitializeComponent();
        }

        private async void OnOpenWebsiteClicked(object sender, EventArgs e)
        {
            try
            {
                await Browser.OpenAsync(Website, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        void TapGestureRecognizer_Tapped(System.Object sender, System.EventArgs e)
        {
        }
    }
}