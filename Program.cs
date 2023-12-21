using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Enumeration;

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole())
    .BuildServiceProvider();

var Folders = new[] { "Q:\\Google\\Photos" };
var FileExtensions = new[] { "*.png", "*.jpg" };
var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();

CancellationTokenSource cts = new();
PerceptualHash hashAlgorithm = new();
HashedFilesCollection hashedFilesCollection = new(logger);
ImageSourceProcessor imageSourceProcessor = new(Folders, FileExtensions, hashAlgorithm, hashedFilesCollection, logger, cts);
ProgressReporter progressReporter = new(hashedFilesCollection, TimeSpan.FromMilliseconds(500), logger, cts);

Task.Run(progressReporter.Invoke);
Task.Run(imageSourceProcessor.Invoke).ContinueWith(_ => cts.Cancel());

Console.WriteLine("Press CTRL+C to cancel...");
Console.ReadKey();
cts.Cancel();

record ProgressReporter(HashedFilesCollection HashedFilesCollection, TimeSpan Interval, ILogger Logger, CancellationTokenSource CancellationTokenSource)
{
    private readonly PeriodicTimer _progressReporter = new(Interval);
    public async Task Invoke()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (await _progressReporter.WaitForNextTickAsync(CancellationTokenSource.Token))
        {
            var elapsed = stopwatch.Elapsed;
            var total = HashedFilesCollection.Total;
            var hits = HashedFilesCollection.Hits;
            var filesPerSecond = total / elapsed.TotalSeconds;
            Logger.LogInformation("Processed {total} files with {hits} duplicate hits in {elapsed} at rate of {filesPerSecond} files per seconds",
                total,
                hits,
                elapsed,
                filesPerSecond);
        }
    }
}

record ImageSourceProcessor(
    IEnumerable<string> Folders,
    IEnumerable<string> FileExtensions,
    IImageHash HashAlgorithm,
    HashedFilesCollection HashedFilesCollection,
    ILogger Logger,
    CancellationTokenSource CancellationTokenSource)
{
    public void Invoke()
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
                HashedFilesCollection.Add(hash, filePath);
            }
        }
    }
}

record HashedFilesCollection(ILogger logger)
{
    private readonly Dictionary<HashValue, List<HashedFile>> _hashedFiles = new();
    private int _total = 0;
    private int _hits = 0;

    public bool Add(ulong hash, string path)
    {
        bool isHit;
        var hashValue = new HashValue(hash);
        if (_hashedFiles.TryGetValue(hashValue, out var hashedFiles))
        {
            isHit = true;
        }
        else
        {
            isHit = false;
            hashedFiles = new();
            _hashedFiles.Add(hashValue, hashedFiles);
        }

        var hashedFile = new HashedFile(path);
        hashedFiles.Add(hashedFile);

        if (isHit)
        {
            _hits++;
            logger.LogInformation("Hit for hash {0}: \n{1}", hash, string.Join("\n", hashedFiles));
        }

        _total++;

        return isHit;
    }

    public int Total => _total;
    public int Hits => _hits;
}

record HashValue(ulong Hash);

record HashedFile(string Path);