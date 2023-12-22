using CommunityToolkit.Mvvm.ComponentModel;
using ImageDedup.Shared;
using System.ComponentModel;

namespace ImageDedup.UI;

public partial class SearchResult : ObservableObject
{
    [ObservableProperty]
    private ulong _hash;

    public int Count => Files.Count;

    public BindingList<DuplicateFile> Files { get; } = new();

    public void Merge(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        foreach (var file in duplicatedFilesCollection.Files)
        {
            var duplicateFile = DuplicateFile.From(file);
            if (!Files.Contains(duplicateFile))
            {
                Files.Add(duplicateFile);
            }
        }
        OnPropertyChanged(nameof(Count));
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
