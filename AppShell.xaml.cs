namespace MauiAccessibilityRepro;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("SelectionPage", typeof(SelectionPage));
	}
}
