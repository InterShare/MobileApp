using System;
using System.Windows.Input;
using SMTSP.Discovery.Entities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceView : ContentView
    {
        public static readonly BindableProperty DeviceInfoProperty = BindableProperty.Create(
            propertyName: nameof(DeviceInfo),
            returnType: typeof(DiscoveryDeviceInfo),
            declaringType: typeof(DeviceView),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay
        );

        public DiscoveryDeviceInfo DeviceInfo
        {
            get => (DiscoveryDeviceInfo)GetValue(DeviceInfoProperty);
            set => SetValue(DeviceInfoProperty, value);
        }

        public ICommand DeviceTappedCommand { get; set; }
        public event EventHandler<DiscoveryDeviceInfo> Clicked = delegate { };

        public DeviceView()
        {
            DeviceTappedCommand = new Command(OnClicked, () => IsEnabled);
            BindingContext = this;
            InitializeComponent();
        }

        private void OnClicked()
        {
            Clicked.Invoke(this, DeviceInfo);
        }
    }
}