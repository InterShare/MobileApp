namespace InterShareMobile.Services
{
    public interface IDirectoryService
    {
        string GetDownloadDirectory();
        void OpenDownloadDirectory();
    }
}