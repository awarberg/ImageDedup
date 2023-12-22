using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
ImageSourceProcessor imageSourceProcessor = new(Folders, FileExtensions, hashAlgorithm, hashedFilesCollection, logger, cts.Token);
ProgressReporter progressReporter = new(hashedFilesCollection, TimeSpan.FromMilliseconds(500), logger, cts);

Task.Run(progressReporter.Invoke);
Task.Run(imageSourceProcessor.Invoke).ContinueWith(_ => cts.Cancel());

Console.WriteLine("Press CTRL+C to cancel...");
Console.ReadKey();
cts.Cancel();
