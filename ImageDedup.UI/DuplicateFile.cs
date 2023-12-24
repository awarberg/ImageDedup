using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageDedup.Shared;
using System.Diagnostics;
using System.IO;

namespace ImageDedup.UI;

public partial class DuplicateFile(string path) : ObservableObject
{
    public string Path { get; } = path;

    public long Size => new FileInfo(Path).Length;

    /// Copy of the data to avoid locking the file for later deletion
    public byte[] Data => File.ReadAllBytes(path);

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    public bool _isDeleted;

    [RelayCommand]
    private void OpenInDefaultApp()
    {
        Process.Start("explorer", Path);
    }

    [RelayCommand]
    private void Unselect()
    {
        IsSelected = false;
    }

    internal static DuplicateFile From(HashedFile file)
    {
        return new(file.Path);
    }
}