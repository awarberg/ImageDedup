using Microsoft.Extensions.Logging;

namespace ImageDedup.Shared;

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
            hashedFiles = [];
            _hashedFiles.Add(hashValue, hashedFiles);
        }

        var hashedFile = new HashedFile(path);
        hashedFiles.Add(hashedFile);

        _total++;

        if (isHit)
        {
            _hits++;
            _logger.LogInformation("Hit for hash {Hash}: \n{Files}", hash, string.Join("\n", hashedFiles));
            return true;
        }

        return false;
    }

    internal DuplicatedFilesCollection Get(ulong hash)
    {
        var hashValue = new HashValue(hash);
        var hashedFiles = _hashedFiles[hashValue];
        return new(hashValue, hashedFiles);
    }

    public int Total => _total;
    public int Hits => _hits;
}
