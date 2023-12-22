using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using ImageDedup.Shared;

namespace ImageDedup.UI;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ViewModel _viewModel = new();

    public MainWindow(ILogger<MainWindow> logger, CancellationTokenSource cancellationTokenSource)
    {
        _logger = logger;
        _cancellationTokenSource = cancellationTokenSource;
        _viewModel.SearchFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        _viewModel.Update(AppStatus.Ready);
        DataContext = _viewModel;
        InitializeComponent();
    }

    #region Event Handlers

    private async void StartSearch_Click(object sender, EventArgs e)
    {
        _viewModel.Update(AppStatus.Searching);

        var FileExtensions = new[] { "*.png", "*.jpg" };
        PerceptualHash hashAlgorithm = new();
        HashedFilesCollection hashedFilesCollection = new(_logger);
        ImageSourceProcessor imageSourceProcessor = new(_viewModel.SearchFolders, FileExtensions, hashAlgorithm, hashedFilesCollection, _logger, _cancellationTokenSource.Token);

        try
        {
            await Task.Run(() =>
            {
                foreach (var duplicatedFilesCollection in imageSourceProcessor.Invoke())
                {
                    UpdateViewModel(duplicatedFilesCollection);
                }
            });

            _viewModel.Update(AppStatus.Completed);
        }
        catch (Exception ex)
        {
            _viewModel.LastException = ex;
            _viewModel.Update(AppStatus.Failed);
        }
    }

    private void StopSearch_Click(object sender, EventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    private void ResetSearch_Click(object sender, EventArgs e)
    {
        _viewModel.Reset();
    }

    private void AddFolder_Click(object sender, EventArgs e)
    {
        OpenFolderDialog openFolderDialog = new()
        {
            Multiselect = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (openFolderDialog.ShowDialog() == true)
        {
            foreach (string folderNames in openFolderDialog.FolderNames)
            {
                _viewModel.SearchFolders.Add(folderNames);
            }
        }
    }

    private void RemoveFolder_Click(object sender, EventArgs e)
    {
        var selectedPaths = SearchFolders.SelectedItems
            .Cast<string>()
            .ToList();
        foreach (string path in selectedPaths)
        {
            _viewModel.SearchFolders.Remove(path);
        }
    }

    private void ClearFolders_Click(object sender, EventArgs e)
    {
        _viewModel.SearchFolders.Clear();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    #endregion


    private void UpdateViewModel(DuplicatedFilesCollection duplicatedFilesCollection)
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.AddOrMerge(duplicatedFilesCollection);
        });
    }
}
