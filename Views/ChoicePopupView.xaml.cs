using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class ChoicePopupView : Popup
{
    public ChoicePopupView(ChoicePopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = viewModel;

        var screenHeight = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;

        if (viewModel.IsShort)
        {
            BindableLayout.SetItemsSource(rows, viewModel.Rows);
            rowsScroll.MaximumHeightRequest = screenHeight * 0.45;
            return;
        }

        // A long list cannot be as tall as rows it has not made yet, so its row of the grid gets a
        // set height. In a row sized to its content, iOS makes every row before the popup shows
        // (five seconds for the countries).
        longRows.ItemsSource = viewModel.Rows;
        root.RowDefinitions[1].Height = new GridLength(screenHeight * 0.45);

        // It starts with the row in use in view.
        var current = viewModel.IsPickOne ? viewModel.Rows.FirstOrDefault(row => row.IsTicked) : null;
        if (current is not null)
        {
            longRows.Loaded += (_, _) => longRows.ScrollTo(current.Index, position: ScrollToPosition.Center, animate: false);
        }
    }
}
