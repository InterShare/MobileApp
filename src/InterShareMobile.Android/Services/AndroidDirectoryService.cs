using Android.Content;
using Android.Net;
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

        public void OpenDownloadDirectory()
        {
            Intent intent = new Intent(Intent.ActionGetContent);

            intent.SetDataAndType(Uri.Parse(GetDownloadDirectory()), "file/*");
            //StartActivityForResult(intent, YOUR_RESULT_CODE);

            Android.App.Application.Context.StartActivity(intent);

        }
    }
}