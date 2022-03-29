using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using InterShareMobile.Services.Discovery;
using Plugin.Media;
using Plugin.Media.Abstractions;
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

                App.SmtspReceiver.RegisterTransferRequestCallback(OnTransferRequestCallback);
                App.SmtspReceiver.OnFileReceive += OnFileReceived;

                DeviceInfo.Port = App.SmtspReceiver.Port;
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

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Success", "The file was saved successfully", "Ok");
                });
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", "File size did not match. The sender may have canceled the transfer", "Ok");
                });

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
                    var navigation = new NavigationPage(new SendFilePage(result.FileName, () => result.OpenReadAsync().Result));

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
                List<MediaFile> chosenLibraryPhotos = await CrossMedia.Current.PickPhotosAsync(new PickMediaOptions
                {
                    RotateImage = false,
                    PhotoSize = PhotoSize.Full,
                    CompressionQuality = 100
                }, new MultiPickerOptions()
                {
                    MaximumImagesCount = 1
                });

                if (chosenLibraryPhotos.Any())
                {
                    MediaFile? selectedFile = chosenLibraryPhotos[0];
                    string? fileName = Path.GetFileName(selectedFile.Path);
                    var navigation = new NavigationPage(new SendFilePage(fileName, () => selectedFile.GetStream()));

                    navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                    await Navigation.PushModalAsync(navigation);
                }
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
                List<MediaFile> chosenLibraryVideos = await CrossMedia.Current.PickVideosAsync();

                if (chosenLibraryVideos.Any())
                {
                    MediaFile? selectedFile = chosenLibraryVideos[0];
                    string? fileName = Path.GetFileName(selectedFile.Path);
                    var navigation = new NavigationPage(new SendFilePage(fileName, () => selectedFile.GetStream()));

                    navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                    await Navigation.PushModalAsync(navigation);
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

