using System.Threading.Tasks;
using InterShareMobile.Entities;

namespace InterShareMobile.Services
{
    public interface IMediaPickerService
    {
        Task<SelectedFile?> PickPhotoOrVideo();
    }
}