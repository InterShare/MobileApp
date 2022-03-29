using System;
using System.Windows.Input;
using SMTSP.Entities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeviceView : ContentView
    {
        public static readonly BindableProperty DeviceInfoProperty = BindableProperty.Create(
            propertyName: nameof(DeviceInfo),
            returnType: typeof(DeviceInfo),
            declaringType: typeof(DeviceView),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay
        );

        public DeviceInfo DeviceInfo
        {
            get => (DeviceInfo)GetValue(DeviceInfoProperty);
            set => SetValue(DeviceInfoProperty, value);
        }

        public ICommand DeviceTappedCommand { get; set; }
        public event EventHandler<DeviceInfo> Clicked = delegate { };

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