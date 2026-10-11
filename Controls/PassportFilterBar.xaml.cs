namespace FlagsRally.Controls;

public partial class PassportFilterBar : ContentView
{
    // The two round buttons on the right, with the spacing between the columns.
    const double ButtonsWidth = 44 + 44 + 3 * 4;

    public PassportFilterBar()
    {
        InitializeComponent();
        bar.SizeChanged += (_, _) => PlaceCountryFilter();
        countryFilterLabel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Label.Text)) PlaceCountryFilter();
        };
    }

    // The country filter stays beside the switch when its text fits there on one line;
    // otherwise it gets a row of its own below, where a long name can wrap at its words.
    void PlaceCountryFilter()
    {
        if (bar.Width <= 0) return;

        var room = bar.Width - bar.Padding.HorizontalThickness - ButtonsWidth
            - regionSwitch.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        var filterWidth = countryFilter.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        var ownRow = filterWidth > room;

        if (ownRow == (Grid.GetRow(countryFilter) == 1)) return;
        Grid.SetRow(countryFilter, ownRow ? 1 : 0);
        Grid.SetColumn(countryFilter, ownRow ? 0 : 1);
        Grid.SetColumnSpan(countryFilter, ownRow ? 4 : 1);
    }
}
