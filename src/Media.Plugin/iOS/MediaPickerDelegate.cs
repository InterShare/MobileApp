using System;
using System.IO;
using System.Threading.Tasks;

using Plugin.Media.Abstractions;

using CoreGraphics;
using AssetsLibrary;
using Foundation;
using UIKit;
using NSAction = System.Action;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using CoreImage;

namespace Plugin.Media
{
    class MediaPickerDelegate : UIImagePickerControllerDelegate
    {
        internal MediaPickerDelegate(UIViewController viewController, UIImagePickerControllerSourceType sourceType, StoreCameraMediaOptions options, CancellationToken token)
        {
            this.viewController = viewController;
            source = sourceType;
            this.options = options ?? new StoreCameraMediaOptions();

            if (viewController != null)
            {
                UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
                observer = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, DidRotate);
            }
        }

        public UIPopoverController Popover
        {
            get;
            set;
        }

        public void CancelTask() => tcs.TrySetResult(null);

        public UIView View => viewController.View;

        public Task<List<MediaFile>> Task => tcs.Task;

        public override async void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            RemoveOrientationChangeObserverAndNotifications();

            MediaFile mediaFile;
            switch ((NSString)info[UIImagePickerController.MediaType])
            {
                case MediaImplementation.TypeImage:
                    mediaFile = await GetPictureMediaFile(info);
                    break;

                case MediaImplementation.TypeMovie:
                    mediaFile = await GetMovieMediaFile(info);
                    break;

                default:
                    throw new NotSupportedException();
            }

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
            }

            Dismiss(picker, () =>
            {
                if (mediaFile == null)
                    tcs.SetException(new FileNotFoundException());
                else
                    tcs.TrySetResult(new List<MediaFile> { mediaFile });
            });
        }

        public void Canceled(UINavigationController picker)
        {
            RemoveOrientationChangeObserverAndNotifications();

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
            }

            Dismiss(picker, () =>
            {
                tcs.TrySetResult(null);
            });
        }

        public override void Canceled(UIImagePickerController picker)
        {
            RemoveOrientationChangeObserverAndNotifications();

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
            }

            Dismiss(picker, () =>
            {
                tcs.TrySetResult(null);
            });
        }

        public void DisplayPopover(bool hideFirst = false)
        {
            if (Popover == null)
                return;

            nfloat swidth = UIScreen.MainScreen.Bounds.Width;
            nfloat sheight = UIScreen.MainScreen.Bounds.Height;

            nfloat width = 400;
            nfloat height = 300;


            if (orientation == null)
            {
                if (IsValidInterfaceOrientation(UIDevice.CurrentDevice.Orientation))
                    orientation = UIDevice.CurrentDevice.Orientation;
                else
                    orientation = GetDeviceOrientation(viewController.InterfaceOrientation);
            }

            double x, y;
            if (orientation == UIDeviceOrientation.LandscapeLeft || orientation == UIDeviceOrientation.LandscapeRight)
            {
                x = (Math.Max(swidth, sheight) - width) / 2;
                y = (Math.Min(swidth, sheight) - height) / 2;
            }
            else
            {
                x = (Math.Min(swidth, sheight) - width) / 2;
                y = (Math.Max(swidth, sheight) - height) / 2;
            }

            if (hideFirst && Popover.PopoverVisible)
                Popover.Dismiss(animated: false);

            Popover.PresentFromRect(new CGRect(x, y, width, height), View, 0, animated: true);
        }

        UIDeviceOrientation? orientation;
        NSObject observer;
        readonly UIViewController viewController;
        readonly UIImagePickerControllerSourceType source;
        TaskCompletionSource<List<MediaFile>> tcs = new TaskCompletionSource<List<MediaFile>>();
        readonly StoreCameraMediaOptions options;

        bool IsCaptured =>
            source == UIImagePickerControllerSourceType.Camera;

        void Dismiss(UINavigationController picker, NSAction onDismiss)
        {
            if (viewController == null)
            {
                onDismiss();
                tcs = new TaskCompletionSource<List<MediaFile>>();
            }
            else
            {
                if (Popover != null)
                {
                    Popover.Dismiss(animated: true);
                    try
                    {
                        Popover.Dispose();
                    }
                    catch
                    {

                    }
                    Popover = null;

                    onDismiss();
                }
                else
                {
                    picker.DismissViewController(true, onDismiss);
                }
            }
        }

        void RemoveOrientationChangeObserverAndNotifications()
        {
            if (viewController != null)
            {
                UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
                NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
                observer.Dispose();
            }
        }

        void DidRotate(NSNotification notice)
        {
            var device = (UIDevice)notice.Object;
            if (!IsValidInterfaceOrientation(device.Orientation) || Popover == null)
                return;
            if (orientation.HasValue && IsSameOrientationKind(orientation.Value, device.Orientation))
                return;

            if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
            {
                if (!GetShouldRotate6(device.Orientation))
                    return;
            }
            else if (!GetShouldRotate(device.Orientation))
                return;

            var co = orientation;
            orientation = device.Orientation;

            if (co == null)
                return;

            DisplayPopover(hideFirst: true);
        }

        bool GetShouldRotate(UIDeviceOrientation orientation)
        {
            UIInterfaceOrientation iOrientation;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    iOrientation = UIInterfaceOrientation.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    iOrientation = UIInterfaceOrientation.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    iOrientation = UIInterfaceOrientation.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    iOrientation = UIInterfaceOrientation.PortraitUpsideDown;
                    break;

                default: return false;
            }

            return viewController.ShouldAutorotateToInterfaceOrientation(iOrientation);
        }

        bool GetShouldRotate6(UIDeviceOrientation orientation)
        {
            if (!viewController.ShouldAutorotate())
                return false;

            UIInterfaceOrientationMask mask;
            switch (orientation)
            {
                case UIDeviceOrientation.LandscapeLeft:
                    mask = UIInterfaceOrientationMask.LandscapeLeft;
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    mask = UIInterfaceOrientationMask.LandscapeRight;
                    break;

                case UIDeviceOrientation.Portrait:
                    mask = UIInterfaceOrientationMask.Portrait;
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    mask = UIInterfaceOrientationMask.PortraitUpsideDown;
                    break;

                default: return false;
            }

            return viewController.GetSupportedInterfaceOrientations().HasFlag(mask);
        }


        async Task<MediaFile> GetPictureMediaFile(NSDictionary info)
        {
            UIImage image = (UIImage)info[UIImagePickerController.EditedImage] ?? (UIImage)info[UIImagePickerController.OriginalImage];

            if (image == null)
                return null;

            string pathExtension = ((info[UIImagePickerController.ReferenceUrl] as NSUrl)?.PathExtension == "PNG") ? "png" : "jpg";

            string path = GetOutputPath(MediaImplementation.TypeImage,
                options.Directory ?? ((IsCaptured) ? string.Empty : "temp"),
                options.Name, pathExtension);

            CGImage cgImage = image.CGImage;

            var percent = 1.0f;
            float newHeight = image.CGImage.Height;
            float newWidth = image.CGImage.Width;

            if (options.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    switch (options.PhotoSize)
                    {
                        case PhotoSize.Large:
                            percent = .75f;
                            break;
                        case PhotoSize.Medium:
                            percent = .5f;
                            break;
                        case PhotoSize.Small:
                            percent = .25f;
                            break;
                        case PhotoSize.Custom:
                            percent = (float)options.CustomPhotoSize / 100f;
                            break;
                    }

                    if (options.PhotoSize == PhotoSize.MaxWidthHeight && options.MaxWidthHeight.HasValue)
                    {
                        long max = Math.Max(image.CGImage.Width, image.CGImage.Height);
                        if (max > options.MaxWidthHeight.Value)
                        {
                            percent = (float)options.MaxWidthHeight.Value / (float)max;
                        }
                    }

                    if (percent < 1.0f)
                    {
                        //begin resizing image
                        image = image.ResizeImageWithAspectRatio(percent);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to compress image: {ex}");
                }
            }


            NSDictionary meta = null;
            try
            {
                if (options.SaveMetaData)
                {
                    if (source == UIImagePickerControllerSourceType.Camera)
                    {
                        meta = info[UIImagePickerController.MediaMetadata] as NSDictionary;
                        if (meta != null && meta.ContainsKey(ImageIO.CGImageProperties.Orientation))
                        {
                            var newMeta = new NSMutableDictionary();
                            newMeta.SetValuesForKeysWithDictionary(meta);
                            var newTiffDict = new NSMutableDictionary();
                            newTiffDict.SetValuesForKeysWithDictionary(meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary);
                            newTiffDict.SetValueForKey(meta[ImageIO.CGImageProperties.Orientation], ImageIO.CGImageProperties.TIFFOrientation);
                            newMeta[ImageIO.CGImageProperties.TIFFDictionary] = newTiffDict;

                            meta = newMeta;
                        }
                        Location location = options.Location;
                        if (meta != null && location != null)
                        {
                            meta = SetGpsLocation(meta, location);
                        }
                    }
                    else
                    {
                        var url = info[UIImagePickerController.ReferenceUrl] as NSUrl;
                        if(url != null)
                            meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(url);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to get metadata: {ex}");
            }

            //iOS quality is 0.0-1.0
            float quality = pathExtension == "jpg" ? (options.CompressionQuality / 100f) : 0f;
            var savedImage = false;
            if (meta != null)
                savedImage = SaveImageWithMetadata(image, quality, meta, path, pathExtension);

            if (!savedImage)
            {
                float finalQuality = quality;
                NSData imageData = pathExtension == "png" ? image.AsPNG() : image.AsJPEG(finalQuality);

                //continue to move down quality , rare instances
                while (imageData == null && finalQuality > 0)
                {
                    finalQuality -= 0.05f;
                    imageData = image.AsJPEG(finalQuality);
                }

                if (imageData == null)
                    throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");


                imageData.Save(path, true);
                imageData.Dispose();

            }


            string aPath = null;
            if (source != UIImagePickerControllerSourceType.Camera)
            {

                //try to get the album path's url
                var url = (NSUrl)info[UIImagePickerController.ReferenceUrl];
                aPath = url?.AbsoluteString;
            }
            else
            {
                if (options.SaveToAlbum)
                {
                    try
                    {
                        var library = new ALAssetsLibrary();
                        NSUrl albumSave = await library.WriteImageToSavedPhotosAlbumAsync(cgImage, meta);
                        aPath = albumSave.AbsoluteString;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("unable to save to album:" + ex);
                    }
                }

            }

            Func<Stream> getStreamForExternalStorage = () =>
            {
                if (options.RotateImage)
                    return RotateImage(image, options.CompressionQuality, pathExtension);
                else
                    return File.OpenRead(path);
            };

            return new MediaFile(path, () => File.OpenRead(path), streamGetterForExternalStorage: () => getStreamForExternalStorage(), albumPath: aPath);
        }

        internal static NSDictionary SetGpsLocation(NSDictionary meta, Location location)
        {
            var newMeta = new NSMutableDictionary();
            newMeta.SetValuesForKeysWithDictionary(meta);
            var newGpsDict = new NSMutableDictionary();
            newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Latitude)), ImageIO.CGImageProperties.GPSLatitude);
            newGpsDict.SetValueForKey(new NSString(location.Latitude > 0 ? "N" : "S"), ImageIO.CGImageProperties.GPSLatitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Longitude)), ImageIO.CGImageProperties.GPSLongitude);
            newGpsDict.SetValueForKey(new NSString(location.Longitude > 0 ? "E" : "W"), ImageIO.CGImageProperties.GPSLongitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Altitude), ImageIO.CGImageProperties.GPSAltitude);
            newGpsDict.SetValueForKey(new NSNumber(0), ImageIO.CGImageProperties.GPSAltitudeRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Speed), ImageIO.CGImageProperties.GPSSpeed);
            newGpsDict.SetValueForKey(new NSString("K"), ImageIO.CGImageProperties.GPSSpeedRef);
            newGpsDict.SetValueForKey(new NSNumber(location.Direction), ImageIO.CGImageProperties.GPSImgDirection);
            newGpsDict.SetValueForKey(new NSString("T"), ImageIO.CGImageProperties.GPSImgDirectionRef);
            newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("hh:mm:ss")), ImageIO.CGImageProperties.GPSTimeStamp);
            newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("yyyy:MM:dd")), ImageIO.CGImageProperties.GPSDateStamp);
            newMeta[ImageIO.CGImageProperties.GPSDictionary] = newGpsDict;
            return newMeta;
        }


        internal static bool SaveImageWithMetadata(UIImage image, float quality, NSDictionary meta, string path, string pathExtension)
        {
            try
            {
                pathExtension = pathExtension.ToLowerInvariant();
                float finalQuality = quality;
                NSData imageData = pathExtension == "png" ? image.AsPNG(): image.AsJPEG(finalQuality);

                //continue to move down quality , rare instances
                while (imageData == null && finalQuality > 0)
                {
                    finalQuality -= 0.05f;
                    imageData = image.AsJPEG(finalQuality);
                }

                if (imageData == null)
                    throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");

                // Copy over meta data
                using CIImage ciImage = CIImage.FromData(imageData);
                using CIImage newImageSource = ciImage.CreateBySettingProperties(meta);
                using var ciContext = new CIContext();

                if (pathExtension == "png")
                    return ciContext.WritePngRepresentation(newImageSource, NSUrl.FromFilename(path), CIFormat.ARGB8, CGColorSpace.CreateSrgb(), new NSDictionary(), out NSError error2);

                return ciContext.WriteJpegRepresentation(newImageSource, NSUrl.FromFilename(path), CGColorSpace.CreateSrgb(), new NSDictionary(), out NSError error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to save image with metadata: {ex}");
            }

            return false;
        }

        async Task<MediaFile> GetMovieMediaFile(NSDictionary info)
        {
            var url = info[UIImagePickerController.MediaURL] as NSUrl;
            if (url == null)
                return null;

            string path = GetOutputPath(MediaImplementation.TypeMovie,
                      options?.Directory ?? ((IsCaptured) ? string.Empty : "temp"),
                      options?.Name ?? Path.GetFileName(url.Path), url.PathExtension);

            try
            {
                File.Move(url.Path, path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to move file, trying to copy. {ex.Message}");
                try
                {
                    File.Copy(url.Path, path);
                    File.Delete(url.Path);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Unable to copy/delete file, will be left around :( {ex.Message}");
                }
            }

            string aPath = null;
            if (source != UIImagePickerControllerSourceType.Camera)
            {
                //try to get the album path's url
                var url2 = info[UIImagePickerController.ReferenceUrl] as NSUrl;
                aPath = url2?.AbsoluteString;
            }
            else
            {
                if (options?.SaveToAlbum ?? false)
                {
                    try
                    {
                        var library = new ALAssetsLibrary();
                        NSUrl albumSave = await library.WriteVideoToSavedPhotosAlbumAsync(new NSUrl(path));
                        aPath = albumSave.AbsoluteString;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("unable to save to album:" + ex);
                    }
                }
            }

            return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath);
        }

        static string GetUniquePath(string type, string path, string name, string pathExtension)
        {
            bool isPhoto = (type == MediaImplementation.TypeImage);
            string ext = Path.GetExtension(name);
            if (string.IsNullOrWhiteSpace(ext))
                ext = "." + pathExtension;
            if(string.IsNullOrWhiteSpace(ext))
                ext = ((isPhoto) ? ".jpg" : ".mp4");

            name = Path.GetFileNameWithoutExtension(name);

            string nname = name + ext;
            var i = 1;
            while (File.Exists(Path.Combine(path, nname)))
                nname = name + "_" + (i++) + ext;

            return Path.Combine(path, nname);
        }

        internal static string GetOutputPath(string type, string path, string name, string extension, long index = 0)
        {
            extension = extension.ToLowerInvariant();
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path);
            Directory.CreateDirectory(path);

            long epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string postpendName = index == 0 ? string.Empty : $"{index}";
            postpendName += Math.Abs(epoch);
            if (string.IsNullOrWhiteSpace(name))
            {
                if (type == MediaImplementation.TypeImage)
                    name = extension == "png" ? $"IMG_{postpendName}.png" : $"IMG_{postpendName}.jpg";
                else
                    name = $"VID_{postpendName}.{extension ?? "mp4"}";
            }
            else
            {
                string[] namePart = name.Split(".");
                name = $"{namePart[0]}_{postpendName}";
                if(namePart.Length > 1)
                {
                    name = name + namePart[1];
                }
            }

            return Path.Combine(path, GetUniquePath(type, path, name, extension));
        }

        static bool IsValidInterfaceOrientation(UIDeviceOrientation self)
        {
            return (self != UIDeviceOrientation.FaceUp && self != UIDeviceOrientation.FaceDown && self != UIDeviceOrientation.Unknown);
        }

        static bool IsSameOrientationKind(UIDeviceOrientation o1, UIDeviceOrientation o2)
        {
            if (o1 == UIDeviceOrientation.FaceDown || o1 == UIDeviceOrientation.FaceUp)
                return (o2 == UIDeviceOrientation.FaceDown || o2 == UIDeviceOrientation.FaceUp);
            if (o1 == UIDeviceOrientation.LandscapeLeft || o1 == UIDeviceOrientation.LandscapeRight)
                return (o2 == UIDeviceOrientation.LandscapeLeft || o2 == UIDeviceOrientation.LandscapeRight);
            if (o1 == UIDeviceOrientation.Portrait || o1 == UIDeviceOrientation.PortraitUpsideDown)
                return (o2 == UIDeviceOrientation.Portrait || o2 == UIDeviceOrientation.PortraitUpsideDown);

            return false;
        }

        static UIDeviceOrientation GetDeviceOrientation(UIInterfaceOrientation self)
        {
            switch (self)
            {
                case UIInterfaceOrientation.LandscapeLeft:
                    return UIDeviceOrientation.LandscapeLeft;
                case UIInterfaceOrientation.LandscapeRight:
                    return UIDeviceOrientation.LandscapeRight;
                case UIInterfaceOrientation.Portrait:
                    return UIDeviceOrientation.Portrait;
                case UIInterfaceOrientation.PortraitUpsideDown:
                    return UIDeviceOrientation.PortraitUpsideDown;
                default:
                    throw new InvalidOperationException();
            }
        }

        public static Stream RotateImage(UIImage image, int compressionQuality, string pathExtension)
        {
            UIImage imageToReturn = null;
            if (image.Orientation == UIImageOrientation.Up)
            {
                imageToReturn = image;
            }
            else
            {
                var transform = CGAffineTransform.MakeIdentity();

                switch (image.Orientation)
                {
                    case UIImageOrientation.Down:
                    case UIImageOrientation.DownMirrored:
                        transform.Rotate((float)Math.PI);
                        transform.Translate(image.Size.Width, image.Size.Height);
                        break;

                    case UIImageOrientation.Left:
                    case UIImageOrientation.LeftMirrored:
                        transform.Rotate((float)Math.PI / 2);
                        transform.Translate(image.Size.Width, 0);
                        break;

                    case UIImageOrientation.Right:
                    case UIImageOrientation.RightMirrored:
                        transform.Rotate(-(float)Math.PI / 2);
                        transform.Translate(0, image.Size.Height);
                        break;
                    case UIImageOrientation.Up:
                    case UIImageOrientation.UpMirrored:
                        break;
                }

                switch (image.Orientation)
                {
                    case UIImageOrientation.UpMirrored:
                    case UIImageOrientation.DownMirrored:
                        transform.Translate(image.Size.Width, 0);
                        transform.Scale(-1, 1);
                        break;

                    case UIImageOrientation.LeftMirrored:
                    case UIImageOrientation.RightMirrored:
                        transform.Translate(image.Size.Height, 0);
                        transform.Scale(-1, 1);
                        break;
                    case UIImageOrientation.Up:
                    case UIImageOrientation.Down:
                    case UIImageOrientation.Left:
                    case UIImageOrientation.Right:
                        break;
                }

                using var context = new CGBitmapContext(IntPtr.Zero,
                                                        (int)image.Size.Width,
                                                        (int)image.Size.Height,
                                                        image.CGImage.BitsPerComponent,
                                                        image.CGImage.BytesPerRow,
                                                        image.CGImage.ColorSpace,
                                                        image.CGImage.BitmapInfo);
                context.ConcatCTM(transform);
                switch (image.Orientation)
                {
                    case UIImageOrientation.Left:
                    case UIImageOrientation.LeftMirrored:
                    case UIImageOrientation.Right:
                    case UIImageOrientation.RightMirrored:
                        context.DrawImage(new CGRect(0, 0, image.Size.Height, image.Size.Width), image.CGImage);
                        break;
                    default:
                        context.DrawImage(new CGRect(0, 0, image.Size.Width, image.Size.Height), image.CGImage);
                        break;
                }

                using CGImage imageRef = context.ToImage();
                imageToReturn = new UIImage(imageRef, 1, UIImageOrientation.Up);
            }

            pathExtension = pathExtension.ToLowerInvariant();
            float finalQuality = pathExtension == "jpg" ? (compressionQuality / 100f) : 0f;
            NSData imageData = pathExtension == "png" ? imageToReturn.AsPNG() : imageToReturn.AsJPEG(finalQuality);
            //continue to move down quality , rare instances
            while (imageData == null && finalQuality > 0)
            {
                finalQuality -= 0.05f;
                imageData = imageToReturn.AsJPEG(finalQuality);
            }

            if (imageData == null)
                throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");

            var stream = new MemoryStream();
            imageData.AsStream().CopyTo(stream);
            stream.Position = 0;
            imageData.Dispose();
            image.Dispose();
            image = null;
            return stream;

        }
    }
}
