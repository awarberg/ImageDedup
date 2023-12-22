public record HashedFile(string Path)
{
    public long Size => new FileInfo(Path).Length;
};