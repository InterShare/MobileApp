using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using SMTSP;
using SMTSP.Discovery;
using SMTSP.Entities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using DeviceInfo = SMTSP.Entities.DeviceInfo;

namespace InterShareMobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SendFilePage : ContentPage
    {
        private readonly Discovery _discovery;
        private readonly Func<Stream> _getStreamCallback;
        private readonly string _fileName;
        private Stream? _fileStream;

        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public SendFileBindings Bindings { get; } = new SendFileBindings
        {
            Port = 42420
        };

        public SendFilePage(string fileName, Func<Stream> getStreamCallback)
        {
            _discovery = new Discovery(AppConfig.MyDeviceInfo);

            _fileName = fileName;
            _getStreamCallback = getStreamCallback;

            try
            {
                // _discovery.DiscoveredDevices.CollectionChanged += DiscoveredDevicesOnCollectionChanged;
                Devices = _discovery.DiscoveredDevices;
                // DiscoveredDevicesOnCollectionChanged(null, null);

                BindingContext = this;
                InitializeComponent();

                _discovery.SendOutLookupSignal();
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void DiscoveredDevicesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs? e)
        {
            // Devices.Clear();
            //
            // foreach (DeviceInfo? device in _discovery.DiscoveredDevices)
            // {
            //     Devices.Add(device);
            // }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _fileStream?.Dispose();
            _discovery.Dispose();
        }

        private async Task SendFile(string ipAddress, ushort port)
        {
            try
            {
                Bindings.Loading = true;

                _fileStream = _getStreamCallback.Invoke();
                long fileSize = _fileStream.Length;

                SendFileResponses result = await SmtspSender.SendFile(
                        new DeviceInfo()
                        {
                            IpAddress = ipAddress,
                            Port = port
                        },
                        new SmtsFile()
                        {
                            Name = _fileName,
                            DataStream = _fileStream,
                            FileSize = fileSize
                        },
                        AppConfig.MyDeviceInfo
                    );

                if (result == SendFileResponses.Denied)
                {
                    await DisplayAlert("Declined", "The receiver declined the file", "Ok");
                    Bindings.Loading = false;
                    return;
                }

                _fileStream?.Dispose();
                await Navigation.PopModalAsync();

            }
            catch(Exception exception)
            {
                await DisplayAlert("Error", exception.Message, "Ok");
            }

            Bindings.Loading = false;
        }

        public async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnDeviceTapped(object sender, DeviceInfo deviceInfo)
        {
            SendFile(deviceInfo.IpAddress, deviceInfo.Port).RunAndForget();
        }

        private void ListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is DeviceInfo deviceInfo)
            {
                SendFile(deviceInfo.IpAddress, deviceInfo.Port).RunAndForget();
            }
        }
    }
}