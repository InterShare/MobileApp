using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using InterShareMobile.Core;
using InterShareMobile.Entities;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FilesPage : ContentPage
    {
        public ObservableCollection<TransferredFile> Files { get; set; } = new ObservableCollection<TransferredFile>();

        public FilesPage()
        {
            GetFiles();
            InitializeComponent();
            BindingContext = this;
            StartPage.OnFileArrived += StartPageOnOnFileArrived;
        }

        private void StartPageOnOnFileArrived(object sender, EventArgs e)
        {
            GetFiles();
        }

        protected override void OnAppearing()
        {
            GetFiles();
        }

        private void GetFiles()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(AppConfig.DownloadPath);

                var files = directoryInfo.GetFiles();

                foreach (FileInfo file in files.OrderBy(file => file.CreationTime).Where(file => Files.FirstOrDefault(f => f.Path == file.FullName) == null))
                {
                    Files.Insert(0, new TransferredFile()
                    {
                        Path = file.FullName,
                        Name = Path.GetFileName(file.Name)
                    });
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            var menuItem = ((MenuItem)sender);

            if (menuItem?.CommandParameter is TransferredFile file)
            {
                File.Delete(file.Path);
                Files.Remove(file);
            }
        }

        private async void OnItemTap(object sender, ItemTappedEventArgs itemTappedEventArgs)
        {
            if (itemTappedEventArgs?.Item == null)
            {
                return;
            }

            var file = ((TransferredFile)itemTappedEventArgs.Item);

            await Share.RequestAsync(new ShareFileRequest()
            {
                Title = file.Name,
                File = new ShareFile(file.Path)
            });
        }
    }
}