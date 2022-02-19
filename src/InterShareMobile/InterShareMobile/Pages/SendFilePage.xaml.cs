using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using InterShareMobile.Services.Discovery;
using SMTSP;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using DeviceInfo = SMTSP.Entities.DeviceInfo;

namespace InterShareMobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SendFilePage : ContentPage
    {
        private readonly IDiscoveryService _discoveryService;

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public Stream? FileStream { get; set; }
        // public long FileSize { get; set; }

        public ObservableCollection<DiscoveryDeviceInfo> Devices { get; set; } = new ObservableCollection<DiscoveryDeviceInfo>();

        public SendFileData SendFileData { get; set; } = new SendFileData()
        {
            Port = 42420
        };

        public SendFilePage(string fileName, string filePath)
        {
            _discoveryService = Ioc.Resolve<IDiscoveryService>();

            FileName = fileName;
            FilePath = filePath;
            // FileStream = fileStream;
            // FileSize = fileSize;

            try
            {
                BindingContext = this;
                InitializeComponent();

                _discoveryService.OnNewDeviceDiscovered += OnDiscoveredDevicesChange;
                _discoveryService.StartDiscovering();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            FileStream?.Dispose();
            _discoveryService.StopDiscovering();
        }

        private void OnDiscoveredDevicesChange(object sender, DiscoveryDeviceInfo deviceInfo)
        {
            Devices.Add(deviceInfo);
        }

        private async Task SendFile(string ipAddress, int port)
        {
            try
            {
                SendFileData.Loading = true;

                FileStream = File.OpenRead(FilePath);
                long fileSize = FileStream.Length;

                SendFileResponses result = await SmtsSender.SendFile(new DeviceInfo()
                    {
                        LanInfo = new LanDeviceInfo()
                        {
                            Ip = ipAddress,
                            FileServerPort = port
                        }
                    },
                    new SmtsFile()
                    {
                        Name = FileName,
                        DataStream = FileStream,
                        FileSize = fileSize
                    }, AppConfig.MyDeviceInfo);

                if (result == SendFileResponses.Denied)
                {
                    await DisplayAlert("Declined", "The receiver declined the file", "Ok");
                    SendFileData.Loading = false;
                    return;
                }

                await Navigation.PopModalAsync();

            }
            catch(Exception exception)
            {
                await DisplayAlert("Error", exception.Message, "Ok");
            }

            _discoveryService.StopDiscovering();
            SendFileData.Loading = false;
        }

        public async void OnCloseClicked(object sender, EventArgs e)
        {
            _discoveryService.StopDiscovering();
            await Navigation.PopModalAsync();
        }

        private void OnDeviceTapped(object sender, DiscoveryDeviceInfo deviceInfo)
        {
            SendFile(deviceInfo.IpAddress, deviceInfo.TransferPort).RunAndForget();
        }
    }
}