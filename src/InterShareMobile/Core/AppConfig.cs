using InterShareMobile.Services;
using InterShareMobile.Services.Discovery;
using SMTSP.Entities;
using Xamarin.Forms;

namespace InterShareMobile.Core
{
    public class AppConfig
    {
        public static string? DownloadPath { get; set; }
        public static DeviceInfo MyDeviceInfo { get; set; }

        public static void Initialize()
        {
            var directoryService = DependencyService.Get<IDirectoryService>();

            DownloadPath ??= directoryService.GetDownloadDirectory();

            Ioc.Register<IDiscoveryService, DiscoveryService>();
        }
    }
}