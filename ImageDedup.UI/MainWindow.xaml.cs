using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static UI.MainWindow;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ViewModel _viewModel = new();

        public MainWindow(ILogger<MainWindow> logger, CancellationTokenSource cancellationTokenSource)
        {
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
            DataContext = _viewModel;
            InitializeComponent();
        }

        private async void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsWaiting = false;
            var Folders = new[] { "Q:\\Google\\Photos\\Photos from 2014" };
            var FileExtensions = new[] { "*.png", "*.jpg" };
            PerceptualHash hashAlgorithm = new();
            HashedFilesCollection hashedFilesCollection = new(_logger);
            ImageSourceProcessor imageSourceProcessor = new(Folders, FileExtensions, hashAlgorithm, hashedFilesCollection, _logger, _cancellationTokenSource.Token);

            await Task.Run(() =>
            {
                foreach (var duplicatedFilesCollection in imageSourceProcessor.Invoke())
                {
                    UpdateViewModel(duplicatedFilesCollection);
                }
            });
        }

        private void UpdateViewModel(DuplicatedFilesCollection duplicatedFilesCollection)
        {
            var duplicatedFilesCollections = _viewModel.DuplicatedFilesCollections;
            Dispatcher.Invoke(() =>
            {
                var previous = duplicatedFilesCollections
                    .FirstOrDefault(dfc => dfc.HashValue == duplicatedFilesCollection.HashValue);
                if (previous is null)
                {
                    duplicatedFilesCollections.Add(duplicatedFilesCollection);
                }
                else
                {
                    previous.Merge(duplicatedFilesCollection);
                }
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        public class ViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private bool _isWaiting = true;
            public bool IsWaiting
            {
                get { return _isWaiting; }
                set { _isWaiting = value; OnPropertyChanged(); }
            }

            public BindingList<DuplicatedFilesCollection> DuplicatedFilesCollections { get; } = new();

            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
