namespace Microsoft.Protocols.Tds;

internal static class DictionaryExtensions
{
    public static void Add<T>(this Dictionary<ReadOnlyMemory<char>, T> d, string key, T value)
        => d.Add(key, value);
}
