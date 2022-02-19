using InterShareMobile.iOS.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(NativeNavigationBarRenderer))]
namespace InterShareMobile.iOS.Renderers
{
    public class NativeNavigationBarRenderer : NavigationRenderer
    {
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationBar.StandardAppearance.ConfigureWithDefaultBackground();
            NavigationBar.CompactAppearance?.ConfigureWithDefaultBackground();
            NavigationBar.ScrollEdgeAppearance?.ConfigureWithTransparentBackground();
            NavigationBar.Translucent = true;
        }
    }
}