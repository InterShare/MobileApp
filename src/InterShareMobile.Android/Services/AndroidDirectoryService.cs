using Android.Content;
using Android.Net;
using InterShareMobile.Droid.Services;
using InterShareMobile.Services;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;

[assembly: Dependency(typeof(AndroidDirectoryService))]
namespace InterShareMobile.Droid.Services
{
    public class AndroidDirectoryService : IDirectoryService
    {
        public string GetDownloadDirectory()
        {
            return System.IO.Path.Join(Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath, "InterShare") ?? "";
        }

        public void OpenDownloadDirectory()
        {
            // Intent intent = new Intent(Intent.ActionGetContent);
            Uri uri = Uri.Parse(GetDownloadDirectory());
            Intent intent = new Intent(Intent.ActionGetContent);
            intent.SetDataAndType(uri, "*/*");
            intent.SetFlags(ActivityFlags.NewTask);
            // StartActivity(intent);
            
            // intent.SetDataAndType(Uri.Parse(GetDownloadDirectory()), "file/*");
            //StartActivityForResult(intent, YOUR_RESULT_CODE);

            Android.App.Application.Context.StartActivity(intent);

        }
    }
}