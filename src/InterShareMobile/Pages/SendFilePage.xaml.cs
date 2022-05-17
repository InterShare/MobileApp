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
using SMTSP.Entities.Content;
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
        private readonly SmtspContent _content;

        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public SendFileBindings Bindings { get; } = new SendFileBindings
        {
            Port = 42420
        };

        public SendFilePage(SmtspContent content)
        {
            _discovery = new Discovery(AppConfig.MyDeviceInfo, DiscoveryTypes.UdpBroadcasts);

            _content = content;

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
            _content?.Dispose();
            _discovery.Dispose();
        }

        private async Task SendFile(string ipAddress, ushort port)
        {
            try
            {
                Bindings.Loading = true;

                SendFileResponses result = await SmtspSender.SendFile(
                        new DeviceInfo()
                        {
                            IpAddress = ipAddress,
                            Port = port
                        },
                        _content,
                        AppConfig.MyDeviceInfo
                    );

                if (result == SendFileResponses.Denied)
                {
                    await DisplayAlert("Declined", "The receiver declined the file", "Ok");
                    Bindings.Loading = false;
                    return;
                }

                _content?.Dispose();
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