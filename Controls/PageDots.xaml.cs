using System.Collections;

namespace FlagsRally.Controls;

public partial class PageDots : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(PageDots));

    public PageDots()
    {
        InitializeComponent();
    }

    /// <summary>The pages, each an <see cref="ViewModels.IPageDot"/>.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
