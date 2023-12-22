namespace ImageDedup.Shared;

public record DuplicatedFilesCollection(HashValue HashValue, IReadOnlyList<HashedFile> Files);