using System;
using SMTSP.Discovery.Entities;

namespace InterShareMobile.Services.Discovery
{
    public interface IDiscoveryService
    {
        event EventHandler<DiscoveryDeviceInfo> OnNewDeviceDiscovered;

        void Setup(DiscoveryDeviceInfo myDevice);
        void RegisterForDiscovery();
        void StartDiscovering();
        void BroadcastMyDevice();
        void StopDiscovering();
    }
}