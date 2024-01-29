using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using ImageDedup.Shared;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.VisualBasic.FileIO;

namespace ImageDedup.UI;

public partial class ViewModel : ObservableObject
{
    private readonly List<SearchResult> _backingSearchResults = [];
    public ViewModel()
    {
        SearchResults = new(_backingSearchResults);
        SearchResults.ListChanged += SearchResults_ListChanged;
    }

    private void SearchResults_ListChanged(object? sender, ListChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MarkedDuplicates));
        DeleteMarkedDuplicatesCommand.NotifyCanExecuteChanged();
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

    [ObservableProperty]
    public string? _currentFolder;

    [ObservableProperty]
    public string? _totalFiles;

    [ObservableProperty]
    public string? _filesPerSecond;

    public BindingList<SearchResult> SearchResults { get; }

    public IReadOnlyList<DuplicateFile> MarkedDuplicates => SearchResults
        .SelectMany(sr => sr.Files.Where(df => df.IsSelected))
        .OrderBy(df => df.Path)
        .ToList();

    [ObservableProperty]
    private int _previewHeight = 300;

    [ObservableProperty]
    private bool _useRecycleBin = true;

    [ObservableProperty]
    private Exception? _lastException;

    [RelayCommand(CanExecute = nameof(CanDeleteMarkedDuplicates))]
    private void DeleteMarkedDuplicates()
    {
        var markedDuplicates = MarkedDuplicates;
        MessageBoxResult messageBoxResult = MessageBox.Show($"Delete {markedDuplicates.Count} file(s)?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
        if (messageBoxResult == MessageBoxResult.Yes)
        {
            foreach (var duplicateFile in markedDuplicates)
            {
                // TODO: Recycle bin does not work for network shares.
                FileSystem.DeleteFile(duplicateFile.Path,
                    UIOption.OnlyErrorDialogs,
                    UseRecycleBin ?
                        RecycleOption.SendToRecycleBin :
                        RecycleOption.DeletePermanently);
                duplicateFile.IsDeleted = true;
            }
        }
    }

    private bool CanDeleteMarkedDuplicates =>
        SearchResults.Any(sr => sr.Files.Any(f => f.IsSelected));

    public CancellationTokenSource? CancellationTokenSource { get; internal set; }

    public void AddOrMerge(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        var previous = SearchResults
            .FirstOrDefault(dfc => dfc.Hash == duplicatedFilesCollection.HashValue.Hash);
        if (previous is null)
        {
            SearchResults.Add(SearchResult.From(duplicatedFilesCollection));
            _backingSearchResults.Sort((x, y) => Comparer<ulong>.Default.Compare(x.Hash, y.Hash));
            SearchResults.ResetBindings();
        }
        else
        {
            previous.Merge(duplicatedFilesCollection);
        }
    }

    public void Reset()
    {
        ResetProgress();
        SearchResults.Clear();
        AppStatus = AppStatus.Ready;
    }

    public void ResetProgress()
    {
        CurrentFolder = string.Empty;
        TotalFiles = string.Empty;
        FilesPerSecond = string.Empty;
    }
}
