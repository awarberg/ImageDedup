using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ImageDedup.Shared;

namespace ImageDedup.UI;

public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private AppStatus _appStatus = AppStatus.Ready;

    public bool CanStartSearch => AppStatus == AppStatus.Ready;
    public bool CanStopSearch => AppStatus == AppStatus.Searching;
    public bool CanResetSearch => AppStatus == AppStatus.Completed;
    public bool CanAddFolder => AppStatus == AppStatus.Ready;
    public bool CanRemoveFolders => AppStatus == AppStatus.Ready;

    [ObservableProperty]
    private Exception _lastException;

    public BindingList<string> SearchFolders { get; } = new();

    public BindingList<SearchResult> SearchResults { get; } = new();

    public void Update(AppStatus appStatus)
    {
        AppStatus = appStatus;

        OnPropertyChanged(nameof(CanStartSearch));
        OnPropertyChanged(nameof(CanStopSearch));
        OnPropertyChanged(nameof(CanResetSearch));

        OnPropertyChanged(nameof(CanAddFolder));
        OnPropertyChanged(nameof(CanRemoveFolders));
    }

    public void AddOrMerge(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        var previous = SearchResults
            .FirstOrDefault(dfc => dfc.Hash == duplicatedFilesCollection.HashValue.Hash);
        if (previous is null)
        {
            SearchResults.Add(SearchResult.From(duplicatedFilesCollection));
        }
        else
        {
            previous.Merge(duplicatedFilesCollection);
        }
        OnPropertyChanged(nameof(SearchResults));
    }

    public void Reset()
    {
        SearchResults.Clear();
        OnPropertyChanged(nameof(SearchResults));
        Update(AppStatus.Ready);
    }
}
