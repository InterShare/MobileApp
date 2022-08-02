using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamEffects.Droid;
using Platform = Xamarin.Essentials.Platform;

namespace InterShareMobile.Droid
{
    [Activity(Label = "InterShare", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : FormsAppCompatActivity
    {
        private WifiManager _wifi;
        private WifiManager.MulticastLock? _multicastLock;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            Platform.Init(this, savedInstanceState);
            Forms.Init(this, savedInstanceState);
            Effects.Init();
            
            _wifi = (WifiManager)ApplicationContext.GetSystemService(WifiService);
            _multicastLock = _wifi.CreateMulticastLock("Zeroconf lock");

            try
            {
                _multicastLock.Acquire();
            }
            catch
            {
                // ignore.
            }

            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        
        protected override void OnDestroy()
        {
            if (_multicastLock != null)
            {
                _multicastLock.Release();
                _multicastLock = null;
            }
            
            base.OnDestroy();
        }
    }
}
