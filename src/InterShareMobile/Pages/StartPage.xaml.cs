using System;
using System.IO;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using InterShareMobile.Services;
using InterShareMobile.Services.Discovery;
using SMTSP.Entities;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using NavigationPage = Xamarin.Forms.NavigationPage;

namespace InterShareMobile.Pages
{
    public partial class StartPage : ContentPage
    {
        private readonly IDiscoveryService _discoveryService;

        public bool InfoVisible { get; set; } = false;

        public MyDeviceInfo DeviceInfo { get; set; } = new MyDeviceInfo();

        public static event EventHandler OnFileArrived = delegate { };

        public StartPage()
        {
            _discoveryService = Ioc.Resolve<IDiscoveryService>();

            DeviceInfo.Loading = false;
            InitializeComponent();
            BindingContext = this;

            Start().RunAndForget();

            Connectivity.ConnectivityChanged += OnConnectivityChanged;
            //
            // var navigation = new NavigationPage(new SendFilePage());
            // navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
            // Navigation.PushModalAsync(navigation);
        }

        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            DeviceInfo.Loading = false;
            DeviceInfo.IpAddress = IpAddress.GetIpAddress();
            DeviceInfo.Loading = true;
        }

        private async Task Start()
        {
            try
            {
                DeviceInfo.Loading = false;

                App.SmtsReceiver.RegisterTransferRequestCallback(OnTransferRequestCallback);
                App.SmtsReceiver.OnFileReceive += OnFileReceived;

                DeviceInfo.Port = App.SmtsReceiver.Port;
                DeviceInfo.IpAddress = IpAddress.GetIpAddress();
                DeviceInfo.Name = AppConfig.MyDeviceInfo.DeviceName;
                DeviceInfo.UserIdentifier = AppConfig.MyDeviceInfo.DeviceId;

                DeviceInfo.Loading = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await DisplayAlert("Error", e.Message, "Ok");
            }
        }

        private async void OnFileReceived(object sender, SmtsFile file)
        {
            var fullPath = $"{AppConfig.DownloadPath}/{file.Name}";

            var count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath)!;
            string newFullPath = fullPath;

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            }

            while(File.Exists(newFullPath))
            {
                var tempFileName = $"{fileNameOnly} ({count++})";
                newFullPath = Path.Combine(path, tempFileName + extension);
            }

            using FileStream fileStream = File.Create(newFullPath);

            await file.DataStream.CopyToAsync(fileStream, 81920);
            file.DataStream.Close();

            if (fileStream.Length == file.FileSize || file.FileSize == -1)
            {
                OnFileArrived.Invoke(this, null);
                await DisplayAlert("Success", "The file was saved successfully", "Ok");
            }
            else
            {
                await DisplayAlert("Error", "File size did not match. The sender may have canceled the transfer", "Ok");

                try
                {
                    File.Delete(newFullPath);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            fileStream.Close();
        }

        private Task<bool> OnTransferRequestCallback(TransferRequest transferRequest)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                bool answer = await DisplayAlert("Received file request", $"{transferRequest.SenderName}\n wants to send you \"{transferRequest.FileName}\"", "Accept", "Deny");

                return answer;
            });
        }

        private async void OnSendFileClicked(object sender, EventArgs e)
        {
            try
            {
                FileResult result = await FilePicker.PickAsync();

                if (result != null)
                {
                    // Stream stream = await result.OpenReadAsync();
                    // long fileSize = stream.Length;

                    var navigation = new NavigationPage(new SendFilePage(result.FileName, result.FullPath));

                    navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                    await Navigation.PushModalAsync(navigation);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                await DisplayAlert("Exception", exception.Message, "OK");
            }
        }

        private async void OnSendPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var directoryService = DependencyService.Get<IMediaPickerService>();
                SelectedFile? result = await directoryService.PickPhotoOrVideo();

                if (result != null)
                {
                    // long fileSize = result.Stream.Length;

                    var navigation = new NavigationPage(new SendFilePage(result.Name, result.Path));

                    navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                    await Navigation.PushModalAsync(navigation);
                }
                // FileResult result = await MediaPicker.PickPhotoAsync();
                //
                // if (result != null)
                // {
                //     Stream stream = await result.OpenReadAsync();
                //     long fileSize = stream.Length;
                //
                //     var navigation = new NavigationPage(new SendFilePage
                //     {
                //         FileName = result.FileName,
                //         FileStream = stream,
                //         FileSize = fileSize
                //     });
                //
                //
                //     navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                //     await Navigation.PushModalAsync(navigation);
                // }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                await DisplayAlert("Exception", exception.Message.ToString(), "OK");
            }
        }

        private async void OnSendVideoClicked(object sender, EventArgs e)
        {
            try
            {
                FileResult result = await MediaPicker.PickVideoAsync();

                if (result != null)
                {
                    // Stream stream = await result.OpenReadAsync();
                    // long fileSize = stream.Length;
                    //
                    // var navigation = new NavigationPage(new SendFilePage(result.FileName, stream, fileSize));

                    // navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                    // await Navigation.PushModalAsync(navigation);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                await DisplayAlert("Exception", exception.Message.ToString(), "OK");
            }
        }
    }
}

