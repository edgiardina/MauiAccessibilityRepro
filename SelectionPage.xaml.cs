using System.Windows.Input;

namespace MauiAccessibilityRepro;

public partial class SelectionPage : ContentPage
{
    private List<ResultItem> _activeResults = [];
    private List<ResultItem> _unusedResults = [];
    private List<ResultItem> _pastResults = [];
    private ResultItem? _selectedResult;
    private bool _isBusy = true;

    public List<ResultItem> ActiveResults
    {
        get => _activeResults;
        set { _activeResults = value; OnPropertyChanged(); }
    }

    public List<ResultItem> UnusedResults
    {
        get => _unusedResults;
        set { _unusedResults = value; OnPropertyChanged(); }
    }

    public List<ResultItem> PastResults
    {
        get => _pastResults;
        set { _pastResults = value; OnPropertyChanged(); }
    }

    public ResultItem? SelectedResult
    {
        get => _selectedResult;
        set { _selectedResult = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public ICommand ProfileCommand => new Command(() => { });

    public SelectionPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(500);

        ActiveResults = Enumerable.Range(1, 20).Select(i => new ResultItem
        {
            TournamentName = $"Tournament {i}",
            EventName = $"Event {i}",
            EventDate = DateTime.Now.AddDays(-i),
            Position = i,
            CurrentPoints = i * 10.5
        }).ToList();

        UnusedResults = Enumerable.Range(1, 10).Select(i => new ResultItem
        {
            TournamentName = $"Unused Tournament {i}",
            EventName = $"Event {i}",
            EventDate = DateTime.Now.AddDays(-i * 2),
            Position = i + 20,
            CurrentPoints = i * 5.25
        }).ToList();

        PastResults = Enumerable.Range(1, 15).Select(i => new ResultItem
        {
            TournamentName = $"Past Tournament {i}",
            EventName = $"Event {i}",
            EventDate = DateTime.Now.AddDays(-i * 30),
            Position = i + 40,
            CurrentPoints = i * 3.0
        }).ToList();

        IsBusy = false;
    }
}
