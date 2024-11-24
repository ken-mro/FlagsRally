using FlagsRally.Repository;

namespace FlagsRally
{
    public partial class AppShell : Shell
    {
        readonly ICustomBoardRepository _customBoardRepository;
        public ShellContent CustomBoardPage;
        public AppShell(ICustomBoardRepository customBoardRepository)
        {
            InitializeComponent();
            _customBoardRepository = customBoardRepository;
            CustomBoardPage = customBoardPage;
            _ = SetCustomBoardPageVisibility();
        }

        public async Task SetCustomBoardPageVisibility()
        {
            CustomBoardPage.IsVisible = await _customBoardRepository.GetCustomBoardExists();
        }
    }
}
