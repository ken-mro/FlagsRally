using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class PayWallView : Popup
{
	public PayWallView(PayWallViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}