using System;
using SMTSP.Discovery;
using SMTSP.Discovery.Entities;

namespace InterShareMobile.Services.Discovery
{
    public class DiscoveryService : IDiscoveryService
    {
        private SmtsDiscovery _smtsDiscovery;

        public event EventHandler<DiscoveryDeviceInfo> OnNewDeviceDiscovered = delegate { };

        public void Setup(DiscoveryDeviceInfo myDevice)
        {
            _smtsDiscovery = new SmtsDiscovery(myDevice);
            _smtsDiscovery.OnNewDeviceDiscovered += OnDiscoveredDevicesChangeEvent;
        }

        private void OnDiscoveredDevicesChangeEvent(object sender, DiscoveryDeviceInfo devicesInfo)
        {
            OnNewDeviceDiscovered.Invoke(this, devicesInfo);
        }

        public void RegisterForDiscovery()
        {
            _smtsDiscovery.AllowToBeDiscovered();
        }

        public void BroadcastMyDevice()
        {
            _smtsDiscovery.BroadcastMyDevice();
        }

        public void StartDiscovering()
        {
            _smtsDiscovery.StartDiscovering();
        }

        public void StopDiscovering()
        {
            _smtsDiscovery.StopDiscovering();
        }
    }
}