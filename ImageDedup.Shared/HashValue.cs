public record HashValue(ulong Hash)
{
    public static readonly HashValue Empty = new(0);
}
