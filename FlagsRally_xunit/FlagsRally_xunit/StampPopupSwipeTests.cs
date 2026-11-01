using System.Xml.Linq;

namespace FlagsRallyTests.Views;

/// <summary>
/// The popup after stamping moves between its pages with a swipe on the stamp. On Android the stamp
/// took the touch itself, so a swipe on it did nothing and the region's page (and OK) could not be
/// reached that way.
/// </summary>
public class StampPopupSwipeTests
{
    [Fact]
    public void The_stamp_lets_a_swipe_through_to_its_stage()
    {
        var document = XDocument.Load(Path.Combine(RepositoryRoot(), "Views", "StampAcquiredPopupView.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2009/xaml";

        var stamp = document.Descendants().Single(e => (string?)e.Attribute(x + "Name") == "stamp");
        var stage = stamp.Parent!;

        Assert.Equal("True", (string?)stamp.Attribute("InputTransparent"));
        Assert.Equal("stage", (string?)stage.Attribute(x + "Name"));
        Assert.Contains(stage.Descendants(), e => e.Name.LocalName == "SwipeGestureRecognizer");
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
