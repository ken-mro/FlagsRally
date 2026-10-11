using System.ComponentModel;
using FlagsRally.ViewModels;

namespace FlagsRally.Controls;

public partial class PassportFilterBar : ContentView
{
    // The two round buttons on the right, with the spacing between the columns.
    const double ButtonsWidth = 44 + 44 + 3 * 4;

    MainPageViewModel? _viewModel;

    public PassportFilterBar()
    {
        InitializeComponent();
        bar.SizeChanged += (_, _) => PlaceCountryFilter();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = BindingContext as MainPageViewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        PlaceCountryFilter();
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.CountryFilterText)) PlaceCountryFilter();
    }

    // The country filter shows its full text beside the switch when it fits there on one line.
    // Otherwise it shows the short form ("All" or the country's flag), and if even that does not
    // fit, it gets a row of its own below. The label's text is set here rather than bound, so it
    // can be measured right away and the chip never shows the wrong form on screen.
    void PlaceCountryFilter()
    {
        if (_viewModel is null) return;
        countryFilterLabel.Text = _viewModel.CountryFilterText;
        if (bar.Width <= 0) return;

        var room = bar.Width - bar.Padding.HorizontalThickness - ButtonsWidth
            - regionSwitch.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;

        var compact = MeasureFilter() > room;
        if (compact) countryFilterLabel.Text = _viewModel.CompactCountryFilterText;
        var ownRow = compact && MeasureFilter() > room;

        if (ownRow == (Grid.GetRow(countryFilter) == 1)) return;
        Grid.SetRow(countryFilter, ownRow ? 1 : 0);
        Grid.SetColumn(countryFilter, ownRow ? 0 : 1);
        Grid.SetColumnSpan(countryFilter, ownRow ? 4 : 1);
    }

    double MeasureFilter() => countryFilter.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
}
