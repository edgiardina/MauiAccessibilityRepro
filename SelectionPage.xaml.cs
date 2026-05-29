namespace MauiAccessibilityRepro;

public partial class SelectionPage : ContentPage
{
    public List<string> Items { get; } = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();

    public SelectionPage()
    {
        InitializeComponent();
        BindingContext = this;
    }
}
