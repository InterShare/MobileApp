namespace InterShareMobile.Entities
{
    public class MyDeviceInfo : Notifiable
    {
        private bool _loading;
        private string _ipAddress;
        private int _port;
        private string _name;
        private string _userIdentifier;

        public bool Loading { get => _loading; set => Set(ref _loading, value); }
        public string IpAddress { get => _ipAddress; set => Set(ref _ipAddress, value); }
        public int Port { get => _port; set => Set(ref _port, value); }
        public string Name {get => _name; set => Set(ref _name, value); }
        public string UserIdentifier { get => _userIdentifier; set => Set(ref _userIdentifier, value); }
    }
}