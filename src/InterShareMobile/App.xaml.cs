using System;
using InterShareMobile.Core;
using InterShareMobile.Pages;
using SMTSP;
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
        public static DeviceDiscovery DeviceDiscovery = null!;

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

            AppConfig.MyDeviceInfo = new SMTSP.Entities.DeviceInfo(
                deviceId: (string) deviceIdentifier,
                deviceName: (string) name,
                deviceType: deviceType,
                capabilities: new[] {"InterShare"}
            );

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
                SmtspReceiver = new SmtspReceiver(AppConfig.MyDeviceInfo);
                SmtspReceiver.StartReceiving();
                
                DeviceDiscovery = new DeviceDiscovery(AppConfig.MyDeviceInfo);
                DeviceDiscovery.Advertise();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnStart()
        {
        }

        protected override async void OnSleep()
        {
            DeviceDiscovery?.StopAdvertising();
        }

        protected override async void OnResume()
        {
            DeviceDiscovery?.Advertise();
        }
    }
}

