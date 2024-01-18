namespace Microsoft.Protocols.Tds.Features;

public interface IConnectionStringFeature
{
    string ConnectionString { get; }

    bool TryGetValue(string key, out ReadOnlyMemory<char> value);
}
