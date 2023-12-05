
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Protocols.Tds;

internal sealed class CopyOnWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _sourceDictionary;
    private readonly IEqualityComparer<TKey> _comparer;
    private IDictionary<TKey, TValue>? _innerDictionary;

    public CopyOnWriteDictionary(
        IDictionary<TKey, TValue> sourceDictionary,
        IEqualityComparer<TKey> comparer)
    {
        _sourceDictionary = sourceDictionary;
        _comparer = comparer;
    }

    private IDictionary<TKey, TValue> ReadDictionary => _innerDictionary ?? _sourceDictionary;

    private IDictionary<TKey, TValue> WriteDictionary => _innerDictionary ??= new Dictionary<TKey, TValue>(_sourceDictionary, _comparer);

    public ICollection<TKey> Keys => ReadDictionary.Keys;

    public ICollection<TValue> Values => ReadDictionary.Values;

    public int Count => ReadDictionary.Count;

    public bool IsReadOnly => false;

    public TValue this[TKey key]
    {
        get => ReadDictionary[key];
        set => WriteDictionary[key] = value;
    }

    public bool ContainsKey(TKey key) => ReadDictionary.ContainsKey(key);

    public void Add(TKey key, TValue value) => WriteDictionary.Add(key, value);

    public bool Remove(TKey key) => WriteDictionary.Remove(key);

    public bool TryGetValue(TKey key,
#if NET8_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out TValue value) => ReadDictionary.TryGetValue(key, out value);

    public void Add(KeyValuePair<TKey, TValue> item) => WriteDictionary.Add(item);

    public void Clear() => WriteDictionary.Clear();

    public bool Contains(KeyValuePair<TKey, TValue> item) => ReadDictionary.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ReadDictionary.CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) => WriteDictionary.Remove(item);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ReadDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
