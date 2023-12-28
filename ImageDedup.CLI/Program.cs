using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ImageDedup.Shared;

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole())
    .BuildServiceProvider();

var Folders = new[] { Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) };
var FileExtensions = new[] { "*.png", "*.jpg" };
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

CancellationTokenSource cts = new();
PerceptualHash hashAlgorithm = new();
HashedFilesCollection hashedFilesCollection = new(logger);
ImageSourceProcessor imageSourceProcessor = new(Folders, FileExtensions, hashAlgorithm, hashedFilesCollection, logger, cts.Token);
ProgressReporter progressReporter = new(hashedFilesCollection, TimeSpan.FromMilliseconds(500), logger, cts);

Task.Run(progressReporter.Invoke);
Task.Run(() => imageSourceProcessor.Invoke().Count()).ContinueWith(_ =>
{
    logger.LogInformation("Scan complete. Press any key to exit.");
    cts.Cancel();
});

Console.WriteLine("Scanning for duplicates... press CTRL+C to cancel.");
Console.ReadKey();
cts.Cancel();
