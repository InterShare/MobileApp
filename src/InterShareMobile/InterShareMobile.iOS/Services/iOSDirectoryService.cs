using System;
using InterShareMobile.iOS.Services;
using InterShareMobile.Services;

[assembly: Xamarin.Forms.Dependency(typeof(iOSDirectoryService))]
namespace InterShareMobile.iOS.Services
{
    public class iOSDirectoryService : IDirectoryService
    {
        public string GetDownloadDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}