using System;
using System.Windows.Input;
using InterShareMobile.Controls.Entities;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace InterShareMobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomButton : ContentView
    {
        private ButtonTypes _type = ButtonTypes.Filled;

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            propertyName: nameof(Text),
            returnType: typeof(string),
            declaringType: typeof(CustomButton),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay
        );

        public static readonly BindableProperty ImageProperty = BindableProperty.Create(
            propertyName: nameof(Image),
            returnType: typeof(string),
            declaringType: typeof(CustomButton),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay
        );


        public static readonly BindableProperty LoadingProperty = BindableProperty.Create(
            propertyName: nameof(Loading),
            returnType: typeof(bool),
            declaringType: typeof(CustomButton),
            defaultValue: false,
            defaultBindingMode: BindingMode.OneWay
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Image
        {
            get => (string)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }


        public ButtonTypes Type
        {
            get => _type;
            set => SetButtonStyle(value);
        }

        public bool Loading
        {
            get => (bool)GetValue(LoadingProperty);
            set => SetValue(LoadingProperty, value);
        }

        public event EventHandler Clicked = delegate { };

        public string ButtonColor { get; set; } = "#3478f6";

        public ICommand TapCommand { get; set; }

        public CustomButton()
        {
            TapCommand = new Command(
                execute: () => OnClicked(null, null),
                canExecute: () => IsEnabled);

            BindingContext = this;
            InitializeComponent();
        }

        private void SetButtonStyle(ButtonTypes value)
        {
            _type = value;

            string userColor = ButtonColor.Remove(0, 1);

            var backgroundColorLight = "";
            var backgroundColorDark = "";
            var color = "white";

            switch (Type)
            {
                case ButtonTypes.Filled:
                    backgroundColorLight = $"#{userColor}";
                    backgroundColorDark = $"#{userColor}";
                    color = "#ffffff";
                    break;

                case ButtonTypes.Tinted:
                    backgroundColorLight = $"#4D{userColor}";
                    backgroundColorDark = $"#4D{userColor}";
                    color = $"#{userColor}";
                    break;

                case ButtonTypes.Gray:
                    backgroundColorLight = "#e9e9eb";
                    backgroundColorDark = "#131314";
                    color = $"#{userColor}";
                    break;
            }

            CustomButtonBox.SetAppThemeColor(BackgroundColorProperty, Color.FromHex(backgroundColorLight), Color.FromHex(backgroundColorDark));
            CustomButtonLabel.TextColor = Color.FromHex(color);
        }

        private void OnClicked(object sender, EventArgs e)
        {
            if (!Loading)
            {
                Clicked?.Invoke(sender, e);
            }
        }
    }
}