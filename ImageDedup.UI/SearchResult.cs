using CommunityToolkit.Mvvm.ComponentModel;
using ImageDedup.Shared;
using System.ComponentModel;

namespace ImageDedup.UI;

public partial class SearchResult : ObservableObject
{
    public SearchResult()
    {
        Files.ListChanged += Files_ListChanged;
    }

    private void Files_ListChanged(object? sender, ListChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(Count));
    }

    [ObservableProperty]
    private ulong _hash;

    public BindingList<DuplicateFile> Files { get; } = [];

    public int Count => Files.Count;

    public void Merge(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        foreach (var file in duplicatedFilesCollection.Files)
        {
            if (!Files.Any(f => f.Path.Equals(file.Path)))
            {
                var duplicateFile = DuplicateFile.From(file);
                Files.Add(duplicateFile);
            }
        }
    }

    public static SearchResult From(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        SearchResult searchResult = new()
        {
            Hash = duplicatedFilesCollection.HashValue.Hash,
        };
        foreach (var file in duplicatedFilesCollection.Files)
        {
            searchResult.Files.Add(DuplicateFile.From(file));
        }
        return searchResult;
    }
}
