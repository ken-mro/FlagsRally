namespace FlagsRally
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            const int newWidth = 500;
            const int newHeight = 900;

            window.MinimumHeight = window.MaximumHeight = newHeight;
            window.MinimumWidth = window.MaximumWidth = newWidth;

            return window;
        }
    }
}
