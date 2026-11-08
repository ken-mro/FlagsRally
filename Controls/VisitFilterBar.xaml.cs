namespace FlagsRally.Controls;

public partial class VisitFilterBar : ContentView
{
    /// <summary>Shows the number of places beside All, Visited and Not visited.</summary>
    public static readonly BindableProperty ShowsCountsProperty =
        BindableProperty.Create(nameof(ShowsCounts), typeof(bool), typeof(VisitFilterBar), false);

    public bool ShowsCounts
    {
        get => (bool)GetValue(ShowsCountsProperty);
        set => SetValue(ShowsCountsProperty, value);
    }

    public VisitFilterBar()
    {
        InitializeComponent();
    }
}
