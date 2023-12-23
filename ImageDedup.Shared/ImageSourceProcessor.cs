using CoenM.ImageHash;
using Microsoft.Extensions.Logging;
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
    public IEnumerable<DuplicatedFilesCollection> Invoke()
    {
        EnumerationOptions enumerationOptions = new() { RecurseSubdirectories = true };
        foreach (var folder in Folders)
        {
            var matchingFilePaths = Directory.EnumerateFiles(folder, "*.*", enumerationOptions)
                .Where(path => FileExtensions.Any(ext => FileSystemName.MatchesSimpleExpression(ext, path)));
            foreach (var filePath in matchingFilePaths)
            {
                using var fileStream = File.OpenRead(filePath);
                var hash = HashAlgorithm.Hash(fileStream);
                if (HashedFilesCollection.Add(hash, filePath))
                {
                    yield return HashedFilesCollection.Get(hash);
                }
                if (CancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }
        }
    }
}
