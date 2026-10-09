using FlagsRally.Views;

namespace FlagsRally
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Detail pages opened from the Collections tab
            Routing.RegisterRoute(FlagsBoardPage.Route, typeof(FlagsBoardPage));
            Routing.RegisterRoute(CustomBoardPage.Route, typeof(CustomBoardPage));
            Routing.RegisterRoute(ManageCustomBoardsPage.Route, typeof(ManageCustomBoardsPage));
            Routing.RegisterRoute(BoardCatalogPage.Route, typeof(BoardCatalogPage));
        }
    }
}
