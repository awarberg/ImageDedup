using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using ImageDedup.Shared;
using static ImageDedup.Shared.ImageSourceProcessor;

namespace ImageDedup.UI;

public partial class MainWindow : Window
{
    private static readonly string[] _fileExtensions = ["*.png", "*.jpg"];
    private readonly ILogger<MainWindow> _logger;
    private readonly ViewModel _viewModel = new();

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger;
        _viewModel.SearchFolders.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        _viewModel.AppStatus = AppStatus.Ready;
        DataContext = _viewModel;
        InitializeComponent();
    }

    #region Event Handlers

    private async void StartSearch_Click(object sender, EventArgs e)
    {
        CancellationTokenSource cts = new();
        _viewModel.CancellationTokenSource = cts;
        _viewModel.AppStatus = AppStatus.Searching;
        PerceptualHash hashAlgorithm = new();
        HashedFilesCollection hashedFilesCollection = new(_logger);
        ImageSourceProcessor imageSourceProcessor = new(_viewModel.SearchFolders, _fileExtensions, hashAlgorithm, hashedFilesCollection, _logger, cts.Token);

        try
        {
            imageSourceProcessor.ProgressUpdate += ImageSourceProcessor_ProgressUpdateChanged;
            await Task.Run(() =>
            {
                foreach (var duplicatedFilesCollection in imageSourceProcessor.Invoke())
                {
                    UpdateViewModel(duplicatedFilesCollection);
                }
            });

            _viewModel.AppStatus = AppStatus.Completed;
        }
        catch (Exception ex)
        {
            _viewModel.LastException = ex;
            _viewModel.AppStatus = AppStatus.Failed;
        }
    }

    private void ImageSourceProcessor_ProgressUpdateChanged(object? sender, EventArgs e)
    {
        var progressUpdate = (ProgressUpdateEventArgs)e;
        _viewModel.CurrentFolder = progressUpdate.CurrentFolder;
        _viewModel.TotalFiles = progressUpdate.TotalFiles.ToString();
        _viewModel.FilesPerSecond = progressUpdate.FilesPerSecond.ToString();
    }

    private void StopSearch_Click(object sender, EventArgs e)
    {
        _viewModel.CancellationTokenSource.Cancel();
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
        _viewModel.CancellationTokenSource?.Cancel();
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
