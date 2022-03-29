using System;
using InterShareMobile.Core;
using InterShareMobile.Pages;
using SMTSP;
using SMTSP.Core;
using SMTSP.Discovery;
using SMTSP.Discovery.Entities;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Application = Xamarin.Forms.Application;
using NavigationPage = Xamarin.Forms.NavigationPage;

namespace InterShareMobile
{
    public partial class App : Application
    {
        private Advertiser? _advertiser;

        public static SmtspReceiver SmtspReceiver { get; set; }

        public App()
        {
            AppConfig.Initialize();

            // _discoveryService = Ioc.Resolve<IDiscoveryService>();

            Current.Properties.TryGetValue("Name", out object name);
            Current.Properties.TryGetValue("DeviceIdentifier", out object deviceIdentifier);

            if (string.IsNullOrEmpty(deviceIdentifier as string))
            {
                deviceIdentifier = Guid.NewGuid().ToString();
                Current.Properties["DeviceIdentifier"] = deviceIdentifier;
            }

            if (string.IsNullOrEmpty(name as string))
            {
                name = DeviceInfo.Name ?? "Unknown";
                Current.Properties["Name"] = name;
            }

            string deviceType = Device.RuntimePlatform switch
            {
                Device.iOS => Device.Idiom == TargetIdiom.Tablet ? DeviceTypes.AppleTablet : DeviceTypes.ApplePhone,
                Device.Android => Device.Idiom == TargetIdiom.Tablet ? DeviceTypes.AndroidTablet : DeviceTypes.AndroidPhone,
                _ => DeviceTypes.Phone
            };

            AppConfig.MyDeviceInfo = new SMTSP.Entities.DeviceInfo()
            {
                DeviceId = (string) deviceIdentifier,
                DeviceName = (string) name,
                DeviceType = deviceType
            };

            InitializeComponent();
            Start();

            var page = new NavigationPage(new StartPage());
            page.On<iOS>().SetPrefersLargeTitles(true);

            MainPage = page;
        }

        private void Start()
        {
            try
            {
                SmtsConfig.LoggerOutputEnabled = true;
                SmtspReceiver = new SmtspReceiver();
                SmtspReceiver.StartReceiving();
                AppConfig.MyDeviceInfo.Port = ushort.Parse(SmtspReceiver.Port.ToString());
                _advertiser = new Advertiser(AppConfig.MyDeviceInfo);

                _advertiser.Advertise();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnStart()
        {
        }

        protected async override void OnSleep()
        {
            _advertiser?.StopAdvertising();
        }

        protected async override void OnResume()
        {
            _advertiser?.Advertise();
        }
    }
}

