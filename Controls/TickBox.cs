using System.Windows.Input;
using FlagsRally.Helpers;
using FlagsRally.Resources;
using Microsoft.Maui.Controls.Shapes;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace FlagsRally.Controls;

/// <summary>
/// The app's tick box, used for every list that ticks things (adding or deleting stamp trails, the
/// map's Filter list and its select-all button). Like a CheckBox, a tap ticks or unticks it and
/// <see cref="IsChecked"/> binds both ways; it also has a partly ticked state (a filled square), for
/// a row that stands for several things of which only some are ticked.
/// It is drawn the same way on Android and iOS (the platform CheckBox is a square on one and a
/// circle on the other, and has no partly ticked state).
/// Colours follow the app's rule: a ticked or chosen state is navy, an action (Add, Delete) is
/// stamp red. Inside a navy chip, set <see cref="Color"/> to white and <see cref="TickColor"/> to navy.
/// </summary>
public class TickBox : ContentView
{
    const double BoxSize = 22;

    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(
        nameof(IsChecked), typeof(bool), typeof(TickBox), false, BindingMode.TwoWay, propertyChanged: Redraw);

    public static readonly BindableProperty IsPartlyCheckedProperty = BindableProperty.Create(
        nameof(IsPartlyChecked), typeof(bool), typeof(TickBox), false, propertyChanged: Redraw);

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(TickBox), null, propertyChanged: Redraw);

    public static readonly BindableProperty TickColorProperty = BindableProperty.Create(
        nameof(TickColor), typeof(Color), typeof(TickBox), null, propertyChanged: Redraw);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(TickBox));

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(TickBox));

    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description), typeof(string), typeof(TickBox), null, propertyChanged: Redraw);

    readonly Border _box;
    readonly MauiPath _tick;
    readonly BoxView _square;

    public TickBox()
    {
        _tick = new MauiPath
        {
            Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M1.2,4.7 L4.7,8.2 L11.2,1.2")!,
            StrokeThickness = 2.4,
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        _square = new BoxView
        {
            WidthRequest = 10,
            HeightRequest = 10,
            CornerRadius = 1,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        _box = new Border
        {
            WidthRequest = BoxSize,
            HeightRequest = BoxSize,
            StrokeShape = new RoundRectangle { CornerRadius = 3 },
            StrokeThickness = 2,
            Padding = 0,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Content = new Grid { Children = { _tick, _square } },
        };

        // 44 x 44, the smallest comfortable touch target, around the 22 point box.
        MinimumWidthRequest = 44;
        MinimumHeightRequest = 44;
        Content = _box;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnTapped();
        GestureRecognizers.Add(tap);
        Update();
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Shows a filled square when not ticked: only some of what the row stands for is ticked.</summary>
    public bool IsPartlyChecked
    {
        get => (bool)GetValue(IsPartlyCheckedProperty);
        set => SetValue(IsPartlyCheckedProperty, value);
    }

    /// <summary>The frame, the fill when ticked and the partly ticked square. Navy unless set.</summary>
    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>The tick on the filled box. Paper white unless set.</summary>
    public Color? TickColor
    {
        get => (Color?)GetValue(TickColorProperty);
        set => SetValue(TickColorProperty, value);
    }

    /// <summary>Run on a tap instead of ticking or unticking, when what a tap does is decided elsewhere.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>What the box is for (such as a board's name); screen readers hear it with the state.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    void OnTapped()
    {
        if (Command is not null)
        {
            if (Command.CanExecute(CommandParameter)) Command.Execute(CommandParameter);
            return;
        }
        IsPartlyChecked = false;
        IsChecked = !IsChecked;
    }

    static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((TickBox)bindable).Update();

    void Update()
    {
        var color = Color ?? AppColors.Get("Navy", "#182A52");
        var tickColor = TickColor ?? AppColors.Get("PaperCard", "#FBF8F0");
        var partly = IsPartlyChecked && !IsChecked;

        _box.Stroke = color;
        _box.BackgroundColor = IsChecked ? color : Colors.Transparent;
        _tick.Stroke = tickColor;
        _tick.IsVisible = IsChecked;
        _square.Color = color;
        _square.IsVisible = partly;

        var state = IsChecked ? AppResources.TickBoxTicked
            : partly ? AppResources.TickBoxPartlyTicked
            : AppResources.TickBoxNotTicked;
        SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Description) ? state : $"{Description}, {state}");
    }
}
