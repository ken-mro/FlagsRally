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

            const int newWidth = 400;
            const int newHeight = 800;

            window.MinimumHeight = window.Height = newHeight;
            window.MinimumWidth = window.Width = newWidth;

            return window;
        }
    }
}
