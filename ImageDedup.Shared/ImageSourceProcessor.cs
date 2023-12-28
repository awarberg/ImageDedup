using CoenM.ImageHash;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Enumeration;

namespace ImageDedup.Shared;

public record ImageSourceProcessor(
    IEnumerable<string> Folders,
    IEnumerable<string> FileExtensions,
    IImageHash HashAlgorithm,
    HashedFilesCollection HashedFilesCollection,
    ILogger Logger,
    CancellationToken CancellationToken)
{
    public event EventHandler? ProgressUpdate;

    public IEnumerable<DuplicatedFilesCollection> Invoke()
    {
        var stopwatch = Stopwatch.StartNew();
        EnumerationOptions enumerationOptions = new() { RecurseSubdirectories = true };
        var paths = Folders
            .SelectMany(f => Directory.EnumerateFiles(f, "*.*", enumerationOptions))
            .Where(path => FileExtensions.Any(ext => FileSystemName.MatchesSimpleExpression(ext, path)));
        var lastProgressUpdate = TimeSpan.Zero;
        var totalFiles = 0;
        foreach (var path in paths)
        {
            totalFiles++;
            using var fileStream = File.OpenRead(path);
            var hash = HashAlgorithm.Hash(fileStream);
            if (HashedFilesCollection.Add(hash, path))
            {
                yield return HashedFilesCollection.Get(hash);
            }
            if (CancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            if (stopwatch.Elapsed - lastProgressUpdate >= TimeSpan.FromSeconds(1))
            {
                if (ProgressUpdate != null)
                {
                    ProgressUpdate(this, new ProgressUpdateEventArgs
                    {
                        CurrentFolder = Path.GetDirectoryName(path),
                        Elapsed = stopwatch.Elapsed,
                        TotalFiles = totalFiles,
                    });
                }
                lastProgressUpdate = stopwatch.Elapsed;
            }
        }
    }

    public class ProgressUpdateEventArgs : EventArgs
    {
        public string? CurrentFolder { get; init; }
        public TimeSpan Elapsed { get; init; }
        public int TotalFiles { get; init; }
        public int FilesPerSecond => TotalFiles / (int)Math.Round(Elapsed.TotalSeconds);
    }
}
