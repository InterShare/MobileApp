namespace InterShareMobile.Entities
{
    public class SendFileBindings : Notifiable
    {
        private bool _loading;
        private string _ipAddress;
        private int _port;

        public bool Loading { get => _loading; set => Set(ref _loading, value); }
        public string IpAddress { get => _ipAddress; set => Set(ref _ipAddress, value); }
        public int Port { get => _port; set => Set(ref _port, value); }
    }
}