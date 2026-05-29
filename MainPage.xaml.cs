namespace MauiAccessibilityRepro;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnNavigateClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SelectionPage");
    }
}
