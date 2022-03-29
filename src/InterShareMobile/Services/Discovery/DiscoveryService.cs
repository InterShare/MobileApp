using System.Collections.ObjectModel;
using SMTSP.Entities;

namespace InterShareMobile.Services.Discovery
{
    public class DiscoveryService : IDiscoveryService
    {
        // private SmtspDiscovery _smtsDiscovery;

        public ObservableCollection<DeviceInfo> DiscoveredDevices { get; set; } = new ObservableCollection<DeviceInfo>();

        public void Setup(DeviceInfo myDevice)
        {
            // _smtsDiscovery = new SmtspDiscovery(myDevice);
            // DiscoveredDevices = _smtsDiscovery.DiscoveredDevices;
        }

        public void Advertise()
        {
            // _smtsDiscovery?.Advertise();
        }

        public void StopAdvertising()
        {
            // _smtsDiscovery?.StopAdvertising();
        }

        public void StartSearchingForDevices()
        {
            // _smtsDiscovery?.SendOutLookupSignal();
        }
    }
}