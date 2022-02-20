using System;
using InterShareMobile.Core;
using InterShareMobile.Pages;
using InterShareMobile.Services.Discovery;
using SMTSP;
using SMTSP.Core;
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
        private readonly IDiscoveryService _discoveryService;

        public static SmtsReceiver SmtsReceiver { get; set; }

        public App()
        {
            AppConfig.Initialize();

            _discoveryService = Ioc.Resolve<IDiscoveryService>();

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

            AppConfig.MyDeviceInfo = new DiscoveryDeviceInfo()
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

                SmtsReceiver = new SmtsReceiver();
                SmtsReceiver.StartReceiving();

                AppConfig.MyDeviceInfo.TransferPort = SmtsReceiver.Port;

                _discoveryService.Setup(AppConfig.MyDeviceInfo);
                _discoveryService.RegisterForDiscovery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            _discoveryService.StopDiscovering();
        }

        protected override void OnResume()
        {
            _discoveryService.RegisterForDiscovery();
            _discoveryService.BroadcastMyDevice();
        }
    }
}

