using System.Collections.Concurrent;
using CoenM.ImageHash;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Threading.Tasks.Dataflow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageDedup.Shared;

public record ImageSourceProcessor(
    IEnumerable<string> Folders,
    IEnumerable<string> FileExtensions,
    IImageHash HashAlgorithm,
    HashedFilesCollection HashedFilesCollection,
    ILogger Logger,
    CancellationToken CancellationToken)
{
    private static readonly TimeSpan ProgressUpdateInterval = TimeSpan.FromSeconds(1);

    public event EventHandler? ProgressUpdate;

    private TimeSpan _lastProgressUpdate = TimeSpan.Zero;
    private int _totalFiles = 0;
    private int _processedFiles = 0;
    private long _timestamp = 0;

    public async IAsyncEnumerable<DuplicatedFilesCollection> Invoke()
    {
        _timestamp = Stopwatch.GetTimestamp();

        EnumerationOptions enumerationOptions = new() { RecurseSubdirectories = true };
        var paths = Folders
            .SelectMany(f => Directory.EnumerateFiles(f, "*.*", enumerationOptions))
            .Where(path => FileExtensions.Any(ext => FileSystemName.MatchesSimpleExpression(ext, path)));

        var processorCount = Environment.ProcessorCount;

        BlockingCollection<DuplicatedFilesCollection> newDuplicatedFiles = new();

        var addToCollectionAction = new ActionBlock<(string path, ulong hash)>(input =>
        {
            if (HashedFilesCollection.Add(input.hash, input.path))
            {
                var duplicatedFilesCollection = HashedFilesCollection.Get(input.hash);
                newDuplicatedFiles.Add(duplicatedFilesCollection, CancellationToken);
            }

            _processedFiles++;

            UpdateProgressIfNecessary(input.path);
        }, new() { MaxDegreeOfParallelism = 1, CancellationToken = CancellationToken });

        var hashAction = new ActionBlock<(string path, Image<Rgba32> image)>(input =>
        {
            var hash = HashAlgorithm.Hash(input.image);
            addToCollectionAction.Post((input.path, hash));

            UpdateProgressIfNecessary(input.path);
        }, new() { MaxDegreeOfParallelism = processorCount / 2, CancellationToken = CancellationToken });

        var loadImageAction = new ActionBlock<string>(path =>
        {
            var bytes = File.ReadAllBytes(path);
            var image = Image.Load<Rgba32>(bytes);

            hashAction.Post((path, image));

            UpdateProgressIfNecessary(path);
        }, new() { MaxDegreeOfParallelism = processorCount / 4, CancellationToken = CancellationToken });

        foreach (var path in paths)
        {
            _totalFiles++;
            loadImageAction.Post(path);
            if (CancellationToken.IsCancellationRequested) yield break;
            UpdateProgressIfNecessary(path);
        }

        var imageProcessing = Task.Run(async () =>
        {
            loadImageAction.Complete();
            await loadImageAction.Completion;

            hashAction.Complete();
            await hashAction.Completion;

            addToCollectionAction.Complete();
            await addToCollectionAction.Completion;

            newDuplicatedFiles.CompleteAdding();
        }, CancellationToken);

        foreach (var duplicatedFilesCollection in newDuplicatedFiles.GetConsumingEnumerable(CancellationToken))
        {
            yield return duplicatedFilesCollection;
        }

        await imageProcessing;

        UpdateProgress(string.Empty);
    }

    private void UpdateProgressIfNecessary(string path)
    {
        var elapsed = Stopwatch.GetElapsedTime(_timestamp);
        if (elapsed - _lastProgressUpdate < ProgressUpdateInterval) return;

        UpdateProgress(path);
    }

    private void UpdateProgress(string path)
    {
        var elapsed = Stopwatch.GetElapsedTime(_timestamp);

        ProgressUpdate?.Invoke(this, new ProgressUpdateEventArgs
        {
            CurrentFolder = Path.GetDirectoryName(path),
            Elapsed = elapsed,
            TotalFiles = _totalFiles,
            ProcessedFiles = _processedFiles,
        });

        _lastProgressUpdate = elapsed;
    }

    public class ProgressUpdateEventArgs : EventArgs
    {
        public string? CurrentFolder { get; init; }
        public TimeSpan Elapsed { get; init; }
        public int TotalFiles { get; init; }
        public int ProcessedFiles { get; init; }
        public int FilesPerSecond => ProcessedFiles / (int)Math.Round(Elapsed.TotalSeconds);
    }
}