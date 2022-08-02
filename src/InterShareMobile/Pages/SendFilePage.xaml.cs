using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using SMTSP;
using SMTSP.Entities;
using SMTSP.Entities.Content;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SendFilePage : ContentPage
    {
        private readonly Func<Stream> _getStreamCallback;
        private readonly SmtspContentBase _content;

        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public SendFileBindings Bindings { get; } = new SendFileBindings
        {
            Port = 42420
        };

        public SendFilePage(SmtspContentBase content)
        {

            _content = content;

            try
            {
                // _discovery.DiscoveredDevices.CollectionChanged += DiscoveredDevicesOnCollectionChanged;
                Devices = App.DeviceDiscovery.DiscoveredDevices;
                // DiscoveredDevicesOnCollectionChanged(null, null);

                BindingContext = this;
                InitializeComponent();

                App.DeviceDiscovery.StartDiscovering();
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
            App.DeviceDiscovery.StopDiscovering();
        }

        private async Task SendFile(DeviceInfo device)
        {
            try
            {
                Bindings.Loading = true;

                SendResponses result = await SmtspSender.Send(
                        device,
                        _content,
                        AppConfig.MyDeviceInfo
                    );

                if (result == SendResponses.Denied)
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
            SendFile(deviceInfo).RunAndForget();
        }

        private void ListView_OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is DeviceInfo deviceInfo)
            {
                SendFile(deviceInfo).RunAndForget();
            }
        }
    }
}