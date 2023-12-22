using System.ComponentModel;
using System.Runtime.CompilerServices;

public class DuplicatedFilesCollection(HashValue hashValue, IEnumerable<HashedFile> files) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public static readonly DuplicatedFilesCollection Empty = new(HashValue.Empty, Array.Empty<HashedFile>());

    private readonly List<HashedFile> _files = new(files);

    public HashValue HashValue => hashValue;
    public IReadOnlyList<HashedFile> Files => _files;

    public void Merge(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        foreach (var file in duplicatedFilesCollection.Files)
        {
            if (!_files.Contains(file))
            {
                _files.Add(file);
                OnPropertyChanged(nameof(Files));
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
};
