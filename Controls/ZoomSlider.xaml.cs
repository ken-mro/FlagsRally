using FlagsRally.Helpers;

namespace FlagsRally.Controls;

/// <summary>
/// The map's zoom control. Sliding reports how far the finger has moved since it went down
/// (<see cref="Dragged"/>); taps on the top and bottom halves report one step (<see cref="Stepped"/>).
/// The page turns them into camera moves with <see cref="MapZoom"/>.
/// </summary>
public partial class ZoomSlider : ContentView
{
    public enum DragStatus { Started, Running, Completed }

    public sealed class DragEventArgs(DragStatus status, double totalY) : EventArgs
    {
        public DragStatus Status { get; } = status;
        /// <summary>How far the finger has moved since it went down; negative is up.</summary>
        public double TotalY { get; } = totalY;
    }

    public event EventHandler<DragEventArgs>? Dragged;

    /// <summary>+1 for the top half (zoom in), −1 for the bottom half (zoom out).</summary>
    public event EventHandler<int>? Stepped;

    public ZoomSlider()
    {
        InitializeComponent();
    }

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                knob.CancelAnimations();
                Dragged?.Invoke(this, new DragEventArgs(DragStatus.Started, 0));
                break;
            case GestureStatus.Running:
                knob.TranslationY = MapZoom.KnobOffset(e.TotalY, (track.Height - knob.Height) / 2);
                Dragged?.Invoke(this, new DragEventArgs(DragStatus.Running, e.TotalY));
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _ = knob.TranslateToAsync(0, 0, 150, Easing.CubicOut);
                Dragged?.Invoke(this, new DragEventArgs(DragStatus.Completed, 0));
                break;
        }
    }

    void OnZoomInTapped(object? sender, TappedEventArgs e) => Stepped?.Invoke(this, 1);

    void OnZoomOutTapped(object? sender, TappedEventArgs e) => Stepped?.Invoke(this, -1);
}
