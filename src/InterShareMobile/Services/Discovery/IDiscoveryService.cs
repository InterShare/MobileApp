using System.Collections.ObjectModel;
using SMTSP.Entities;

namespace InterShareMobile.Services.Discovery
{
    public interface IDiscoveryService
    {
        ObservableCollection<DeviceInfo> DiscoveredDevices { get; set; }

        void Setup(DeviceInfo myDevice);
        void Advertise();
        void StopAdvertising();
        void StartSearchingForDevices();
    }
}