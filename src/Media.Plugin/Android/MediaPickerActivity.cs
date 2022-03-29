using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Environment = Android.OS.Environment;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;
using Plugin.Media.Abstractions;
using Android.Content.PM;
using System.Globalization;
#if __ANDROID_29__
using AndroidX.Core.Content;
#else
using Android.Support.V4.Content;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Plugin.Media
{
	/// <summary>
	/// Picker
	/// </summary>
	[Activity(ConfigurationChanges= ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MediaPickerActivity
        : Activity, Android.Media.MediaScannerConnection.IOnScanCompletedListener
    {
        internal const string ExtraPath = "path";
        internal const string ExtraLocation = "location";
        internal const string ExtraType = "type";
        internal const string ExtraId = "id";
        internal const string ExtraAction = "action";
        internal const string ExtraTasked = "tasked";
		internal const string ExtraMultiSelect = "multi_select";
		internal const string ExtraSaveToAlbum = "album_save";
        internal const string ExtraFront = "android.intent.extras.CAMERA_FACING";

        internal static event EventHandler<MediaPickedEventArgs> MediaPicked;

        int id;
        int front;
        string title;
        string description;
        string type;

        /// <summary>
        /// The user's destination path.
        /// </summary>
        Uri path;
        bool isPhoto;
        bool saveToAlbum;
        string action;
		bool multiSelect;

		int seconds;
        long size;
        VideoQuality quality;

        bool tasked;
        /// <summary>
        /// OnSaved
        /// </summary>
        /// <param name="outState"></param>
        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean("ran", true);
            outState.PutString(MediaStore.MediaColumns.Title, title);
            outState.PutString(MediaStore.Images.ImageColumns.Description, description);
            outState.PutInt(ExtraId, id);
            outState.PutString(ExtraType, type);
            outState.PutString(ExtraAction, action);
            outState.PutInt(MediaStore.ExtraDurationLimit, seconds);
            outState.PutLong(MediaStore.ExtraSizeLimit, size);
            outState.PutInt(MediaStore.ExtraVideoQuality, (int)quality);
            outState.PutBoolean(ExtraSaveToAlbum, saveToAlbum);
            outState.PutBoolean(ExtraTasked, tasked);
            outState.PutInt(ExtraFront, front);
			outState.PutBoolean(ExtraMultiSelect, multiSelect);

			if (path != null)
                outState.PutString(ExtraPath, path.Path);

            base.OnSaveInstanceState(outState);
        }

		const string huaweiManufacturer = "Huawei";

        /// <summary>
        /// OnCreate
        /// </summary>
        /// <param name="savedInstanceState"></param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
	        MediaImplementation.CancelRequested += CancellationRequested;

			Bundle b = (savedInstanceState ?? Intent.Extras);

            bool ran = b.GetBoolean("ran", defaultValue: false);

            title = b.GetString(MediaStore.MediaColumns.Title);
            description = b.GetString(MediaStore.Images.ImageColumns.Description);

            tasked = b.GetBoolean(ExtraTasked);
            id = b.GetInt(ExtraId, 0);
            type = b.GetString(ExtraType);
            front = b.GetInt(ExtraFront);
			multiSelect = b.GetBoolean(ExtraMultiSelect);
			if (type == "image/*")
                isPhoto = true;

            action = b.GetString(ExtraAction);
            Intent pickIntent = null;
            try
            {
                pickIntent = new Intent(action);
                if (action == Intent.ActionPick)
				{
                    if (multiSelect)
                    {
                        // Override any default photo gallery by explicity choosing the google photo gallery here
                        // Customization added by Matthew K - Oct 4, 2019
                        //pickIntent.SetClassName("com.google.android.apps.photos", "com.google.android.apps.photos.picker.external.ExternalPickerActivity");
                        pickIntent.SetPackage("com.google.android.apps.photos");
                        pickIntent.PutExtra(Intent.ExtraAllowMultiple, true);
                    }

					pickIntent.SetType(type);
				}
				else
                {
                    if (!isPhoto)
                    {
						var isPixel = false;
						try
						{
							string name = Settings.System.GetString(Application.Context.ContentResolver, "device_name");
							isPixel = name.Contains("Pixel") || name.Contains("pixel");
						}
						catch (Exception)
						{
						}

                        seconds = b.GetInt(MediaStore.ExtraDurationLimit, 0);
						if (seconds != 0 && !isPixel)
							pickIntent.PutExtra(MediaStore.ExtraDurationLimit, seconds);
						
                        size = b.GetLong(MediaStore.ExtraSizeLimit, 0);
                        if (size != 0)
                        {
                            pickIntent.PutExtra(MediaStore.ExtraSizeLimit, size);
                        }
                    }

                    saveToAlbum = b.GetBoolean(ExtraSaveToAlbum);
                    pickIntent.PutExtra(ExtraSaveToAlbum, saveToAlbum);

                    quality = (VideoQuality)b.GetInt(MediaStore.ExtraVideoQuality, (int)VideoQuality.High);
                    pickIntent.PutExtra(MediaStore.ExtraVideoQuality, GetVideoQuality(quality));

                    if (front != 0)
                    {
                        pickIntent.UseFrontCamera();
                    }
                    else
                    {
                        pickIntent.UseBackCamera();
                    }

                    if (!ran)
                    {
                        path = GetOutputMediaFile(this, b.GetString(ExtraPath), title, isPhoto, false);

                        Touch();
						

						if (path.Scheme == "file")
						{
							try
							{
								Uri photoURI = FileProvider.GetUriForFile(this,
																		  Application.Context.PackageName + ".fileprovider",
																		  new Java.IO.File(path.Path));

								GrantUriPermissionsForIntent(pickIntent, photoURI);
								pickIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
								pickIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
								pickIntent.PutExtra(MediaStore.ExtraOutput, photoURI);
							}
							catch (Java.Lang.IllegalArgumentException iae)
							{
								//Using a Huawei device on pre-N. Increased likelihood of failure...
								if (huaweiManufacturer.Equals(Build.Manufacturer, StringComparison.CurrentCultureIgnoreCase) && (int)Build.VERSION.SdkInt < 24)
								{
									pickIntent.PutExtra(MediaStore.ExtraOutput, path);
								}
								else
								{
									System.Diagnostics.Debug.WriteLine($"Unable to get file location, check and set manifest with file provider. Exception: {iae}");

									throw new ArgumentException("Unable to get file location. This most likely means that the file provider information is not set in your Android Manifest file. Please check documentation on how to set this up in your project.", iae);
								}
							}
							catch (Exception ex)
							{
								System.Diagnostics.Debug.WriteLine($"Unable to get file location, check and set manifest with file provider. Exception: {ex}");

								throw new ArgumentException("Unable to get file location. This most likely means that the file provider information is not set in your Android Manifest file. Please check documentation on how to set this up in your project.", ex);
							}
						}
						else
						{
							pickIntent.PutExtra(MediaStore.ExtraOutput, path);
						}
                    }
                    else
                        path = Uri.Parse(b.GetString(ExtraPath));
                }



                if (!ran)
                    StartActivityForResult(pickIntent, id);
            }
            catch (Exception ex)
            {
                OnMediaPicked(new MediaPickedEventArgs(id, ex));
                //must finish here because an exception has occured else blank screen
                Finish();
            }
            finally
            {
                if (pickIntent != null)
                    pickIntent.Dispose();
            }
        }

	    void CancellationRequested(object sender, EventArgs e)
	    {
			FinishActivity(id);
			DeleteOutputFile();
			Finish();
		}
		
		void Touch()
        {
            if (path.Scheme != "file")
                return;

            string newPath = GetLocalPath(path);
            try
            {
                FileStream stream = File.Create(newPath);
                stream.Close();
                stream.Dispose();

            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Unable to create path: " + newPath + " " + ex.Message + "This means you have illegal characters");
                throw ex;
            }
        }

        void DeleteOutputFile()
        {
            try
            {
                if (path?.Scheme != "file")
                    return;

                string localPath = GetLocalPath(path);

                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Unable to delete file: " + ex.Message);
            }
        }

		void GrantUriPermissionsForIntent(Intent intent, Uri uri)
		{
			var resInfoList = PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
			foreach (ResolveInfo resolveInfo in resInfoList)
			{
				string packageName = resolveInfo.ActivityInfo.PackageName;
				GrantUriPermission(packageName, uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
			}
		}

        internal static Task<MediaPickedEventArgs> GetMediaFileAsync(Context context, int requestCode, string action, bool isPhoto, ref Uri path, Uri data, bool saveToAlbum)
        {
            Task<Tuple<string, bool>> pathFuture;

            string originalPath = null;

            if (action != Intent.ActionPick)
            {

                originalPath = path.Path;


                // Not all camera apps respect EXTRA_OUTPUT, some will instead
                // return a content or file uri from data.
                if (data != null && data.Path != originalPath)
                {
                    originalPath = data.ToString();
                    string currentPath = path.Path;
                    pathFuture = TryMoveFileAsync(context, data, path, isPhoto, false).ContinueWith(t =>
                        new Tuple<string, bool>(t.Result ? currentPath : null, false));
                }
                else
                {
                    pathFuture = TaskFromResult(new Tuple<string, bool>(path.Path, false));
                   
                }
            }
            else if (data != null)
            {
                originalPath = data.ToString();
                path = data;
                pathFuture = GetFileForUriAsync(context, path, isPhoto, false);
            }
            else
                pathFuture = TaskFromResult<Tuple<string, bool>>(null);

            return pathFuture.ContinueWith(t =>
            {
                
                string resultPath = t?.Result?.Item1;
                string aPath = originalPath;
                if (resultPath != null && File.Exists(resultPath))
                {
                    var mf = new MediaFile(resultPath, () =>
                      {
                          return File.OpenRead(resultPath);
                      }, albumPath: aPath);
                    return new MediaPickedEventArgs(requestCode, false, mf);
                }
                else
                    return new MediaPickedEventArgs(requestCode, new MediaFileNotFoundException(originalPath));
            });
        }

        bool completed;
        /// <summary>
        /// OnActivity Result
        /// </summary>
        /// <param name="requestCode"></param>
        /// <param name="resultCode"></param>
        /// <param name="data"></param>
        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            completed = true;
	        MediaImplementation.CancelRequested -= CancellationRequested;
            base.OnActivityResult(requestCode, resultCode, data);
			

            if (tasked)
            {
				if (data?.ClipData != null)
				{
					ClipData clipData = data.ClipData;
					var mediaFiles = new List<MediaFile>();
					for (var i = 0; i < clipData.ItemCount; i++)
					{
						ClipData.Item item = clipData.GetItemAt(i);
						MediaPickedEventArgs media = await GetMediaFileAsync(this, requestCode, action, isPhoto, ref path, item.Uri, false);

						// TODO: This can be done better.
						mediaFiles.AddRange(media.Media);
					}

					Finish();
					await Task.Delay(50);
					OnMediaPicked(new MediaPickedEventArgs(requestCode, resultCode == Result.Canceled, mediaFiles));

					return;
				}

				Task<MediaPickedEventArgs> future;

                if (resultCode == Result.Canceled)
                {
                    //delete empty file
                    DeleteOutputFile();

                    future = TaskFromResult(new MediaPickedEventArgs(requestCode, isCanceled: true));

                    Finish();
                    await Task.Delay(50);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    future.ContinueWith(t => OnMediaPicked(t.Result));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                else
                {
                    
                    MediaPickedEventArgs e = await GetMediaFileAsync(this, requestCode, action, isPhoto, ref path, data?.Data, false);
                    Finish();
                    await Task.Delay(50);
                    OnMediaPicked(e);

                }
            }
            else
            {
                if (resultCode == Result.Canceled)
                {
                    //delete empty file
                    DeleteOutputFile();

                    SetResult(Result.Canceled);
                }
                else
                {
                    var resultData = new Intent();
                    resultData.PutExtra("MediaFile", data?.Data);
                    resultData.PutExtra("path", path);
                    resultData.PutExtra("isPhoto", isPhoto);
                    resultData.PutExtra("action", action);
                    resultData.PutExtra(ExtraSaveToAlbum, saveToAlbum);
                    SetResult(Result.Ok, resultData);
                }

                Finish();
            }
        }

        static Task<bool> TryMoveFileAsync(Context context, Uri url, Uri path, bool isPhoto, bool saveToAlbum)
        {
            string moveTo = GetLocalPath(path);
            return GetFileForUriAsync(context, url, isPhoto, false).ContinueWith(t =>
            {
                if (t.Result.Item1 == null)
                    return false;

                try
                {
                    if (url.Scheme == "content")
                        context.ContentResolver.Delete(url, null, null);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to delete content resolver file: " + ex.Message);
                }

                try
                {
                    File.Delete(moveTo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to delete normal file: " + ex.Message);
                }

                try
                {
                    File.Move(t.Result.Item1, moveTo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to move files: " + ex.Message);
                }

                return true;
            }, TaskScheduler.Default);
        }

        static int GetVideoQuality(VideoQuality videoQuality)
        {
            switch (videoQuality)
            {
                case VideoQuality.Medium:
                case VideoQuality.High:
                    return 1;

                default:
                    return 0;
            }
        }

        static string GetUniquePath(string folder, string name, bool isPhoto)
        {
            string ext = Path.GetExtension(name);
            if (ext == string.Empty)
                ext = (isPhoto) ? ".jpg" : ".mp4";

            name = Path.GetFileNameWithoutExtension(name);

            string nname = name + ext;
            var i = 1;
            while (File.Exists(Path.Combine(folder, nname)))
                nname = name + "_" + (i++) + ext;

            return Path.Combine(folder, nname);
        }

        /// <summary>
        /// Try go get output file
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subdir"></param>
        /// <param name="name"></param>
        /// <param name="isPhoto"></param>
        /// <param name="saveToAlbum"></param>
        /// <returns></returns>
        public static Uri GetOutputMediaFile(Context context, string subdir, string name, bool isPhoto, bool saveToAlbum)
        {
            subdir = subdir ?? string.Empty;
            Uri uri;

            if (string.IsNullOrWhiteSpace(name))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                if (isPhoto)
                    name = "IMG_" + timestamp + ".jpg";
                else
                    name = "VID_" + timestamp + ".mp4";
            }

            if ((int)Build.VERSION.SdkInt < 29)
            {
                string mediaType = (isPhoto) ? Environment.DirectoryPictures : Environment.DirectoryMovies;
                Java.IO.File directory = saveToAlbum ? Environment.GetExternalStoragePublicDirectory(mediaType) : context.GetExternalFilesDir(mediaType);

                using (var mediaStorageDir = new Java.IO.File(directory, subdir))
                {
                    if (!mediaStorageDir.Exists())
                    {
                        if (!mediaStorageDir.Mkdirs())
                            throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");

                        if (!saveToAlbum)
                        {
                            // Ensure this media doesn't show up in gallery apps
                            using (var nomedia = new Java.IO.File(mediaStorageDir, ".nomedia"))
                                nomedia.CreateNewFile();
                        }
                    }

                    uri = Uri.FromFile(new Java.IO.File(GetUniquePath(mediaStorageDir.Path, name, isPhoto)));
                }
            }
            else
            {
                string mediaType = (isPhoto) ? Environment.DirectoryPictures : Environment.DirectoryMovies;
                Java.IO.File directory = context.GetExternalFilesDir(mediaType);

                using (var mediaStorageDir = new Java.IO.File(directory, subdir))
                {
                    mediaStorageDir.Mkdirs();

                    if (!saveToAlbum)
                    {
                        // Ensure this media doesn't show up in gallery apps
                        using (var nomedia = new Java.IO.File(mediaStorageDir, ".nomedia"))
                            nomedia.CreateNewFile();
                    }

                    uri = Uri.FromFile(new Java.IO.File(GetUniquePath(mediaStorageDir.Path, name, isPhoto)));
                }

                if (saveToAlbum)
                {
                    uri = (isPhoto) ? MediaStore.Images.Media.ExternalContentUri : MediaStore.Video.Media.ExternalContentUri;
                    uri = Uri.WithAppendedPath(uri, name);
                }
            }

            return uri;
        }

        internal static Task<Tuple<string, bool>> GetFileForUriAsync(Context context, Uri uri, bool isPhoto, bool saveToAlbum)
        {
            var tcs = new TaskCompletionSource<Tuple<string, bool>>();

            if (uri.Scheme == "file")
                tcs.SetResult(new Tuple<string, bool>(new System.Uri(uri.ToString()).LocalPath, false));
            else if (uri.Scheme == "content")
            {
                Task.Factory.StartNew(() =>
                {
                    ICursor cursor = null;
                    try
                    {
                        string[] proj = null;
                        if ((int)Build.VERSION.SdkInt >= 22)
                            proj = new[] { MediaStore.MediaColumns.Data };

                        cursor = context.ContentResolver.Query(uri, proj, null, null, null);
                        if (cursor == null || !cursor.MoveToNext())
                            tcs.SetResult(new Tuple<string, bool>(null, false));
                        else
                        {
                            int column = cursor.GetColumnIndex(MediaStore.MediaColumns.Data);
                            string contentPath = null;

                            if (column != -1)
                                contentPath = cursor.GetString(column);



                            // If they don't follow the "rules", try to copy the file locally
							if (contentPath == null || !contentPath.StartsWith("file", StringComparison.InvariantCultureIgnoreCase))
                            {
								string fileName = null;
								try
								{
									fileName = Path.GetFileName(contentPath);
								}
								catch(Exception ex)
								{ 
									System.Diagnostics.Debug.WriteLine("Unable to get file path name, using new unique " + ex);
								}


								Uri outputPath = GetOutputMediaFile(context, "temp", fileName, isPhoto, false);

								try
                                {
                                    using (Stream input = context.ContentResolver.OpenInputStream(uri))
                                        using (FileStream output = File.Create(outputPath.Path))
                                            input.CopyTo(output);

                                    contentPath = outputPath.Path;
                                }
                                catch (Java.IO.FileNotFoundException fnfEx)
                                {
                                    // If there's no data associated with the uri, we don't know
                                    // how to open this. contentPath will be null which will trigger
                                    // MediaFileNotFoundException.
									System.Diagnostics.Debug.WriteLine("Unable to save picked file from disk " + fnfEx);
                                }
                            }

                            tcs.SetResult(new Tuple<string, bool>(contentPath, false));
                        }
                    }
                    finally
                    {
                        if (cursor != null)
                        {
                            cursor.Close();
                            cursor.Dispose();
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }
            else
                tcs.SetResult(new Tuple<string, bool>(null, false));

            return tcs.Task;
        }

        static string GetLocalPath(Uri uri) =>  new System.Uri(uri.ToString()).LocalPath;
        

        static Task<T> TaskFromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        static void OnMediaPicked(MediaPickedEventArgs e) =>
			MediaPicked?.Invoke(null, e);
        

		/// <summary>
		/// Scan completed
		/// </summary>
		/// <param name="path"></param>
		/// <param name="uri"></param>
        public void OnScanCompleted(string path, Uri uri) =>
			Console.WriteLine("scan complete: " + path);
        
		/// <summary>
		/// On Destroy
		/// </summary>
        protected override void OnDestroy()
        {
            if(!completed)
            {
                DeleteOutputFile();
				MediaImplementation.CompletionSource = null;
				MediaImplementation.CompletionSourceMulti = null;
                MediaPicked = null;
            }
            base.OnDestroy();
        }
    }

    class MediaPickedEventArgs
        : EventArgs
    {
        public MediaPickedEventArgs(int id, Exception error)
        {
            RequestId = id;
            Error = error ?? throw new ArgumentNullException("error");
        }

        public MediaPickedEventArgs(int id, bool isCanceled, MediaFile media = null)
        {
            RequestId = id;
            IsCanceled = isCanceled;
            if (!IsCanceled && media == null)
                throw new ArgumentNullException("media");
			
			Media = new List<MediaFile> { media };
		}

		public MediaPickedEventArgs(int id, bool isCanceled, List<MediaFile> medias)
		{
			RequestId = id;
			IsCanceled = isCanceled;
			if (!IsCanceled && medias == null)
				throw new ArgumentNullException("medias");

			Media = medias;
		}

		public int RequestId
        {
            get;
            private set;
        }

        public bool IsCanceled
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public List<MediaFile> Media
        {
            get;
            private set;
        }

        public Task<MediaFile> ToTask()
        {
            var tcs = new TaskCompletionSource<MediaFile>();

            if (IsCanceled)
                tcs.SetResult(null);
            else if (Error != null)
                tcs.SetException(Error);
            else
                tcs.SetResult(Media.FirstOrDefault());

            return tcs.Task;
        }

       
    }
}
