using Microsoft.Extensions.Logging;

public class HashedFilesCollection
{
    private readonly Dictionary<HashValue, List<HashedFile>> _hashedFiles = new();
    private readonly ILogger _logger;
    private int _total = 0;
    private int _hits = 0;

    public HashedFilesCollection(ILogger logger)
    {
        _logger = logger;
    }

    public (bool, DuplicatedFilesCollection) Add(ulong hash, string path)
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

        _total++;

        if (isHit)
        {
            _hits++;
            _logger.LogInformation("Hit for hash {0}: \n{1}", hash, string.Join("\n", hashedFiles));
            return (true, new DuplicatedFilesCollection(hashValue, hashedFiles));
        }

        return (false, DuplicatedFilesCollection.Empty);
    }

    public int Total => _total;
    public int Hits => _hits;
}
