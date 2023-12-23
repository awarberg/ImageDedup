using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ImageDedup.Shared;

namespace ImageDedup.UI;

public partial class ViewModel : ObservableObject
{
    public ViewModel()
    {
        SearchResults.ListChanged += SearchResults_ListChanged;
    }

    private void SearchResults_ListChanged(object? sender, ListChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MarkedDuplicates));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CanStartSearch),
        nameof(CanStopSearch),
        nameof(CanResetSearch),
        nameof(CanAddFolder),
        nameof(CanRemoveFolders))]
    private AppStatus _appStatus = AppStatus.Ready;

    public bool CanStartSearch => AppStatus == AppStatus.Ready;
    public bool CanStopSearch => AppStatus == AppStatus.Searching;
    public bool CanResetSearch => AppStatus == AppStatus.Completed;
    public bool CanAddFolder => AppStatus == AppStatus.Ready;
    public bool CanRemoveFolders => AppStatus == AppStatus.Ready;

    public BindingList<string> SearchFolders { get; } = [];

    public BindingList<SearchResult> SearchResults { get; } = [];

    public IReadOnlyList<DuplicateFile> MarkedDuplicates => SearchResults
        .SelectMany(sr => sr.Files.Where(df => df.IsSelected))
        .OrderBy(df => df.Path)
        .ToList();

    [ObservableProperty]
    private Exception? _lastException;

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
    }

    public void Reset()
    {
        SearchResults.Clear();
        AppStatus = AppStatus.Ready;
    }
}
