namespace Microsoft.Protocols.Tds;

public interface ITdsConnectionBuilder
{
    public IServiceProvider Services { get; }

    public ITdsConnectionBuilder Use(Func<TdsConnectionDelegate, TdsConnectionDelegate> middleware);

    public ITdsConnectionBuilder New();

    public IDictionary<string, object?> Properties { get; }

    TdsConnectionDelegate Build();
}
