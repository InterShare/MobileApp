using System;
using CoreImage;
using Foundation;
using Photos;
using UIKit;

namespace Plugin.Media
{
    /// <summary>
    /// Accesst library
    /// </summary>
    public static class PhotoLibraryAccess
    {
        /// <summary>
        /// Fetch image metadata from a photos Library path
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static NSDictionary GetPhotoLibraryMetadata(NSUrl url)
        {
            NSDictionary meta = null;

            var image = PHAsset.FetchAssets(new NSUrl[] { url }, new PHFetchOptions()).firstObject as PHAsset;
            if (image == null)
                return null;

            try
            {
                var imageManager = PHImageManager.DefaultManager;
                var requestOptions = new PHImageRequestOptions
                {
                    Synchronous = true,
                    NetworkAccessAllowed = true,
                    DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
                };

                if(UIDevice.CurrentDevice.CheckSystemVersion(13,0))
                {
                    imageManager.RequestImageDataAndOrientation(image, requestOptions, (data, dataUti, orientation, info) =>
                    {
                        try
                        {
                            CIImage fullimage = CIImage.FromData(data);
                            if (fullimage?.Properties != null)
                            {
                                meta = new NSMutableDictionary
                                {
                                    [ImageIO.CGImageProperties.Orientation] = NSNumber.FromNInt((int)(fullimage.Properties.Orientation ?? CIImageOrientation.TopLeft)),
                                    [ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary()
                                };
                            }
                            
                            fullimage?.Dispose();
							fullimage = null;
							GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                    });
                }
                else
                {
                    imageManager.RequestImageData(image, requestOptions, (data, dataUti, orientation, info) =>
                    {
                        try
                        {
                            CIImage fullimage = CIImage.FromData(data);
                            if (fullimage?.Properties != null)
                            {
                                meta = new NSMutableDictionary
                                {
                                    [ImageIO.CGImageProperties.Orientation] = NSNumber.FromNInt((int)(fullimage.Properties.Orientation ?? CIImageOrientation.TopLeft)),
                                    [ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary(),
                                    [ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary()
                                };
                            }
                            
                            fullimage?.Dispose();
							fullimage = null;
							GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                    });
                }
                
            }
            catch
            {
                
            }

            return meta;
        }
        
        /// <summary>
		/// Fetch image metadata from a photos Local (in app) path
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static NSDictionary GetLocalFileMetadata(NSUrl url)
        {
			ImageIO.CGImageSource ImgSrc;
			ImgSrc = ImageIO.CGImageSource.FromUrl(url, null);

			var meta = new NSDictionary();
			NSDictionary props = ImgSrc.CopyProperties(meta, 0);

			ImgSrc?.Dispose();
			ImgSrc = null;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

			return props;
        }
    }
}
