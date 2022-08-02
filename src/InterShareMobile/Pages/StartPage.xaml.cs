using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InterShareMobile.Core;
using InterShareMobile.Dto;
using InterShareMobile.Entities;
using InterShareMobile.Helper;
using InterShareMobile.Services;
using InterShareMobile.Services.Discovery;
using Plugin.Media;
using Plugin.Media.Abstractions;
using SMTSP.Entities;
using SMTSP.Entities.Content;
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
                App.SmtspReceiver.OnContentReceive += OnContentReceived;

                DeviceInfo.Port = AppConfig.MyDeviceInfo.TcpPort;
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

        private async void OnContentReceived(object sender, SmtspContentBase content)
        {
            if (content is SmtspFileContent fileContent)
            {
                await HandleFileReceived(fileContent);
            }
            else if (content is SmtspClipboardContent clipboardContent)
            {
                await HandleClipboardReceived(clipboardContent);
            }
        }

        private async Task HandleClipboardReceived(SmtspContentBase content)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                using var sr = new StreamReader(content.DataStream!);
                string? receivedText = await sr.ReadToEndAsync();
                await Clipboard.SetTextAsync(receivedText);
            });
        }

        private async Task HandleFileReceived(SmtspFileContent file)
        {
            var fullPath = $"{AppConfig.DownloadPath}/{file.FileName}";

            var count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath)!;
            string newFullPath = fullPath;

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            }

            while (File.Exists(newFullPath))
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
                bool answer = false;

                if (transferRequest.ContentBase is SmtspFileContent fileContent)
                {
                    answer = await DisplayAlert("Received file request", $"{transferRequest.SenderName}\n wants to send you \"{fileContent.FileName}\"", "Accept", "Deny");
                }
                else
                {
                    answer = await DisplayAlert("Received clipboard request", $"{transferRequest.SenderName}\n wants to share the clipboard", "Accept", "Deny");
                }

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
                    var fileStream = await result.OpenReadAsync();

                    var content = new SmtspFileContent()
                    {
                        FileName = result.FileName,
                        DataStream = fileStream,
                        FileSize = fileStream.Length
                    };

                    var navigation = new NavigationPage(new SendFilePage(content));

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
                    var fileStream = selectedFile.GetStream();

                    var content = new SmtspFileContent()
                    {
                        FileName = fileName,
                        DataStream = fileStream,
                        FileSize = fileStream.Length
                    };

                    var navigation = new NavigationPage(new SendFilePage(content));

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
                    var fileStream = selectedFile.GetStream();

                    var content = new SmtspFileContent()
                    {
                        FileName = fileName,
                        DataStream = fileStream,
                        FileSize = fileStream.Length
                    };

                    var navigation = new NavigationPage(new SendFilePage(content));

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

        private async void OnSendClipboardClicked(object sender, EventArgs e)
        {
            try
            {
                string? clipboard = Clipboard.HasText ? await Clipboard.GetTextAsync() : null;

                if (clipboard == null)
                {
                    await DisplayAlert("Cannot send", "Clipboard is empty", "Ok");
                    return;
                }

                var clipboardStream = new MemoryStream(Encoding.UTF8.GetBytes(clipboard));

                var content = new SmtspClipboardContent
                {
                    DataStream = clipboardStream
                };

                var navigation = new NavigationPage(new SendFilePage(content));

                navigation.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
                await Navigation.PushModalAsync(navigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await DisplayAlert("Exception", ex.Message, "Ok");
            }
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
        }

        private void OnShowDownloadedFilesClicked(object sender, EventArgs e)
        {
            var directoryService = DependencyService.Get<IDirectoryService>();
            directoryService.OpenDownloadDirectory();
        }
    }
}

