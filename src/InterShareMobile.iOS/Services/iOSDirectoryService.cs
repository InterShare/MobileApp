using System;
using System.IO;
using InterShareMobile.iOS.Services;
using InterShareMobile.Services;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(iOSDirectoryService))]
namespace InterShareMobile.iOS.Services
{
    public class iOSDirectoryService : IDirectoryService
    {
        public string GetDownloadDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public void OpenDownloadDirectory()
        {
            Launcher.OpenAsync($"shareddocuments://{GetDownloadDirectory()}");
        }
    }
}