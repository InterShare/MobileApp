using InterShareMobile.Droid.Services;
using InterShareMobile.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(AndroidDirectoryService))]
namespace InterShareMobile.Droid.Services
{
    public class AndroidDirectoryService : IDirectoryService
    {
        public string GetDownloadDirectory()
        {
            return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments)?.AbsolutePath;
        }
    }
}