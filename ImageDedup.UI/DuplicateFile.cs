using ImageDedup.Shared;
using System.IO;

namespace ImageDedup.UI;

public record DuplicateFile(string Path)
{
    public long Size => new FileInfo(Path).Length;

    internal static DuplicateFile From(HashedFile file)
    {
        return new(Path: file.Path);
    }
}