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

    [ObservableProperty]
    private bool _isSelected;

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