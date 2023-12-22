using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageDedup.Shared;

public record ProgressReporter(HashedFilesCollection HashedFilesCollection, TimeSpan Interval, ILogger Logger, CancellationTokenSource CancellationTokenSource)
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
