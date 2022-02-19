using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InterShareMobile.Entities;
using InterShareMobile.iOS.Services;
using InterShareMobile.Services;
using MobileCoreServices;
using PhotosUI;
using UIKit;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(iOSMediaPickerService))]
namespace InterShareMobile.iOS.Services
{
    public class iOSMediaPickerService : IMediaPickerService
    {

        private static UIViewController GetHostViewController()
        {
            UIViewController? viewController = null;
            UIWindow? window = UIApplication.SharedApplication.KeyWindow;

            if (window == null)
                throw new InvalidOperationException("There's no current active window");

            if (window.WindowLevel == UIWindowLevel.Normal)
                viewController = window.RootViewController;

            if (viewController == null)
            {
                window = UIApplication.SharedApplication.Windows.OrderByDescending(w => w.WindowLevel).FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);

                if (window == null)
                {
                    throw new InvalidOperationException("Could not find current view controller");
                }

                viewController = window.RootViewController;
            }

            while (viewController?.PresentedViewController != null)
                viewController = viewController.PresentedViewController;

            return viewController;
        }


        private class PhotoPickerDelegate : PHPickerViewControllerDelegate
        {
            public Action<PHPickerResult[]?> CompletedHandler { get; set; } = delegate { };

            public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results) =>
                CompletedHandler.Invoke(results.Length > 0 ? results : null);
        }

        private static async Task<SelectedFile?> PickerResultsToMediaFile(IEnumerable<PHPickerResult>? results)
        {
            if (results == null)
            {
                return null;
            }

            PHPickerResult file = results.FirstOrDefault()!;

            var taskCompletionSource = new TaskCompletionSource<SelectedFile>();

            file.ItemProvider.LoadFileRepresentation(UTType.Data, (item, error) =>
            {
                if (error != null || item == null) return;

                // NSData data = NSData.FromUrl(item);
                // Stream stream = data.AsStream();
                string fileName = Path.GetFileName(item.Path);

                taskCompletionSource.SetResult(new SelectedFile()
                {
                    Name = fileName,
                    Path = item.Path
                });
            });

            return await taskCompletionSource.Task;
        }

        public async Task<SelectedFile?> PickPhotoOrVideo()
        {
            UIViewController? rootViewController = GetHostViewController();
            var taskCompletionSource = new TaskCompletionSource<SelectedFile?>();

            var config = new PHPickerConfiguration();
            var picker = new PHPickerViewController(config);

            async void CompletedHandler(PHPickerResult[]? phPickerResults) => taskCompletionSource.TrySetResult(await PickerResultsToMediaFile(phPickerResults));

            picker.Delegate = new PhotoPickerDelegate
            {
                CompletedHandler = CompletedHandler
            };

            // if (!string.IsNullOrWhiteSpace(options?.Title))
            //     picker.Title = options?.Title;

            if (DeviceInfo.Idiom == DeviceIdiom.Tablet && picker?.PopoverPresentationController != null && rootViewController?.View != null)
                picker.PopoverPresentationController.SourceRect = rootViewController.View.Bounds;

            if (rootViewController != null)
            {
                await rootViewController.PresentViewControllerAsync(picker!, true);

                SelectedFile? result = await taskCompletionSource.Task;

                await rootViewController.DismissViewControllerAsync(true);

                picker?.Dispose();

                return result;
            }

            return null;
        }
    }
}