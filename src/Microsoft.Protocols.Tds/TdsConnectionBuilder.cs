namespace Microsoft.Protocols.Tds;

public sealed class TdsConnectionBuilder : ITdsConnectionBuilder, IServiceProvider
{
    private readonly List<Func<TdsConnectionDelegate, TdsConnectionDelegate>> _components = new();

    private TdsConnectionBuilder(IServiceProvider? provider)
    {
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            { nameof(Services), provider ?? this }
        };
    }

    private TdsConnectionBuilder(IDictionary<string, object?> properties)
    {
        Properties = new CopyOnWriteDictionary<string, object?>(properties, StringComparer.Ordinal);
    }

    public IServiceProvider Services => Properties.TryGetValue(nameof(Services), out var result) && result is IServiceProvider services ? services : this;

    public IDictionary<string, object?> Properties { get; }

    public static ITdsConnectionBuilder Create(IServiceProvider? provider = null) => new TdsConnectionBuilder(provider);

    private static TdsConnectionDelegate _connection = context =>
#if NET8_0_OR_GREATER
        ValueTask.CompletedTask;
#else
        default;
#endif

    public TdsConnectionDelegate Build()
    {
        var connection = _connection;

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            connection = _components[c](connection);
        }

        return connection;
    }

    public ITdsConnectionBuilder New() => new TdsConnectionBuilder(Properties);

    public ITdsConnectionBuilder Use(Func<TdsConnectionDelegate, TdsConnectionDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    object? IServiceProvider.GetService(Type serviceType) => null;
}
